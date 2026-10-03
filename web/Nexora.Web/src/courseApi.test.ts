import { describe, expect, it } from 'vitest';
import { fractionToPercent, percentToFraction } from './courseApi';

describe('owner percentage at SQL decimal boundary', () => {
  it('preserves smallest supported value and an eight-place fraction without binary floating point', () => {
    expect(percentToFraction('0.000001')).toBe('0.00000001');
    expect(percentToFraction('12.345678')).toBe('0.12345678');
    expect(fractionToPercent('0.12345678')).toBe('12.345678');
    expect(percentToFraction('100.000000')).toBe('1');
    expect(fractionToPercent('1')).toBe('100');
    expect(fractionToPercent(null)).toBe('');
  });
  it('rejects a value that would round and progress outside owner range', () => {
    for (const input of ['0.0000001', '100.000001', '101', '-1', '1e2', '12,5']) expect(() => percentToFraction(input)).toThrow();
  });
});
