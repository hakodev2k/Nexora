export type DirtyLeaveContinuation = () => void;
export type DirtyLeaveGuard = (continueAction: DirtyLeaveContinuation) => boolean;
const guards = new Map<symbol, { guard: DirtyLeaveGuard; priority: number }>();
export function registerDirtyLeaveGuard(guard: DirtyLeaveGuard, priority = 0) {
  const token = Symbol('dirty-leave'); guards.set(token, { guard, priority });
  return () => { guards.delete(token); };
}
export function attemptDirtyLeave(proceed: DirtyLeaveContinuation) {
  return [...guards.values()].reverse().sort((a, b) => b.priority - a.priority).some(({ guard }) => guard(proceed));
}
export function requestDirtyLeave(proceed: DirtyLeaveContinuation) { if (!attemptDirtyLeave(proceed)) proceed(); }
