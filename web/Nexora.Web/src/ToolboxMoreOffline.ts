/** No fetch, storage, clipboard, HTML interpretation or execution of supplied text. */
export type MoreOfflineCode = 'text-diff' | 'color';
export type MoreOfflineOptions = Record<string, string>;
export type MoreOfflineResult = {
  toolCode: MoreOfflineCode; output: string; warning: string; errorPath: null; durationMilliseconds: number;
};

export class MoreOfflineError extends Error {
  constructor(public readonly code: string, message: string) { super(message); this.name = 'MoreOfflineError'; }
}

function fail(code: string, message: string): never { throw new MoreOfflineError(code, message); }
function checkCancelled(signal?: AbortSignal) {
  if (signal?.aborted) fail('Cancelled', 'Tool processing was cancelled.');
}
function validateOptions(options: MoreOfflineOptions, allowed: string[]) {
  if (Object.keys(options).length > 8 || Object.entries(options).some(([key, value]) =>
    !allowed.includes(key) || typeof value !== 'string' || !value.length || value.length > 256))
    fail('ValidationFailed', 'Unsupported or malformed tool option.');
}
function enumOption(options: MoreOfflineOptions, key: string, fallback: string, allowed: string[]) {
  const value = options[key] ?? fallback;
  if (!allowed.includes(value)) fail('ValidationFailed', 'Select a supported tool option.');
  return value;
}
function boundedText(value: string, maxBytes: number) {
  // Avoid encoding an unbounded string before checking the UTF-8 byte limit.
  if (value.length > maxBytes || new TextEncoder().encode(value).length > maxBytes)
    fail('InputTooLarge', 'Input exceeds the selected tool limit.');
  for (let index = 0; index < value.length; index++) {
    const unit = value.charCodeAt(index);
    if (unit >= 0xd800 && unit <= 0xdbff) {
      const next = value.charCodeAt(++index);
      if (!(next >= 0xdc00 && next <= 0xdfff)) fail('ValidationFailed', 'Input must contain valid Unicode text.');
    } else if (unit >= 0xdc00 && unit <= 0xdfff) fail('ValidationFailed', 'Input must contain valid Unicode text.');
  }
}

type DiffOperation = { kind: 'equal' | 'delete' | 'insert'; text: string };

async function diffText(before: string, after: string, options: MoreOfflineOptions, signal?: AbortSignal) {
  validateOptions(options, ['mode', 'newline']);
  boundedText(before, 128 * 1024); boundedText(after, 128 * 1024);
  const mode = enumOption(options, 'mode', 'line', ['line', 'character']);
  const newline = enumOption(options, 'newline', 'normalize', ['normalize', 'preserve']);
  if (newline === 'normalize') {
    before = before.replace(/\r\n?/g, '\n'); after = after.replace(/\r\n?/g, '\n');
  }
  const tokenize = (text: string) => mode === 'character' ? Array.from(text) :
    text.match(/[^\r\n]*(?:\r\n|\r|\n)|[^\r\n]+$/g) ?? [];
  const left = tokenize(before), right = tokenize(after);
  if (left.length > 4096 || right.length > 4096 || left.length * right.length > 1_000_000)
    fail('DiffLimitExceeded', 'Diff exceeds 4,096 tokens per side or 1,000,000 comparisons; reduce input or use line mode.');
  const width = right.length + 1;
  const table = new Uint16Array((left.length + 1) * width);
  let work = 0;
  for (let i = left.length - 1; i >= 0; i--) {
    for (let j = right.length - 1; j >= 0; j--) {
      table[i * width + j] = left[i] === right[j] ? 1 + table[(i + 1) * width + j + 1] :
        Math.max(table[(i + 1) * width + j], table[i * width + j + 1]);
      if (++work % 16_384 === 0) {
        await new Promise<void>(resolve => setTimeout(resolve, 0));
        checkCancelled(signal);
      }
    }
  }
  checkCancelled(signal);
  const operations: DiffOperation[] = [];
  const append = (kind: DiffOperation['kind'], text: string) => {
    const previous = operations[operations.length - 1];
    if (previous?.kind === kind) previous.text += text;
    else operations.push({ kind, text });
  };
  let i = 0, j = 0;
  while (i < left.length || j < right.length) {
    if (i < left.length && j < right.length && left[i] === right[j]) { append('equal', left[i]); i++; j++; }
    else if (i < left.length && (j === right.length || table[(i + 1) * width + j] >= table[i * width + j + 1])) {
      append('delete', left[i++]);
    } else append('insert', right[j++]);
  }
  return { mode, newline, characterUnit: 'Unicode scalar (not grapheme)', identical: before === after, operations };
}

type Color = { r: number; g: number; b: number; a: number };
const round = (value: number, digits = 6) => Number(value.toFixed(digits));
function colorInvalid(): never {
  return fail('ColorInvalid', 'Use a supported HEX, comma RGB(A), or HSL(A) color within the declared channel ranges.');
}
function fromHsl(hue: number, saturation: number, lightness: number, a: number): Color {
  const h = ((hue % 360) + 360) % 360 / 60, s = saturation / 100, l = lightness / 100;
  const c = (1 - Math.abs(2 * l - 1)) * s, x = c * (1 - Math.abs(h % 2 - 1)), m = l - c / 2;
  const [r, g, b] = h < 1 ? [c, x, 0] : h < 2 ? [x, c, 0] : h < 3 ? [0, c, x] :
    h < 4 ? [0, x, c] : h < 5 ? [x, 0, c] : [c, 0, x];
  return { r: Math.round((r + m) * 255), g: Math.round((g + m) * 255), b: Math.round((b + m) * 255), a };
}
function parseColor(input: string): Color {
  if (input.length > 256) fail('InputTooLarge', 'Color input must be at most 256 characters.');
  const text = input.trim();
  if (/^#(?:[\da-f]{3}|[\da-f]{4}|[\da-f]{6}|[\da-f]{8})$/i.test(text)) {
    let hex = text.slice(1);
    if (hex.length < 5) hex = Array.from(hex, value => value + value).join('');
    return { r: parseInt(hex.slice(0, 2), 16), g: parseInt(hex.slice(2, 4), 16), b: parseInt(hex.slice(4, 6), 16),
      a: hex.length === 8 ? parseInt(hex.slice(6, 8), 16) / 255 : 1 };
  }
  const match = /^(rgb|rgba|hsl|hsla)\(([^()]*)\)$/i.exec(text);
  if (!match) return colorInvalid();
  const kind = match[1].toLowerCase(), parts = match[2].split(',').map(value => value.trim());
  if (parts.length !== (kind.endsWith('a') ? 4 : 3)) return colorInvalid();
  const hsl = kind.startsWith('hsl');
  const values = parts.map((part, index) => {
    const percent = hsl && (index === 1 || index === 2);
    if (percent) { if (!part.endsWith('%')) return colorInvalid(); part = part.slice(0, -1); }
    if (!/^(?:\d+(?:\.\d+)?|\.\d+)$/.test(part)) return colorInvalid();
    const value = Number(part);
    if (!Number.isFinite(value)) return colorInvalid();
    return value;
  });
  const a = values[3] ?? 1;
  if (a < 0 || a > 1) return colorInvalid();
  if (hsl) {
    if (values[0] > 360 || values[1] > 100 || values[2] > 100) return colorInvalid();
    return fromHsl(values[0], values[1], values[2], a);
  }
  if (values.slice(0, 3).some(value => !Number.isInteger(value) || value < 0 || value > 255)) return colorInvalid();
  return { r: values[0], g: values[1], b: values[2], a };
}
function toHsl(color: Color) {
  const r = color.r / 255, g = color.g / 255, b = color.b / 255;
  const max = Math.max(r, g, b), min = Math.min(r, g, b), delta = max - min, l = (max + min) / 2;
  let hue = 0;
  if (delta !== 0) hue = max === r ? 60 * ((g - b) / delta % 6) : max === g ?
    60 * ((b - r) / delta + 2) : 60 * ((r - g) / delta + 4);
  return { h: (hue + 360) % 360, s: delta === 0 ? 0 : delta / (1 - Math.abs(2 * l - 1)) * 100, l: l * 100 };
}
function luminance(color: { r: number; g: number; b: number }) {
  const linear = (channel: number) => {
    const value = channel / 255;
    return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
  };
  return 0.2126 * linear(color.r) + 0.7152 * linear(color.g) + 0.0722 * linear(color.b);
}
function describeColor(color: Color, background: Color) {
  const byte = (value: number) => Math.round(value).toString(16).padStart(2, '0');
  const hsl = toHsl(color);
  const foregroundLuminance = luminance({ r: color.r * color.a + background.r * (1 - color.a),
    g: color.g * color.a + background.g * (1 - color.a), b: color.b * color.a + background.b * (1 - color.a) });
  const backgroundLuminance = luminance(background);
  const ratio = (Math.max(foregroundLuminance, backgroundLuminance) + 0.05) /
    (Math.min(foregroundLuminance, backgroundLuminance) + 0.05);
  return {
    hex: '#' + byte(color.r) + byte(color.g) + byte(color.b) + (color.a === 1 ? '' : byte(color.a * 255)),
    rgb: { r: color.r, g: color.g, b: color.b, alpha: round(color.a) },
    hsl: { h: round(hsl.h), s: round(hsl.s), l: round(hsl.l), alpha: round(color.a) },
    contrast: { ratio: round(ratio), aaNormal: ratio >= 4.5, aaLarge: ratio >= 3, aaaNormal: ratio >= 7, aaaLarge: ratio >= 4.5 },
  };
}
function convertColor(input: string, options: MoreOfflineOptions) {
  validateOptions(options, ['operation', 'scheme', 'background']);
  const operation = enumOption(options, 'operation', 'convert', ['convert', 'palette']);
  const scheme = enumOption(options, 'scheme', 'complementary', ['complementary', 'analogous', 'triadic', 'monochromatic']);
  const color = parseColor(input), background = parseColor(options.background ?? '#ffffff');
  if (background.a !== 1) fail('ColorInvalid', 'Contrast background must be opaque.');
  const hsl = toHsl(color);
  const offsets = scheme === 'complementary' ? [0, 180] : scheme === 'analogous' ? [-30, 0, 30] : [0, 120, 240];
  const palette = operation !== 'palette' ? [] : scheme === 'monochromatic' ?
    [10, 30, 50, 70, 90].map(lightness => ({ label: `lightness ${lightness}%`,
      ...describeColor(fromHsl(hsl.h, hsl.s, lightness, color.a), background) })) :
    offsets.map(offset => ({ label: `hue offset ${offset} degrees`,
      ...describeColor(fromHsl(hsl.h + offset, hsl.s, hsl.l, color.a), background) }));
  return { operation, scheme: operation === 'palette' ? scheme : null,
    background: describeColor(background, background).hex, color: describeColor(color, background), palette };
}

/** authorize must reject on denial; it receives no input or options. */
export async function runMoreOfflineBrowser(
  toolCode: MoreOfflineCode, input: string, secondInput: string, options: MoreOfflineOptions,
  authorize: () => Promise<unknown>, signal?: AbortSignal,
): Promise<MoreOfflineResult> {
  // Snapshot controlled options before yielding to authority/other UI updates.
  const snapshot = { ...options };
  checkCancelled(signal);
  await authorize();
  checkCancelled(signal);
  const started = performance.now();
  let value: unknown, warning: string;
  if (toolCode === 'text-diff') {
    value = await diffText(input, secondInput, snapshot, signal);
    warning = 'Browser only. Equal/delete/insert are literal text. Character mode compares Unicode scalars, not graphemes; newline normalization is explicit.';
  } else if (toolCode === 'color') {
    value = convertColor(input, snapshot);
    warning = 'Numeric sRGB preview only. HSL to RGB rounds channels; HEX alpha quantizes to 8 bits. Contrast composites alpha onto the selected opaque background. Palette colors may repeat and do not certify accessibility.';
  } else return fail('ValidationFailed', 'Unsupported browser tool.');
  checkCancelled(signal);
  const output = JSON.stringify(value, null, 2);
  if (new TextEncoder().encode(output).length > 2 * 1024 * 1024)
    fail('OutputTooLarge', 'Tool output exceeds the 2 MiB limit.');
  return { toolCode, output, warning, errorPath: null, durationMilliseconds: Math.round(performance.now() - started) };
}
