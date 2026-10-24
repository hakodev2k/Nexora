export type TextChange = { kind: 'same' | 'removed' | 'added'; text: string };
export type TextComparison = { changes: TextChange[]; simplified: boolean };

/** Exact source chunks, including CRLF and the presence/absence of a final newline.
 * LCS is bounded; large snapshots use an honest replacement view instead. */
export function compareDocumentText(before: string, after: string): TextComparison {
  if (before === after) return { changes: before ? [{ kind: 'same', text: before }] : [], simplified: false };
  const replacement = (): TextComparison => ({ simplified: true, changes: [
    ...(before ? [{ kind: 'removed' as const, text: before }] : []),
    ...(after ? [{ kind: 'added' as const, text: after }] : [])
  ] });
  if (before.length + after.length > 100_000) return replacement();
  const lines = (text: string) => text.match(/[^\n]*\n|[^\n]+$/g) ?? [];
  const left = lines(before), right = lines(after);
  if (left.length > 400 || right.length > 400 || left.length * right.length > 160_000) return replacement();
  const width = right.length + 1;
  const lcs = new Uint16Array((left.length + 1) * width);
  for (let i = left.length - 1; i >= 0; i--) {
    for (let j = right.length - 1; j >= 0; j--) {
      lcs[i * width + j] = left[i] === right[j]
        ? lcs[(i + 1) * width + j + 1] + 1
        : Math.max(lcs[(i + 1) * width + j], lcs[i * width + j + 1]);
    }
  }
  const changes: TextChange[] = [];
  function add(kind: TextChange['kind'], text: string) {
    const previous = changes.at(-1);
    if (previous?.kind === kind) previous.text += text;
    else changes.push({ kind, text });
  }
  let i = 0, j = 0;
  while (i < left.length || j < right.length) {
    if (i < left.length && j < right.length && left[i] === right[j]) { add('same', left[i++]); j++; }
    else if (i < left.length && (j === right.length || lcs[(i + 1) * width + j] >= lcs[i * width + j + 1])) add('removed', left[i++]);
    else add('added', right[j++]);
  }
  return { changes, simplified: false };
}
