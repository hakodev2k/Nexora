import { useCallback, useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { ActionDialog, registerDirtyLeaveGuard } from './App';
import { ResourceFormDialog } from './ResourceFormDialog';
import { SkillEvidencePanel } from './SkillEvidence';
import { skillMerge, skillMergePreview, skillMergeHistory, type SkillMergePreview, type SkillMergeHistory } from './skillApi';
import { skillCapabilities, skillGet, skillList, skillPreview, skillSave, skillSetProficiency, skillTransition, type Skill, type SkillOperation, type SkillPreview } from './skillApi';

type Draft = { title: string; level: string; description: string; category: string; lastUsed: string };
const emptyDraft: Draft = { title: '', level: '', description: '', category: '', lastUsed: '' };
const names: Record<SkillOperation, string> = { archive: 'Archive', unarchive: 'Unarchive', trash: 'Move to Trash', restore: 'Restore', purge: 'Delete permanently' };
const message = (e: unknown) => e instanceof Error ? e.message : 'Không thể hoàn tất thao tác.';

export function SkillScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const fieldPrefix = useId();
  const [caps, setCaps] = useState<Record<string, boolean>>({}); const [items, setItems] = useState<Skill[]>([]);
  const [filter, setFilter] = useState({ status: 'Active', query: '' }); const [query, setQuery] = useState('');
  const [cursor, setCursor] = useState<string | null>(null); const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null); const [formError, setFormError] = useState<string | null>(null);
  const [editor, setEditor] = useState<{ item?: Skill; mode: 'metadata' | 'proficiency'; baseline: string } | null>(null); const [draft, setDraft] = useState<Draft>(emptyDraft);
  const [pending, setPending] = useState<{ preview: SkillPreview; title: string } | null>(null);
  const [evidenceSkill, setEvidenceSkill] = useState<Skill | null>(null);
  const [merge, setMerge] = useState<{ source: Skill; targets: Skill[]; cursor: string | null; query: string } | null>(null);
  const [targetQuery, setTargetQuery] = useState(''); const [mergePreview, setMergePreview] = useState<SkillMergePreview | null>(null);
  const [mergeError, setMergeError] = useState<string | null>(null); const [mergeUncertain, setMergeUncertain] = useState(false);
  const [history, setHistory] = useState<{ skill: Skill; items: SkillMergeHistory[]; cursor: string | null } | null>(null);
  const mergeKey = useRef(crypto.randomUUID());
  const [conflict, setConflict] = useState(false); const [currentVersion, setCurrentVersion] = useState<Skill | null>(null); const [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const sequence = useRef(0); const key = useRef(crypto.randomUUID()); const inFlight = useRef(false);
  const fail = useCallback((e: unknown) => {
    const status = (e as { status?: number }).status; setError(message(e));
    if (status === 401 || status === 403) { sequence.current++; setLoading(false); setItems([]); setCaps({}); setCursor(null); setEditor(null); setDraft(emptyDraft); setPending(null); setMerge(null); setMergePreview(null); setMergeUncertain(false); setHistory(null); setEvidenceSkill(null); setLeave(null); setConflict(false); setCurrentVersion(null); if (status === 401) onAuthLost(); }
  }, [onAuthLost]);
  const load = useCallback(async () => {
    const stamp = ++sequence.current; setLoading(true); setError(null);
    try { const allowed = await skillCapabilities(); const page = allowed['learning.skill.read'] ? await skillList(filter.status, filter.query) : null;
      if (stamp !== sequence.current) return; setCaps(allowed); setItems(page?.items ?? []); setCursor(page?.nextCursor ?? null);
      if (!allowed['learning.skill.read']) { setEditor(null); setDraft(emptyDraft); setPending(null); setMerge(null); setMergePreview(null); setHistory(null); setFormError(null); setLeave(null); setConflict(false); setCurrentVersion(null); }
    } catch (e) { if (stamp === sequence.current) fail(e); } finally { if (stamp === sequence.current) setLoading(false); }
  }, [filter, fail]);
  useEffect(() => { void load(); return () => { sequence.current++; }; }, [load]);
  const dirty = mergeUncertain || (editor !== null && editor.baseline !== JSON.stringify(draft));
  useEffect(() => registerDirtyLeaveGuard(proceed => { if (busy && !editor) return true; if (!dirty && !busy) return false; setLeave({ proceed }); return true; }), [dirty, busy, editor]);
  useEffect(() => {
    if (!dirty && !busy) return;
    const prevent = (event: BeforeUnloadEvent) => { event.preventDefault(); event.returnValue = ''; };
    window.addEventListener('beforeunload', prevent); return () => window.removeEventListener('beforeunload', prevent);
  }, [dirty, busy]);
  const can = (action: string) => caps['learning.skill.' + action] === true;
  function begin(item?: Skill, mode: 'metadata' | 'proficiency' = 'metadata') {
    const next = item ? { title: item.title, level: item.level, description: item.description ?? '', category: item.category ?? '', lastUsed: item.lastUsed ?? '' } : emptyDraft;
    setDraft(next); setEditor({ item, mode, baseline: JSON.stringify(next) }); setFormError(null); setConflict(false); setCurrentVersion(null); key.current = crypto.randomUUID();
  }
  async function save(event: FormEvent) {
    event.preventDefault(); await persist();
  }
  async function persist() {
    if (!editor || inFlight.current || conflict) return false; inFlight.current = true; setBusy(true); setFormError(null);
    try { if (editor.mode === 'proficiency' && editor.item) await skillSetProficiency(editor.item, draft.level, key.current); else await skillSave({ ...draft, description: draft.description || null, category: draft.category || null, lastUsed: draft.lastUsed || null }, key.current, editor.item); setEditor(null); setDraft(emptyDraft); setCurrentVersion(null); await load(); return true; }
    catch (e) {
      setFormError(message(e)); const status = (e as { status?: number }).status;
      if (status === 412 && editor.item) { setConflict(true); try { setCurrentVersion(await skillGet(editor.item.id)); } catch (readError) { fail(readError); } }
      if ([401, 403].includes(status ?? 0)) fail(e); return false;
    }
    finally { inFlight.current = false; setBusy(false); }
  }
  async function resolveConflict(reapply: boolean) {
    if (!editor?.item || inFlight.current) return; inFlight.current = true; setBusy(true);
    try {
      const current = await skillGet(editor.item.id);
      if (reapply && current.etag !== currentVersion?.etag) { setCurrentVersion(current); setFormError('Revision lại thay đổi. So sánh bản hiện tại mới trước khi Reapply.'); return; }
      if (current.status === 'Archived' || current.status === 'Trash') { setFormError('Mục đã Archived hoặc Trash. Hủy bản nháp và khôi phục mục trước khi sửa.'); return; }
      const next = { title: current.title, level: current.level, description: current.description ?? '', category: current.category ?? '', lastUsed: current.lastUsed ?? '' };
      setEditor({ item: current, mode: editor.mode, baseline: JSON.stringify(next) }); if (!reapply) setDraft(next);
      setConflict(false); setCurrentVersion(null); setFormError(reapply ? 'Đã lấy revision mới. Kiểm tra bản nháp rồi chọn Save để áp dụng.' : null); key.current = crypto.randomUUID();
    } catch (e) { if ((e as { status?: number }).status === 404) { setEditor(null); setDraft(emptyDraft); } fail(e); }
    finally { inFlight.current = false; setBusy(false); }
  }
  async function preview(item: Skill, operation: SkillOperation) {
    if (inFlight.current) return; inFlight.current = true; setBusy(true);
    try { const result = await skillPreview(item, operation); key.current = crypto.randomUUID(); setPending({ preview: result, title: item.title }); }
    catch (e) { fail(e); } finally { inFlight.current = false; setBusy(false); }
  }
  async function commit() {
    if (!pending || inFlight.current) return false; inFlight.current = true; setBusy(true);
    try { await skillTransition(pending.preview, key.current); await load(); return true as const; }
    catch (e) { if ((e as { status?: number }).status === 412) { setPending(null); await load(); } fail(e); return { error: message(e) }; } finally { inFlight.current = false; setBusy(false); }
  }
  async function chooseMerge(source: Skill, append = false) {
    if (inFlight.current) return; inFlight.current = true; setBusy(true); setMergeError(null);
    try {
      const appliedQuery = append ? merge?.query ?? '' : merge ? targetQuery : '';
      const page = await skillList('Active', appliedQuery, append ? merge?.cursor ?? undefined : undefined);
      const targets = page.items.filter(item => item.id !== source.id && !item.mergedIntoId);
      setMerge(old => ({ source, targets: append ? [...(old?.targets ?? []), ...targets] : targets, cursor: page.nextCursor, query: appliedQuery }));
      if (!append) setMergePreview(null);
    } catch (e) { setMergeError(message(e)); fail(e); } finally { inFlight.current = false; setBusy(false); }
  }
  async function previewMerge(target: Skill) {
    if (!merge || inFlight.current) return; inFlight.current = true; setBusy(true); setMergeError(null);
    try { const result = await skillMergePreview(merge.source, target); mergeKey.current = crypto.randomUUID(); setMergePreview(result); }
    catch (e) { setMergeError(message(e)); if ((e as { status?: number }).status === 412) { setMerge(null); await load(); } fail(e); }
    finally { inFlight.current = false; setBusy(false); }
  }
  async function commitMerge() {
    if (!mergePreview || inFlight.current) return false; inFlight.current = true; setBusy(true); setMergeError(null);
    try {
      await skillMerge(mergePreview, mergeKey.current); setMergeUncertain(false); setMergePreview(null); setMerge(null); await load(); return true as const;
    } catch (e) {
      const status = (e as { status?: number }).status, code = (e as { code?: string }).code;
      const uncertain = !status || status >= 500 || code === 'RequestInProgress' || code === 'ReceiptUnavailable';
      setMergeUncertain(uncertain); setMergeError(message(e));
      if (!uncertain) { setMergePreview(null); setMerge(null); await load(); }
      fail(e); return { error: message(e) };
    } finally { inFlight.current = false; setBusy(false); }
  }
  async function readHistory(skill: Skill, append = false) {
    if (inFlight.current) return; inFlight.current = true; setBusy(true);
    try { const page = await skillMergeHistory(skill.id, append ? history?.cursor ?? undefined : undefined); setHistory(old => ({ skill, items: append ? [...(old?.items ?? []), ...page.items] : page.items, cursor: page.nextCursor })); }
    catch (e) { setHistory(null); fail(e); } finally { inFlight.current = false; setBusy(false); }
  }
  const blocked = loading || busy || evidenceSkill !== null || merge !== null || history !== null || mergePreview !== null;
  if (evidenceSkill) return <SkillEvidencePanel key={evidenceSkill.id} skill={evidenceSkill} canWrite={can('evidence')} onClose={() => { setEvidenceSkill(null); void load(); }} onFailure={e => { setEvidenceSkill(null); fail(e); }} />;
  return <section className="content-section" aria-labelledby="skill-title">
    <div className="content-heading"><div><p className="eyebrow">FX40 / LEARNING</p><h1 id="skill-title">Skills</h1><p>Kỹ năng cá nhân và mức độ tự đánh giá.</p></div><button className="secondary-button" disabled={blocked} onClick={() => void load()}>Tải lại</button></div>
    {error && <p role="alert">{error}</p>}{loading && <p role="status">Đang tải…</p>}
    <button className="primary-button" disabled={!can('create') || blocked} onClick={() => begin()}>New Skill</button>
    {can('read') && <>
      <form className="resource-form" onSubmit={e => { e.preventDefault(); setFilter(old => ({ ...old, query })); }}>
        <label>Title search<input value={query} maxLength={200} onChange={e => setQuery(e.target.value)} /></label><button disabled={blocked}>Search Skill</button>
        <div><label htmlFor={`${fieldPrefix}-status`}>Status</label><select id={`${fieldPrefix}-status`} value={filter.status} disabled={blocked} onChange={e => setFilter(old => ({ ...old, status: e.target.value }))}>{['Active', 'Archived', 'Trash'].map(status => <option key={status}>{status}</option>)}</select></div>
        <button type="button" disabled={blocked} onClick={() => { setQuery(''); setFilter({ status: 'Active', query: '' }); }}>Clear filters</button>
      </form>
      {!loading && !error && !items.length && <p>{filter.query ? 'Không có mục khớp bộ lọc.' : 'Chưa có mục trong trạng thái này.'}</p>}
      <div className="resource-cards">{items.map(item => {
        const operations: SkillOperation[] = item.mergedIntoId ? [] : item.status === 'Trash' ? ['restore', 'purge'] : item.status === 'Archived' ? ['unarchive', 'trash'] : ['archive', 'trash'];
        return <article key={item.id} className="resource-card"><div><h2>{item.title}</h2><p>{item.status} · Self-assessed: {item.level}</p>{item.category && <p>Category: {item.category}</p>}{item.lastUsed && <p>Last used: {item.lastUsed}</p>}{item.description && <p>{item.description}</p>}</div><div className="resource-actions">
          <button disabled={blocked} onClick={() => setEvidenceSkill(item)}>Evidence</button>
          <button disabled={blocked} onClick={() => void readHistory(item)}>Merge history</button>
          {item.mergedIntoId && <p>Merged into {item.mergedIntoId}. Original metadata and evidence retained read-only.</p>}
          {item.status === 'Active' && !item.mergedIntoId && <button disabled={!can('merge') || blocked} onClick={() => { setTargetQuery(''); void chooseMerge(item); }}>Merge into another Skill</button>}
          {item.status === 'Active' && <><button disabled={!can('update') || blocked} onClick={() => begin(item)}>Edit Skill</button><button disabled={!can('proficiency') || blocked} onClick={() => begin(item, 'proficiency')}>Assess proficiency</button></>}
          {operations.map(op => <button key={op} disabled={!can(op) || blocked} onClick={() => void preview(item, op)}>{names[op]}</button>)}
        </div></article>;
      })}</div>
      {cursor && <button disabled={blocked} onClick={async () => { const stamp = sequence.current; setLoading(true); try { const page = await skillList(filter.status, filter.query, cursor); if (stamp === sequence.current) { setItems(old => [...old, ...page.items]); setCursor(page.nextCursor); } } catch (e) { if (stamp === sequence.current) fail(e); } finally { if (stamp === sequence.current) setLoading(false); } }}>Load more Skill items</button>}
    </>}
    <ResourceFormDialog open={editor !== null && leave === null} title={editor?.mode === 'proficiency' ? 'Self-assessed proficiency' : editor?.item ? 'Edit Skill' : 'New Skill'} busy={busy} dirty={dirty} onClose={() => { setEditor(null); setDraft(emptyDraft); }}>
      <form onSubmit={save} className="resource-form">{formError && <p role="alert">{formError}</p>}
        {conflict && <div><p>Revision đã thay đổi. Reload current thay bản nháp; Reapply draft giữ bản nháp để bạn kiểm tra và Save lại.</p>{currentVersion && <div aria-label="Current Skill version"><h3>Current version</h3><p>{currentVersion.status} · {currentVersion.etag} · {new Date(currentVersion.updatedAt).toLocaleString()}</p><dl>{(['title', 'level', 'description', 'category', 'lastUsed'] as const).map(field => <div key={field}><dt>{field}</dt><dd>{currentVersion[field] ?? '—'}</dd></div>)}</dl></div>}<button type="button" disabled={busy} onClick={() => void resolveConflict(false)}>Reload current</button><button type="button" disabled={busy || !currentVersion} onClick={() => void resolveConflict(true)}>Reapply draft</button></div>}
        {(editor?.mode === 'proficiency' || !editor?.item) && <div><label htmlFor={`${fieldPrefix}-level`}>Self-assessed level</label><select id={`${fieldPrefix}-level`} required value={draft.level} onChange={e => { key.current = crypto.randomUUID(); setDraft(old => ({ ...old, level: e.target.value })); }}><option value="">Choose a level</option>{['Beginner', 'Intermediate', 'Advanced', 'Expert'].map(level => <option key={level}>{level}</option>)}</select></div>}
        {editor?.mode !== 'proficiency' && (['title', 'description', 'category', 'lastUsed'] as const).map((field, index) => <div key={field}><label htmlFor={`${fieldPrefix}-${field}`}>{['Title', 'Description', 'Category', 'Last used'][index]}</label>{field === 'description' ? <textarea id={`${fieldPrefix}-${field}`} maxLength={20000} value={draft[field]} onChange={e => { key.current = crypto.randomUUID(); setDraft(old => ({ ...old, [field]: e.target.value })); }} /> : <input id={`${fieldPrefix}-${field}`} required={field === 'title'} type={field === 'lastUsed' ? 'date' : 'text'} maxLength={200} value={draft[field]} onChange={e => { key.current = crypto.randomUUID(); setDraft(old => ({ ...old, [field]: e.target.value })); }} />}</div>)}
        <button className="primary-button" disabled={busy || conflict}>Save Skill</button>
      </form>
    </ResourceFormDialog>
    {leave && <ActionDialog title="Bỏ thay đổi chưa lưu?" description={mergeUncertain ? 'Kết quả merge chưa xác định. Rời trang không hủy thao tác đã có thể commit. Thử lại đúng request để đối chiếu trước khi rời.' : 'Bản nháp Skill chưa được lưu. Hủy để tiếp tục sửa.'} confirmLabel="Rời trang" confirmDisabled={busy} onClose={() => { if (!inFlight.current) setLeave(null); }} onConfirm={() => { if (inFlight.current) return false; const proceed = leave.proceed; setEditor(null); setDraft(emptyDraft); setMerge(null); setMergePreview(null); setMergeUncertain(false); setCurrentVersion(null); setLeave(null); proceed(); return true; }}>{!mergeUncertain && <button disabled={busy || conflict} onClick={async () => { const proceed = leave.proceed; if (await persist()) { setLeave(null); proceed(); } else setLeave(null); }}>Save and leave</button>}</ActionDialog>}
    <ResourceFormDialog open={merge !== null && mergePreview === null} title={`Merge · ${merge?.source.title ?? ''}`} busy={busy} dirty={false} onClose={() => { if (!inFlight.current) setMerge(null); }}>
      <p>Chọn Skill đích Active. Giữ tên, mô tả, last used và level của đích. Nguồn trở thành Archived chỉ đọc; evidence và lịch sử nguồn được giữ lại.</p>
      <form onSubmit={e => { e.preventDefault(); if (merge) void chooseMerge(merge.source); }}><label>Tìm Skill đích<input maxLength={200} value={targetQuery} disabled={busy} onChange={e => setTargetQuery(e.target.value)} /></label><button disabled={busy}>Search targets</button></form>
      {mergeError && <p role="alert">{mergeError}</p>}{busy && <p role="status">Đang tải…</p>}
      {!busy && !merge?.targets.length && <p>Không có Skill đích phù hợp. Đổi tìm kiếm hoặc tạo Skill trước.</p>}
      {merge?.targets.map(target => <article className="resource-card" key={target.id}><h3>{target.title}</h3><p>Self-assessed: {target.level} · {target.category ?? '—'}</p><button disabled={busy} onClick={() => void previewMerge(target)}>Preview merge into {target.title}</button></article>)}
      {merge?.cursor && <button disabled={busy} onClick={() => void chooseMerge(merge.source, true)}>Load more target Skills</button>}
    </ResourceFormDialog>
    {mergePreview && !leave && <ActionDialog title="Confirm Skill merge" description={<span>{mergePreview.source.title} → {mergePreview.target.title}. Nguồn sẽ Archived chỉ đọc, không có thao tác unmerge. Level đích giữ nguyên: {mergePreview.target.level}.</span>} confirmLabel={mergeUncertain ? 'Thử lại đúng request/mã cũ' : 'Confirm merge'} confirmDisabled={busy} onClose={() => { if (!inFlight.current && !mergeUncertain) setMergePreview(null); }} onConfirm={commitMerge}>
      <p>Source revision: {mergePreview.source.etag}; target revision: {mergePreview.target.etag}.</p>
      <p>{mergePreview.sourceEvidenceCount} evidence nguồn; {mergePreview.targetEvidenceCount} evidence đích. Thêm {mergePreview.addedEvidenceCount} liên kết; {mergePreview.overlapCount} liên kết trùng giữ note đích và note nguồn tại bản nguồn.</p>
      <p>Giữ metadata, evidence gốc, lịch sử và các liên kết khác tại nguồn. Lịch sử merge giữ cả hai bản trước merge và ngăn purge khi còn tham chiếu.</p>
      <p>Preview expires: {new Date(mergePreview.expiresAt).toLocaleString()}.</p>
      {mergeError && <p role="alert">{mergeError}</p>}{mergeUncertain && <p role="status">Chưa xác định kết quả. Giữ nguyên request/mã cũ để đối chiếu; không tạo merge mới.</p>}
    </ActionDialog>}
    <ResourceFormDialog open={history !== null} title={`Merge history · ${history?.skill.title ?? ''}`} busy={busy} dirty={false} onClose={() => { if (!inFlight.current) setHistory(null); }}>
      {!history?.items.length && <p>Chưa có lịch sử merge. Không tạo lại lịch sử trước khi tính năng được cài.</p>}
      {history?.items.map(item => <article className="resource-card" key={item.id}><h3>{item.source.title} → {item.target.title}</h3><p>{new Date(item.mergedAt).toLocaleString()} · {item.sourceEvidenceCount - item.overlapCount} evidence được thêm, {item.overlapCount} liên kết trùng.</p>
        <details><summary>Metadata trước merge</summary>{[item.source, item.target].map((snapshot, index) => <div key={index}><h4>{index ? 'Target' : 'Source'} · {snapshot.title}</h4><p>{snapshot.status} · Self-assessed: {snapshot.level} · Revision: {snapshot.etag}</p><p>{snapshot.category ?? '—'} · Last used: {snapshot.lastUsed ?? '—'}</p><p>{snapshot.description ?? '—'}</p></div>)}</details>
        <button disabled={busy} onClick={async () => { if (inFlight.current) return; inFlight.current = true; setBusy(true); try { const current = await skillGet(item.source.id); setHistory(null); setEvidenceSkill(current); } catch (e) { fail(e); } finally { inFlight.current = false; setBusy(false); } }}>Read preserved source evidence</button>
      </article>)}
      {history?.cursor && <button disabled={busy} onClick={() => void readHistory(history.skill, true)}>Load more merge history</button>}
    </ResourceFormDialog>
    {pending && <ActionDialog title={names[pending.preview.operation]} description={<span>{pending.title}. {pending.preview.operation === 'purge' ? 'Xóa vĩnh viễn; không thể hoàn tác.' : 'Giữ trạng thái trước đó để khôi phục.'}{pending.preview.referenceCount > 0 && ` ${pending.preview.referenceCount} references giữ mục này.`}</span>} confirmLabel={names[pending.preview.operation]} confirmDisabled={pending.preview.operation === 'purge' && pending.preview.referenceCount > 0} onClose={() => setPending(null)} onConfirm={commit} />}
  </section>;
}
