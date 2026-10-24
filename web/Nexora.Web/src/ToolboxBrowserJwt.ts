export type BrowserJwtResult = {
  toolCode: 'jwt'; output: string; warning: string; errorPath: null; durationMilliseconds: number;
};

export const JWT_UNVERIFIED_WARNING = 'Unverified: decoded data only. Signature, issuer, audience and lifetime were NOT verified. Never use this output to authenticate.';

function invalid(): never { throw new Error('JWT input is malformed, unsupported or exceeds decoder limits.'); }

function decodeObject(segment: string): Record<string, unknown> {
  const bytes = decodeSegment(segment);
  if (bytes.length > 16 * 1024) invalid();
  let parsed: unknown;
  try { parsed = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(bytes)); }
  catch { return invalid(); }
  if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) invalid();
  const pending: { value: unknown; depth: number }[] = [{ value: parsed, depth: 1 }];
  let count = 0;
  while (pending.length) {
    const { value, depth } = pending.pop()!;
    if (++count > 4096 || depth > 32) invalid();
    if (value && typeof value === 'object') {
      for (const child of Object.values(value)) pending.push({ value: child, depth: depth + 1 });
    }
  }
  return parsed as Record<string, unknown>;
}

function decodeSegment(segment: string): Uint8Array {
  if (!/^[A-Za-z0-9_-]*$/.test(segment) || segment.length % 4 === 1) invalid();
  try {
    const standard = segment.replaceAll('-', '+').replaceAll('_', '/');
    const decoded = atob(standard + '='.repeat((4 - standard.length % 4) % 4));
    // Reject alternate encodings with nonzero unused bits. No verification of the signature.
    if (btoa(decoded).replaceAll('+', '-').replaceAll('/', '_').replace(/=+$/, '') !== segment) invalid();
    return Uint8Array.from(decoded, character => character.charCodeAt(0));
  } catch { return invalid(); }
}

/** No fetch/storage/clipboard/URL API; its caller supplies a payload-free authority check. */
export async function runBrowserJwt(input: string, authorize: () => Promise<unknown>): Promise<BrowserJwtResult> {
  // Capture and validate only local synthetic text. Never pass input to authorize.
  if (new TextEncoder().encode(input).length > 32 * 1024) invalid();
  await authorize(); // A denial aborts; the integrating screen also guards stale completion/revocation.
  const parts = input.split('.');
  if (parts.length !== 3 || !parts[0] || !parts[1]) invalid();
  const header = decodeObject(parts[0]);
  const payload = decodeObject(parts[1]);
  decodeSegment(parts[2]);
  return {
    toolCode: 'jwt', output: JSON.stringify({ header, payload, signatureVerified: false }, null, 2),
    warning: JWT_UNVERIFIED_WARNING, errorPath: null, durationMilliseconds: 0,
  };
}
