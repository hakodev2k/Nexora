// Compare scaled integers; never round a user-entered decimal through JS Number.
export function exactGoalDecimal(text: string): bigint | null {
  if (!/^-?\d{1,20}(?:\.\d{1,8})?$/.test(text)) return null;
  const negative = text.startsWith('-'), [whole, fraction = ''] = (negative ? text.slice(1) : text).split('.');
  const scaled = BigInt(whole) * 100000000n + BigInt(fraction.padEnd(8, '0'));
  return negative ? -scaled : scaled;
}
