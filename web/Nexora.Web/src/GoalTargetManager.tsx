import { useEffect, useRef, useState, type FormEvent } from 'react';
import {
  changeGoalTarget, createBooleanGoalTarget, createIdempotencyKey, createNumericGoalTarget,
  previewGoalTargetChange, recordBooleanGoalProgress, recordGoalProgress,
  type GoalDetail, type GoalTargetChangePreview, type GoalTargetRecord
} from './api';
import { exactGoalDecimal } from './goalNumbers';
import { ResourceFormDialog, useResourceDialog } from './ResourceFormDialog';

type Mode = 'CreateNumeric' | 'CreateBoolean' | 'Update' | 'Remove' | 'Progress';
type Draft = { title: string; initial: string; current: string; target: string; checked: string; note: string };
const empty = (): Draft => ({ title: '', initial: '0', current: '0', target: '', checked: 'false', note: '' });
const percent = (ratio: number) => `${(Math.max(0, Math.min(1, ratio)) * 100).toFixed(1)}%`;

export function GoalTargetManager({ detail, busy, uncertain, error, onCommand, onError }: {
  detail: GoalDetail; busy: boolean; uncertain: boolean; error?: string;
  onCommand: (send: () => ReturnType<typeof createNumericGoalTarget>) => Promise<boolean>;
  onError: (error: unknown) => void;
}) {
  const [mode, setMode] = useState<Mode>('CreateNumeric'), [target, setTarget] = useState<GoalTargetRecord | null>(null);
  const [revision, setRevision] = useState(''), [draft, setDraft] = useState<Draft>(empty);
  const [preview, setPreview] = useState<GoalTargetChangePreview | null>(null);
  const [previewBusy, setPreviewBusy] = useState(false), [localError, setLocalError] = useState<string | null>(null);
  const flight = useRef(false), generation = useRef(0), abort = useRef<AbortController | null>(null);
  const dialog = useResourceDialog(draft, setDraft);
  const editable = ['Draft', 'Active'].includes(detail.goal.status);
  const kind = mode === 'CreateNumeric' ? 'Numeric' : mode === 'CreateBoolean' ? 'Boolean' : target?.kind;
  const blocked = busy || previewBusy || uncertain;
  useEffect(() => { if (uncertain) dialog.finish(); }, [uncertain]);
  useEffect(() => () => { ++generation.current; abort.current?.abort(); }, []);

  function begin(nextMode: Mode, nextTarget: GoalTargetRecord | null = null) {
    if (blocked || flight.current) return;
    ++generation.current; abort.current?.abort();
    const next = nextTarget ? {
      title: nextTarget.title, initial: nextTarget.initialValueExact ?? '', current: nextTarget.currentValueExact ?? '',
      target: nextTarget.targetValueExact ?? '', checked: nextTarget.booleanValue ? 'true' : 'false', note: ''
    } : empty();
    setMode(nextMode); setTarget(nextTarget); setRevision(detail.goal.etag); setDraft(next);
    setPreview(null); setLocalError(null); dialog.begin(next);
  }

  function cancel() {
    if (blocked || flight.current) return;
    ++generation.current; abort.current?.abort(); setPreview(null); setLocalError(null); dialog.cancel();
  }

  function validation(): string | null {
    if (mode === 'Remove') return null;
    if (!draft.title.trim() || draft.title.trim().length > 200) return 'Tên target phải từ 1 đến 200 ký tự.';
    if (kind === 'Numeric') {
      if (mode === 'Progress') return exactGoalDecimal(draft.current.trim()) === null ? 'Nhập giá trị có tối đa 20 chữ số nguyên và 8 chữ số thập phân.' : null;
      const initial = exactGoalDecimal(draft.initial.trim()), end = exactGoalDecimal(draft.target.trim());
      if (initial === null || end === null || end <= initial) return 'Giá trị đích phải lớn hơn ban đầu; tối đa 20 chữ số nguyên và 8 chữ số thập phân.';
      if (mode === 'CreateNumeric' && exactGoalDecimal(draft.current.trim()) === null) return 'Giá trị hiện tại không hợp lệ.';
    }
    return null;
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (blocked || flight.current || !editable) return;
    const problem = validation();
    if (problem) { setLocalError(problem); return; }
    setLocalError(null);
    const goalId = detail.goal.id, targetId = target?.id, etag = revision;
    const title = draft.title.trim(), initial = draft.initial.trim(), current = draft.current.trim(), end = draft.target.trim();
    const checked = draft.checked === 'true', note = draft.note.trim() || null;
    if ((mode === 'Update' || mode === 'Remove') && !preview && targetId) {
      const epoch = ++generation.current;
      const controller = new AbortController(); abort.current = controller;
      flight.current = true; setPreviewBusy(true);
      try {
        const result = await previewGoalTargetChange(goalId, targetId, etag, mode === 'Remove'
          ? { mode: 'Remove' }
          : kind === 'Numeric' ? { mode: 'Update', title, initialValue: initial, targetValue: end }
            : { mode: 'Update', title }, controller.signal);
        if (generation.current === epoch) setPreview(result);
      } catch (failure) {
        if (generation.current === epoch && !controller.signal.aborted) onError(failure);
      } finally {
        flight.current = false;
        if (generation.current === epoch) setPreviewBusy(false);
      }
      return;
    }
    const key = createIdempotencyKey();
    let send: (() => ReturnType<typeof createNumericGoalTarget>) | null = null;
    if (mode === 'CreateNumeric') send = () => createNumericGoalTarget(goalId, etag, title, initial, current, end, key);
    else if (mode === 'CreateBoolean') send = () => createBooleanGoalTarget(goalId, etag, title, checked, key);
    else if (mode === 'Progress' && targetId) send = kind === 'Numeric'
      ? () => recordGoalProgress(goalId, targetId, etag, current, note, key)
      : () => recordBooleanGoalProgress(goalId, targetId, etag, checked, note, key);
    else if ((mode === 'Update' || mode === 'Remove') && targetId && preview) {
      const frozenMode = mode, token = preview.previewToken;
      send = () => changeGoalTarget(goalId, targetId, etag, frozenMode, token, key);
    }
    if (!send) return;
    flight.current = true;
    try { if (await onCommand(send)) { dialog.finish(); setPreview(null); } }
    finally { flight.current = false; }
  }

  const title = mode === 'Remove' ? 'Bỏ target khỏi Goal' : mode === 'Update' ? 'Sửa định nghĩa target'
    : mode === 'Progress' ? 'Ghi tiến độ target' : `Thêm target ${kind === 'Numeric' ? 'số' : 'yes/no'}`;
  return <section className="form-panel" aria-labelledby="goal-targets-title">
    <div className="section-heading"><h3 id="goal-targets-title">Targets</h3>
      {editable && <div className="form-actions"><button className="secondary-button" type="button" disabled={blocked} onClick={() => begin('CreateNumeric')}>Thêm Numeric</button><button className="secondary-button" type="button" disabled={blocked} onClick={() => begin('CreateBoolean')}>Thêm yes/no</button></div>}
    </div>
    {detail.targets.length === 0 ? <p>Chưa có target (No targets) · 0.0%</p> : <div className="resource-cards">
      {detail.targets.map(item => <article className="resource-card" key={item.id}>
        <strong>{item.title}</strong><p>{item.kind} · {percent(item.progress)}</p>
        {item.kind === 'Numeric' ? <p>Ban đầu: {item.initialValueExact} · Hiện tại: {item.currentValueExact} · Đích: {item.targetValueExact}</p>
          : item.kind === 'Boolean' ? <p>{item.booleanValue ? 'Có' : 'Chưa'}</p> : <p>Tiến độ đọc từ nguồn.</p>}
        {editable && ['Numeric', 'Boolean'].includes(item.kind) && <div className="resource-actions">
          <button className="primary-button" type="button" disabled={blocked} onClick={() => begin('Progress', item)}>Ghi tiến độ</button>
          <button className="secondary-button" type="button" disabled={blocked} onClick={() => begin('Update', item)}>Sửa định nghĩa</button>
          <button className="secondary-button" type="button" disabled={blocked} onClick={() => begin('Remove', item)}>Bỏ target</button>
        </div>}
      </article>)}
    </div>}
    <ResourceFormDialog open={dialog.open} title={title} busy={busy || previewBusy} dirty={dialog.dirty || preview !== null} onClose={cancel}>
      {(localError || error) && <p role="alert">{localError || error}</p>}
      <form className="resource-form" onSubmit={submit}>
        {preview ? <div aria-live="polite">
          <p><strong>{preview.targetTitle}</strong> · {preview.kind}</p>
          {mode === 'Update' && <p>Tên mới: {preview.proposedTitle}{preview.kind === 'Numeric' && <> · Ban đầu: {preview.initialValueExact} · Đích: {preview.targetValueExact}</>}</p>}
          <p>Số target: {preview.beforeCount} → {preview.afterCount}</p><p>Tiến độ Goal: {percent(preview.beforeProgress)} → {percent(preview.afterProgress)}</p>
          {mode === 'Remove' && <p>Target sẽ không còn trong mẫu số. Lịch sử tiến độ và audit được giữ lại; không xóa nguồn liên kết.</p>}
          <p>Trạng thái Goal không tự thay đổi.</p>
          <div className="form-actions"><button className="secondary-button" type="button" disabled={blocked} onClick={() => { setPreview(null); setLocalError(null); }}>Quay lại</button><button className="primary-button" type="submit" disabled={blocked}>{busy ? 'Đang lưu…' : mode === 'Remove' ? 'Xác nhận bỏ target' : 'Xác nhận thay đổi'}</button></div>
        </div> : <>
          {mode === 'Remove' ? <p>Xem ảnh hưởng trước khi bỏ <strong>{target?.title}</strong> khỏi Goal. Các lần ghi tiến độ đã lưu sẽ được giữ lại.</p>
            : mode === 'Progress' ? <p><strong>{target?.title}</strong> · Giá trị cũ: {kind === 'Numeric' ? target?.currentValueExact : target?.booleanValue ? 'Có' : 'Chưa'}</p>
              : <div className="field-group"><label htmlFor="goal-target-manager-title">Tên target</label><input id="goal-target-manager-title" value={draft.title} maxLength={200} required onChange={event => setDraft({ ...draft, title: event.target.value })} /></div>}
          {mode !== 'Remove' && kind === 'Numeric' && <div className="form-grid">
            {mode !== 'Progress' && <div className="field-group"><label htmlFor="goal-target-manager-initial">Giá trị ban đầu</label><input id="goal-target-manager-initial" type="text" inputMode="decimal" required value={draft.initial} onChange={event => setDraft({ ...draft, initial: event.target.value })} /></div>}
            {mode !== 'Update' && <div className="field-group"><label htmlFor="goal-target-manager-current">{mode === 'Progress' ? 'Giá trị mới' : 'Giá trị hiện tại'}</label><input id="goal-target-manager-current" type="text" inputMode="decimal" required value={draft.current} onChange={event => setDraft({ ...draft, current: event.target.value })} /></div>}
            {mode !== 'Progress' && <div className="field-group"><label htmlFor="goal-target-manager-end">Giá trị đích</label><input id="goal-target-manager-end" type="text" inputMode="decimal" required value={draft.target} onChange={event => setDraft({ ...draft, target: event.target.value })} /></div>}
          </div>}
          {kind === 'Boolean' && ['CreateBoolean', 'Progress'].includes(mode) && <div className="field-group"><label htmlFor="goal-target-manager-checked">{mode === 'Progress' ? 'Giá trị mới' : 'Giá trị ban đầu'}</label><select id="goal-target-manager-checked" value={draft.checked} onChange={event => setDraft({ ...draft, checked: event.target.value })}><option value="false">Chưa · 0.0%</option><option value="true">Có · 100.0%</option></select></div>}
          {mode === 'Progress' && <div className="field-group"><label htmlFor="goal-target-manager-note">Ghi chú (tùy chọn)</label><textarea id="goal-target-manager-note" value={draft.note} maxLength={2000} onChange={event => setDraft({ ...draft, note: event.target.value })} /></div>}
          <p className="muted">Loại target giữ nguyên. Sửa định nghĩa không ghi đè giá trị tiến độ hiện tại. Đạt 100% không tự hoàn thành Goal.</p>
          <div className="form-actions"><button className="secondary-button" type="button" disabled={blocked} onClick={cancel}>Hủy</button><button className="primary-button" type="submit" disabled={blocked}>{blocked ? 'Đang xử lý…' : mode === 'Update' || mode === 'Remove' ? 'Xem ảnh hưởng' : 'Lưu'}</button></div>
        </>}
      </form>
    </ResourceFormDialog>
  </section>;
}
