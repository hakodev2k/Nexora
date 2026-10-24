import { test } from '@playwright/test';

// Source-defined stage identifiers only; never propagate an assertion call log.
export type SafeStage = 'IDENTITY_REGISTER_VERIFY_LOGIN' | 'IDENTITY_HOME' | 'IDENTITY_SEARCH' | 'IDENTITY_SNIPPET' | 'IDENTITY_SECURITY' | 'IDENTITY_REFLOW' | 'RESET_LOGOUT' | 'RESET_REQUEST' | 'RESET_COMMIT_LOGIN' | 'GOAL_LOGIN' | 'GOAL_CREATE' | 'GOAL_PROGRESS' | 'GOAL_LIFECYCLE' | 'GOAL_PERSISTENCE';
export async function safeStep<T>(code: SafeStage, action: () => Promise<T>): Promise<T> {
  return test.step(code, async () => {
    try { return await action(); }
    catch { throw new Error('NEXORA_E2E_STEP[' + code + ']'); }
  });
}
