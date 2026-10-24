import { expect, test } from 'vitest';
import { compareDocumentText } from './documentTextDiff';

const pairs: [string, string][] = [
  ['', ''], ['', 'new'], ['old', ''], ['same\n', 'same\n'],
  ['a\nb\n', 'a\nc\n'], ['a\r\nb\r\n', 'a\nb\n'],
  ['line\n', 'line'], ['line', 'line\n'],
  ['Xin chào 👩🏽‍💻\n<script>bad</script>', 'Xin chào 世界\n<img src=x>'],
  ['a\na\nb\n', 'b\na\na\n']
];
test.each(pairs)('source can be reconstructed byte-for-character from chunks %#', (before, after) => {
  const result = compareDocumentText(before, after);
  expect(result.changes.filter(x => x.kind !== 'added').map(x => x.text).join('')).toBe(before);
  expect(result.changes.filter(x => x.kind !== 'removed').map(x => x.text).join('')).toBe(after);
  expect(compareDocumentText(before, after)).toEqual(result);
});

test('untrusted very long source uses bounded replacement without losing content', () => {
  const before = 'x'.repeat(100_001), after = 'y'.repeat(100_001);
  expect(compareDocumentText(before, after)).toEqual({ simplified: true, changes: [{ kind: 'removed', text: before }, { kind: 'added', text: after }] });
});
test('many short lines cannot cause an unbounded quadratic diff', () => {
  const before = 'a\n'.repeat(401), after = 'b\n'.repeat(401);
  expect(compareDocumentText(before, after).simplified).toBe(true);
  expect(compareDocumentText(before, after).changes).toHaveLength(2);
});
