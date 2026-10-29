/** Pure browser text parsing. No fetch, navigation, storage or clipboard APIs. */
export type ToolboxUrlHeadersCode = 'url-parse' | 'headers';
export type ToolboxUrlHeadersResult = {
  toolCode: ToolboxUrlHeadersCode;
  output: string;
  warning: string;
  errorPath: null;
  durationMilliseconds: number;
};

export class ToolboxUrlHeadersError extends Error {
  constructor(public readonly code: string, message: string) {
    super(message);
    this.name = 'ToolboxUrlHeadersError';
  }
}

const REDACTED = '[redacted]';
const encoder = new TextEncoder();
export const TOOLBOX_URL_HEADERS_WARNING =
  'Browser only; no fetch or navigation. Field-based redaction is not a secret scanner, URL allowlist, authentication check or trust certification. Unlabelled sensitive data can remain in visible fields; review before copying.';

function fail(code: string, message: string): never { throw new ToolboxUrlHeadersError(code, message); }
function cancelled(signal?: AbortSignal) {
  if (signal?.aborted) fail('Cancelled', 'Tool processing was cancelled.');
}

function validateText(input: string, maxBytes: number, allowHeaderNewlines = false) {
  if (input.length > maxBytes || encoder.encode(input).length > maxBytes)
    fail('InputTooLarge', 'Input exceeds the selected tool UTF-8 limit.');
  for (let index = 0; index < input.length; index++) {
    const unit = input.charCodeAt(index);
    if (unit >= 0xd800 && unit <= 0xdbff) {
      const next = input.charCodeAt(++index);
      if (next >= 0xdc00 && next <= 0xdfff) continue;
      fail('ValidationFailed', 'Input must contain valid Unicode text.');
    }
    if (unit >= 0xdc00 && unit <= 0xdfff)
      fail('ValidationFailed', 'Input must contain valid Unicode text.');
    if ((unit < 32 && !(allowHeaderNewlines && (unit === 9 || unit === 10 || unit === 13))) ||
        (unit >= 0x7f && unit <= 0x9f) || (unit >= 0x202a && unit <= 0x202e) ||
        (unit >= 0x2066 && unit <= 0x2069) || unit === 0x200e || unit === 0x200f || unit === 0x061c)
      fail('ValidationFailed', 'Control or bidirectional formatting characters are not supported.');
  }
}

// Conservative ASCII field classifier: punctuation/case cannot hide credentials.
// This is deliberately not presented as a detector of arbitrary secret contents.
function sensitiveName(name: string): boolean {
  if (/[^\x20-\x7e]/.test(name)) return true;
  const compact = name.toLowerCase().replace(/[^a-z0-9]/g, '');
  return /auth|password|passwd|pwd|token|secret|cookie|credential|session|signature|key|csrf|xsrf/.test(compact) ||
    ['code', 'sig', 'state', 'assertion'].includes(compact) || /^(?:xamz|xgoog)/.test(compact);
}

function credentialValue(value: string): boolean {
  if (/\b(?:bearer|basic)\s+\S+/i.test(value)) return true;
  // Scan assignment names instead of a regex with overlapping greedy quantifiers.
  for (let equal = value.indexOf('='); equal >= 0; equal = value.indexOf('=', equal + 1)) {
    let end = equal;
    while (end > 0 && /[ \t]/.test(value[end - 1])) end--;
    let start = end;
    while (start > 0 && !/[?&#;\s=]/.test(value[start - 1])) start--;
    if (end > start && sensitiveName(value.slice(start, end))) return true;
  }
  return false;
}

// Inspect authority before decoding can turn encoded userinfo into path/query delimiters.
// A structurally credential-bearing or ambiguous authority masks the whole field.
function credentialAuthority(text: string): boolean {
  for (let marker = text.indexOf('://'); marker >= 0; marker = text.indexOf('://', marker + 3)) {
    // The scheme prefix may itself be encoded; every literal marker is inspected.
    let end = marker + 3;
    while (end < text.length && !/[\s/?#]/.test(text[end])) end++;
    const authority = text.slice(marker + 3, end);
    if (authority.includes('@') || authority.includes('\\') ||
        /%(?:2f|3f|23|40|5c|20|0[0-9a-f]|1[0-9a-f]|7f)/i.test(authority)) return true;
  }
  return false;
}

function encodedSchemeMarker(text: string): boolean {
  // Do not decode a scheme marker and a userinfo delimiter in the same step.
  // Even an encoded URL without credentials is conservatively masked here.
  const markers = /(?::|%3a)(?:\/|%2f)(?:\/|%2f)/gi;
  for (const marker of text.matchAll(markers)) if (marker[0].includes('%')) return true;
  return false;
}

// Additional encodings are inspected only locally. Ambiguity redacts, never repairs.
function inspectEncoding(text: string): { text: string; ambiguous: boolean } {
  let decoded = text;
  for (let layer = 0; layer < 4; layer++) {
    if (encodedSchemeMarker(decoded) || credentialAuthority(decoded) || /[\x00-\x08\x0a-\x1f\x7f-\x9f\u061c\u200e\u200f\u202a-\u202e\u2066-\u2069]/.test(decoded))
      return { text: '', ambiguous: true };
    if (!decoded.includes('%')) return { text: decoded, ambiguous: false };
    try { decoded = decodeURIComponent(decoded); }
    catch { return { text: '', ambiguous: true }; }
  }
  return { text: decoded, ambiguous: decoded.includes('%') || credentialAuthority(decoded) ||
    /[\x00-\x08\x0a-\x1f\x7f-\x9f\u061c\u200e\u200f\u202a-\u202e\u2066-\u2069]/.test(decoded) };
}

function safeVisibleText(text: string, preserveEncoding = false): string {
  const inspected = inspectEncoding(text);
  if (inspected.ambiguous || credentialValue(inspected.text)) return REDACTED;
  return preserveEncoding ? text : inspected.text;
}

function strictDecode(text: string, plusAsSpace = false): string {
  try { return decodeURIComponent(plusAsSpace ? text.replace(/\+/g, ' ') : text); }
  catch { return fail('UrlInvalid', 'URL contains malformed percent encoding or UTF-8.'); }
}

export type ToolboxUrlQueryField = { name: string; value: string; redacted: boolean; classification: 'sensitive' | 'unclassified' };

/** Absolute hierarchical URL components only; never returns the original URL. */
export function parseToolboxUrl(input: string) {
  validateText(input, 16 * 1024);
  if (!/^(?:https?|wss?|ftp):\/\//i.test(input) || /[\s\\]/u.test(input))
    fail('UrlInvalid', 'Use an absolute supported hierarchical URL without whitespace or backslashes.');
  // Validate before WHATWG parsing can normalize malformed/control data.
  validateText(strictDecode(input), 16 * 1024);
  let parsed: URL;
  try { parsed = new URL(input); }
  catch { return fail('UrlInvalid', 'URL is malformed or uses an unsupported authority.'); }
  if (!parsed.hostname) fail('UrlInvalid', 'URL requires a hostname.');
  const rawAuthority = input.slice(input.indexOf('://') + 3).split(/[/?#]/, 1)[0];
  if (!rawAuthority) fail('UrlInvalid', 'URL requires an explicit authority.');
  const userinfoPresent = rawAuthority.includes('@');
  const query: ToolboxUrlQueryField[] = [];
  for (const pair of parsed.search.slice(1).split('&')) {
    if (pair === '') continue;
    if (query.length >= 128) fail('UrlLimitExceeded', 'URL exceeds 128 query fields.');
    const separator = pair.indexOf('=');
    const rawName = separator < 0 ? pair : pair.slice(0, separator);
    const rawValue = separator < 0 ? '' : pair.slice(separator + 1);
    // Preserve privacy decisions before form/query decoding can erase authority boundaries.
    const rawNameInspection = inspectEncoding(rawName);
    const rawValueInspection = inspectEncoding(rawValue);
    const name = strictDecode(rawName, true);
    const value = strictDecode(rawValue, true);
    if (Array.from(name).length > 256 || Array.from(value).length > 4096)
      fail('UrlLimitExceeded', 'URL query field exceeds its decoded character limit.');
    const inspectedName = inspectEncoding(name);
    const namePreview = rawNameInspection.ambiguous ? REDACTED : safeVisibleText(name);
    const valuePreview = safeVisibleText(value);
    const sensitive = rawNameInspection.ambiguous || rawValueInspection.ambiguous || inspectedName.ambiguous || sensitiveName(inspectedName.text) ||
      namePreview !== inspectedName.text || valuePreview !== inspectEncoding(value).text;
    query.push({ name: namePreview, value: sensitive ? REDACTED : valuePreview, redacted: sensitive,
      classification: sensitive ? 'sensitive' : 'unclassified' });
  }
  return {
    parser: 'WHATWG URL; normalized components, no DNS or trust validation',
    scheme: parsed.protocol.slice(0, -1),
    authority: parsed.host,
    hostname: parsed.hostname,
    hostSyntax: parsed.hostname.startsWith('[') ? 'ipv6-literal' : /^\d+\.\d+\.\d+\.\d+$/.test(parsed.hostname) ? 'ipv4-literal' : 'dns-name',
    port: parsed.port || null,
    path: safeVisibleText(parsed.pathname),
    credentials: { present: userinfoPresent, username: userinfoPresent ? REDACTED : null,
      password: userinfoPresent ? REDACTED : null },
    query,
    fragment: { present: input.includes('#'), value: input.includes('#') ? REDACTED : null },
    normalization: 'Hostname case/IDNA, default ports and dot-segments follow the browser URL parser. Visible text is percent-decoded for inspection.',
  };
}

type HeaderField = { name: string; value: string; redacted: boolean; classification: 'sensitive' | 'unclassified'; duplicateCount: number };
function headersInvalid(line: number): never {
  return fail('HeadersInvalid', `Pasted headers are malformed or unsupported at line ${line}.`);
}

/** Pasted header block only. Folded lines reject the entire run; no partial result. */
export function inspectToolboxHeaders(input: string) {
  validateText(input, 64 * 1024, true);
  if (/\r(?!\n)/.test(input)) fail('HeadersInvalid', 'Pasted headers must use LF or CRLF line endings.');
  const lines = input.split(/\r?\n/);
  const fields: HeaderField[] = [];
  const counts = new Map<string, number>();
  let status: { version: string; code: number } | null = null;
  let ended = false;
  for (let index = 0; index < lines.length; index++) {
    const line = lines[index];
    if (encoder.encode(line).length > 8192)
      fail('HeadersLimitExceeded', 'Header line exceeds 8 KiB UTF-8.');
    if (line === '') { ended = true; continue; }
    if (ended || /^[ \t]/.test(line)) headersInvalid(index + 1);
    if (index === 0 && line.startsWith('HTTP/')) {
      const match = /^HTTP\/(1\.0|1\.1|2(?:\.0)?|3(?:\.0)?) ([1-5]\d\d)(?: [^\r\n]*)?$/.exec(line);
      if (!match) headersInvalid(index + 1);
      status = { version: match[1], code: Number(match[2]) };
      continue;
    }
    const separator = line.indexOf(':');
    const name = line.slice(0, separator);
    if (separator < 1 || name.length > 128 || !/^[!#$%&'*+.^_`|~0-9A-Za-z-]+$/.test(name)) headersInvalid(index + 1);
    if (fields.length >= 200) fail('HeadersLimitExceeded', 'Pasted block exceeds 200 header fields.');
    const normalizedName = name.toLowerCase();
    const value = line.slice(separator + 1).replace(/^[ \t]+|[ \t]+$/g, '');
    const preview = safeVisibleText(value, true);
    const sensitive = sensitiveName(normalizedName) || normalizedName.includes('%') ||
      ['location', 'content-location', 'referer', 'refresh', 'link'].includes(normalizedName) ||
      preview !== value;
    fields.push({ name: normalizedName, value: sensitive ? REDACTED : preview, redacted: sensitive,
      classification: sensitive ? 'sensitive' : 'unclassified', duplicateCount: 0 });
    counts.set(normalizedName, (counts.get(normalizedName) ?? 0) + 1);
  }
  const sensitiveGroups = new Set(fields.filter(field => field.redacted).map(field => field.name));
  for (const field of fields) {
    field.duplicateCount = counts.get(field.name)!;
    if (sensitiveGroups.has(field.name)) {
      field.value = REDACTED; field.redacted = true; field.classification = 'sensitive';
    }
  }
  return { status, count: fields.length, fields,
    semantics: 'Pasted text only; duplicate order retained, no comma joining, request replay or authentication verification.' };
}

/** Caller supplies the existing empty-payload authority check, rejecting on denial. */
export async function runToolboxUrlHeaders(
  toolCode: ToolboxUrlHeadersCode, input: string, authorize: () => Promise<unknown>, signal?: AbortSignal,
): Promise<ToolboxUrlHeadersResult> {
  cancelled(signal);
  await authorize();
  cancelled(signal);
  const started = performance.now();
  const value = toolCode === 'url-parse' ? parseToolboxUrl(input) : toolCode === 'headers' ? inspectToolboxHeaders(input) :
    fail('ValidationFailed', 'Unsupported browser tool.');
  const output = JSON.stringify(value, null, 2).replace(/[<>&\u2028\u2029]/g, character =>
    '\\u' + character.charCodeAt(0).toString(16).padStart(4, '0'));
  if (encoder.encode(output).length > 2 * 1024 * 1024) fail('OutputTooLarge', 'Tool output exceeds 2 MiB UTF-8.');
  cancelled(signal);
  return { toolCode, output, warning: TOOLBOX_URL_HEADERS_WARNING, errorPath: null,
    durationMilliseconds: Math.round(performance.now() - started) };
}
