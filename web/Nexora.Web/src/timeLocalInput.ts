// Time form boundary only: SQL/API retain up to seven fractional digits. The
// browser editor displays milliseconds, so unchanged known instants stay raw.
export const browserTimeZone = () => Intl.DateTimeFormat().resolvedOptions().timeZone;
const pattern = /^(\d{4}-\d{2}-\d{2})T(\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{1,3}))?)?$/;
function normalize(value: string) {
  const match = pattern.exec(value);
  if (!match || match[1].startsWith('0000')) throw new Error('Nhập ngày và giờ hợp lệ từ năm 0001 đến 9999.');
  const normalized = `${match[1]}T${match[2]}:${match[3]}:${match[4] ?? '00'}.${(match[5] ?? '').padEnd(3, '0')}`;
  const utc = new Date(normalized + 'Z');
  if (!Number.isFinite(utc.getTime()) || utc.toISOString().slice(0, 23) !== normalized) throw new Error('Ngày hoặc giờ không hợp lệ. Kiểm tra lại ngày, giờ và phút.');
  return normalized;
}
function formatter(zone: string) {
  return new Intl.DateTimeFormat('en-GB', { timeZone: zone, calendar: 'gregory', numberingSystem: 'latn', era: 'short', year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit', fractionalSecondDigits: 3, hourCycle: 'h23' });
}
function localValue(at: number, format: Intl.DateTimeFormat) {
  const parts = format.formatToParts(at); const get = (kind: string) => parts.find(p => p.type === kind)?.value ?? '';
  if (get('era') !== 'AD' || Number(get('year')) > 9999) return '';
  return `${get('year').padStart(4, '0')}-${get('month')}-${get('day')}T${get('hour')}:${get('minute')}:${get('second')}.${get('fractionalSecond')}`;
}
export function timeInstantToLocal(instant: string, zone = browserTimeZone()) {
  const result = localValue(Date.parse(instant), formatter(zone));
  if (!result) throw new Error('Ngày trong múi giờ trình duyệt nằm ngoài khoảng được hỗ trợ.');
  return result;
}
export function timeLocalToInstant(value: string, original?: string | null, zone = browserTimeZone()): string {
  const normalized = normalize(value), format = formatter(zone);
  if (original && localValue(Date.parse(original), format) === normalized) return original;
  const naiveUtc = Date.parse(normalized + 'Z'), offsets = new Set<number>();
  // Read the zone's actual offsets on both sides of nearby transitions. Match
  // candidates back to the full local date/time rather than letting Date guess
  // a gap or a fold. This includes half-hour and date-line transitions.
  for (let hours = -48; hours <= 48; hours++) {
    const sample = naiveUtc + hours * 3600000, local = localValue(sample, format);
    if (local) offsets.add(Date.parse(local + 'Z') - sample);
  }
  const candidates = [...offsets].map(offset => naiveUtc - offset).filter(at => localValue(at, format) === normalized);
  if (!candidates.length) throw new Error('Giờ này không tồn tại trong múi giờ trình duyệt do đổi giờ. Chọn giờ khác.');
  if (candidates.length !== 1) throw new Error('Giờ này bị lặp do đổi giờ trong múi giờ trình duyệt. Chọn một giờ không bị lặp; thời điểm đã lưu không đổi nếu bạn chỉ sửa mô tả.');
  const instant = new Date(candidates[0]).toISOString();
  if (!/^\d{4}-/.test(instant) || instant.startsWith('0000')) throw new Error('Thời điểm UTC nằm ngoài năm 0001 đến 9999. Chọn ngày khác trong múi giờ này.');
  return instant;
}
export function timeInstantTicks(instant: string): bigint {
  const milliseconds = Date.parse(instant);
  if (!Number.isFinite(milliseconds)) throw new Error('Thời điểm không hợp lệ.');
  const fraction = /\.(\d+)(?:Z|[+-]\d{2}:\d{2})$/i.exec(instant)?.[1] ?? '';
  return BigInt(milliseconds) * 10000n + BigInt(fraction.padEnd(7, '0').slice(3, 7));
}
