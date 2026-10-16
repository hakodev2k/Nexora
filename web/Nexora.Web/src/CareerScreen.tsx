import { useCallback, useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { ActionDialog, registerDirtyLeaveGuard } from './App';
import { ResourceFormDialog } from './ResourceFormDialog';
import { careerCapabilities, careerCompanies, careerCompany, careerJobs, careerJob, careerHistory, careerStages, saveCompany, saveJob, changeJobStage, previewJob, jobLifecycle, previewCompanyMerge, mergeCompany, returnToProgress, type CareerCompany, type CareerJob, type CompanyMetadata, type JobMetadata, type JobFilter, type HistoryFilter, type JobEvent, type JobPreview, type MergePreview } from './careerApi';

type Draft = Record<keyof JobMetadata | 'industry' | 'stage' | 'reason', string>;
const blank: Draft = { title: '', companyId: '', url: '', location: '', workMode: '', employmentType: '', salaryText: '', salaryMin: '', salaryMax: '', currency: '', description: '', notes: '', source: '', discoveredOn: '', appliedOn: '', industry: '', stage: '', reason: '' };
const emptyFilter: JobFilter = { state: 'Active', stage: '', companyId: '', query: '', location: '', dateField: '', from: '', to: '' };
const emptyHistory: HistoryFilter = { action: '', from: '', to: '' };
const operationNames = { trash: 'Move to Trash', restore: 'Restore', purge: 'Delete permanently' };
type Editor = { kind: 'company'; item?: CareerCompany; baseline: string } | { kind: 'job'; item?: CareerJob; baseline: string } | { kind: 'stage'; item: CareerJob; baseline: string };
const draftOf = (item?: CareerCompany | CareerJob): Draft => item ? { ...blank, ...Object.fromEntries(Object.entries(item.metadata).map(([k, v]) => [k, v ?? ''])), stage: 'stage' in item ? item.stage : '' } : { ...blank };
const textError = (e: unknown) => e instanceof Error ? e.message : 'Không thể hoàn tất thao tác.';

export function CareerScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const prefix = useId(); const [caps, setCaps] = useState<Record<string, boolean>>({});
  const [companies, setCompanies] = useState<CareerCompany[]>([]); const [jobs, setJobs] = useState<CareerJob[]>([]);
  const [companyCursor, setCompanyCursor] = useState<string | null>(null); const [jobCursor, setJobCursor] = useState<string | null>(null);
  const [companyQuery, setCompanyQuery] = useState(''); const [companyApplied, setCompanyApplied] = useState('');
  const [filter, setFilter] = useState(emptyFilter); const [applied, setApplied] = useState(emptyFilter); const [view, setView] = useState<'Table' | 'Kanban'>('Table');
  const [picker, setPicker] = useState<CareerCompany[]>([]); const [pickerCursor, setPickerCursor] = useState<string | null>(null); const [pickerQuery, setPickerQuery] = useState(''); const [pickerApplied, setPickerApplied] = useState('');
  const [editor, setEditor] = useState<Editor | null>(null); const [draft, setDraft] = useState(blank); const [confirmed, setConfirmed] = useState(false); const [reopen, setReopen] = useState(false);
  const [conflict, setConflict] = useState<CareerCompany | CareerJob | null>(null);
  const [merge, setMerge] = useState<{ source: CareerCompany; targetId: string; preview: MergePreview | null } | null>(null); const [keepTarget, setKeepTarget] = useState(false);
  const [history, setHistory] = useState<{ id: string; title: string; items: JobEvent[]; cursor: string | null } | null>(null); const [historyFilter, setHistoryFilter] = useState(emptyHistory); const [historyApplied, setHistoryApplied] = useState(emptyHistory);
  const [pending, setPending] = useState<{ preview: JobPreview; title: string } | null>(null); const [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const [busy, setBusy] = useState(false); const [loading, setLoading] = useState(true); const [error, setError] = useState<string | null>(null); const [formError, setFormError] = useState<string | null>(null);
  const flight = useRef(false); const sequence = useRef(0); const key = useRef(crypto.randomUUID());
  const can = (action: string) => caps['career.' + action] === true;
  const clearEditor = () => { setEditor(null); setDraft(blank); setConflict(null); setFormError(null); setPicker([]); setPickerCursor(null); };
  const fail = useCallback((e: unknown) => {
    setError(textError(e)); const status = (e as { status?: number }).status;
    if (status === 401 || status === 403) { sequence.current++; setLoading(false); setBusy(false); setCaps({}); setCompanies([]); setJobs([]); setCompanyCursor(null); setJobCursor(null); setHistory(null); setEditor(null); setDraft(blank); setConflict(null); setPicker([]); setPickerCursor(null); setPending(null); setMerge(null); setLeave(null); setFormError(null); if (status === 401) onAuthLost(); }
  }, [onAuthLost]);
  const load = useCallback(async () => {
    const stamp = ++sequence.current; setLoading(true); setError(null); setHistory(null);
    try {
      const allowed = await careerCapabilities();
      const [companyPage, jobPage] = await Promise.all([allowed['career.company.read'] ? careerCompanies(companyApplied) : null, allowed['career.job.read'] ? careerJobs(applied) : null]);
      if (stamp !== sequence.current) return; setCaps(allowed); setCompanies(companyPage?.items ?? []); setCompanyCursor(companyPage?.nextCursor ?? null); setJobs(jobPage?.items ?? []); setJobCursor(jobPage?.nextCursor ?? null);
    } catch (e) { if (stamp === sequence.current) fail(e); } finally { if (stamp === sequence.current) setLoading(false); }
  }, [companyApplied, applied, fail]);
  useEffect(() => { void load(); return () => { sequence.current++; }; }, [load]);
  const dirty = !!editor && editor.baseline !== JSON.stringify(draft); const mergeDirty = !!merge && (!!merge.targetId || !!merge.preview);
  useEffect(() => registerDirtyLeaveGuard(proceed => { if (!dirty && !mergeDirty && !busy) return false; setLeave({ proceed }); return true; }), [dirty, mergeDirty, busy]);
  useEffect(() => { if (!dirty && !mergeDirty && !busy) return; const prevent = (e: BeforeUnloadEvent) => { e.preventDefault(); e.returnValue = ''; }; window.addEventListener('beforeunload', prevent); return () => window.removeEventListener('beforeunload', prevent); }, [dirty, mergeDirty, busy]);
  async function run(work: () => Promise<void>) { if (flight.current) return; flight.current = true; setBusy(true); setError(null); try { await work(); } catch (e) { fail(e); } finally { flight.current = false; setBusy(false); } }
  async function loadPicker(query = '', cursor?: string) {
    if (!can('company.read')) { setPicker([]); setPickerCursor(null); return; }
    const page = await careerCompanies(query, cursor, true); setPicker(old => cursor ? [...old, ...page.items] : page.items); setPickerCursor(page.nextCursor); setPickerApplied(query);
  }
  async function begin(kind: Editor['kind'], item?: CareerCompany | CareerJob) {
    await run(async () => {
      if (kind !== 'stage') { setPickerQuery(''); await loadPicker(); }
      const next = draftOf(item); const baseline = JSON.stringify(next);
      setDraft(next); setEditor(kind === 'company' ? { kind, item: item as CareerCompany | undefined, baseline } : kind === 'job' ? { kind, item: item as CareerJob | undefined, baseline } : { kind, item: item as CareerJob, baseline });
      setConfirmed(false); setReopen(false); setConflict(null); setFormError(null); key.current = crypto.randomUUID();
    });
  }
  function change(field: keyof Draft, value: string) { key.current = crypto.randomUUID(); setDraft(old => ({ ...old, [field]: value })); }
  function companyMetadata(): CompanyMetadata { return { title: draft.title, url: draft.url || null, industry: draft.industry || null, location: draft.location || null, notes: draft.notes || null }; }
  function jobMetadata(): JobMetadata { return { title: draft.title, companyId: draft.companyId || null, url: draft.url || null, location: draft.location || null, workMode: draft.workMode || null, employmentType: draft.employmentType || null, salaryText: draft.salaryText || null, salaryMin: draft.salaryMin || null, salaryMax: draft.salaryMax || null, currency: draft.currency || null, description: draft.description || null, notes: draft.notes || null, source: draft.source || null, discoveredOn: draft.discoveredOn || null, appliedOn: draft.appliedOn || null }; }
  async function persist() {
    if (!editor || flight.current || conflict) return false; flight.current = true; setBusy(true); setFormError(null);
    try {
      if (editor.kind === 'company') await saveCompany(companyMetadata(), key.current, editor.item);
      else if (editor.kind === 'job') await saveJob(jobMetadata(), draft.stage, key.current, editor.item);
      else await changeJobStage(editor.item, draft.stage, draft.reason || null, confirmed, reopen, key.current);
      clearEditor(); await load(); return true;
    } catch (e) {
      setFormError(textError(e)); const status = (e as { status?: number }).status;
      if (status === 412 && editor.item) { try { setConflict(editor.kind === 'company' ? await careerCompany(editor.item.id) : await careerJob(editor.item.id)); } catch (readError) { fail(readError); } }
      if (status === 401 || status === 403) fail(e); return false;
    } finally { flight.current = false; setBusy(false); }
  }
  async function resolve(reapply: boolean) {
    if (!editor?.item || !conflict) return;
    await run(async () => {
      const current = editor.kind === 'company' ? await careerCompany(editor.item!.id) : await careerJob(editor.item!.id);
      if (reapply && current.etag !== conflict.etag) { setConflict(current); setFormError('Revision lại thay đổi. So sánh bản hiện tại trước khi Reapply.'); return; }
      if ('isTrash' in current ? current.isTrash : !!current.mergedIntoId) { setFormError('Khôi phục Job hoặc chọn Company chưa gộp trước khi sửa.'); return; }
      const next = draftOf(current); setEditor({ ...editor, item: current, baseline: JSON.stringify(next) } as Editor); if (!reapply) setDraft(next);
      setConflict(null); setConfirmed(false); setReopen(false); setFormError(reapply ? 'Đã lấy revision mới. Kiểm tra bản nháp rồi Save.' : null); key.current = crypto.randomUUID();
    });
  }
  async function openHistory(item: CareerJob) { await run(async () => { const page = await careerHistory(item.id, emptyHistory); setHistoryFilter(emptyHistory); setHistoryApplied(emptyHistory); setHistory({ id: item.id, title: item.metadata.title, items: page.items, cursor: page.nextCursor }); }); }
  async function life(item: CareerJob, operation: JobPreview['operation']) { await run(async () => { const preview = await previewJob(item, operation); key.current = crypto.randomUUID(); setPending({ preview, title: item.metadata.title }); }); }
  async function commitLife() {
    if (!pending || flight.current) return false; flight.current = true; setBusy(true);
    try { await jobLifecycle(pending.preview, key.current); setPending(null); await load(); return true as const; }
    catch (e) { if ([409, 412].includes((e as { status?: number }).status ?? 0)) { setPending(null); await load(); } fail(e); return { error: textError(e) }; } finally { flight.current = false; setBusy(false); }
  }
  const blocked = loading || busy; const backwards = editor?.kind === 'stage' && returnToProgress(editor.item.stage, draft.stage);
  function field(name: keyof Draft, label: string, max = 200, multiline = false, type = 'text', required = false) {
    return <div key={name}><label htmlFor={prefix + name}>{label}</label>{multiline ? <textarea id={prefix + name} maxLength={max} value={draft[name]} onChange={e => change(name, e.target.value)} /> : <input id={prefix + name} type={type} required={required} maxLength={max} value={draft[name]} onChange={e => change(name, e.target.value)} />}</div>;
  }
  const jobActions = (item: CareerJob) => <div className="resource-actions">
    {!item.isTrash && <><button disabled={!can('job.update') || blocked} onClick={() => void begin('job', item)}>Edit Job</button><button disabled={!can('job.transition') || blocked} onClick={() => void begin('stage', item)}>Change Job stage</button></>}
    <button disabled={!can('job.history') || blocked} onClick={() => void openHistory(item)}>Job history</button>
    {(item.isTrash ? ['restore', 'purge'] as const : ['trash'] as const).map(op => <button key={op} disabled={!can('job.' + op) || blocked} onClick={() => void life(item, op)}>{operationNames[op]}</button>)}
  </div>;
  const metadataDetails = (metadata: CompanyMetadata | JobMetadata) => <details><summary>View metadata</summary><dl>{Object.entries(metadata).map(([name,value]) => <div key={name}><dt>{name}</dt><dd>{value ?? '—'}</dd></div>)}</dl></details>;
  const jobCard = (item: CareerJob) => <article key={item.id} className="resource-card"><h2>{item.metadata.title}</h2><p>{item.stage}{item.isTrash ? ' · Trash' : ''}</p>{item.companyLabel && <p>Company: {item.companyLabel}</p>}{item.metadata.location && <p>{item.metadata.location} · {item.metadata.workMode}</p>}{item.metadata.salaryText && <p>{item.metadata.salaryText}</p>}{(item.metadata.salaryMin || item.metadata.salaryMax) && <p>{item.metadata.salaryMin ?? '—'} … {item.metadata.salaryMax ?? '—'} {item.metadata.currency}</p>}{item.metadata.notes && <p>{item.metadata.notes}</p>}{metadataDetails(item.metadata)}{jobActions(item)}</article>;
  return <section className="content-section career-screen" aria-labelledby="career-title">
    <div className="content-heading"><div><p className="eyebrow">FX39 / CAREER</p><h1 id="career-title">Career</h1><p>Company và quy trình ứng tuyển cá nhân.</p></div><button disabled={blocked} onClick={() => void run(load)}>Tải lại</button></div>
    {error && <p role="alert">{error}</p>}{loading && <p role="status">Đang tải…</p>}
    <section aria-label="Companies"><h2>Companies</h2><button disabled={!can('company.create') || blocked} onClick={() => void begin('company')}>New Company</button>
      {can('company.read') && <><form className="resource-form career-form" onSubmit={e => { e.preventDefault(); setCompanyApplied(companyQuery); }}><label htmlFor={prefix+'company-query'}>Company search</label><input id={prefix+'company-query'} maxLength={200} disabled={blocked} value={companyQuery} onChange={e => setCompanyQuery(e.target.value)} /><button disabled={blocked}>Search Companies</button></form>
        {!loading && !companies.length && <p>Không có Company khớp bộ lọc.</p>}<div className="resource-cards">{companies.map(item => <article className="resource-card" key={item.id}><h3>{item.metadata.title}</h3><p>{item.metadata.industry} · {item.metadata.location}</p>{item.metadata.notes && <p>{item.metadata.notes}</p>}{metadataDetails(item.metadata)}{item.mergedIntoId && <p>Merged · {item.mergedIntoId}</p>}{item.linkedJobCount !== null && <p>{item.linkedJobCount} linked Jobs</p>}<div className="resource-actions"><button disabled={!!item.mergedIntoId || !can('company.update') || blocked} onClick={() => void begin('company', item)}>Edit Company</button><button disabled={!!item.mergedIntoId || !can('company.merge') || blocked} onClick={() => void run(async () => { setPickerQuery(''); await loadPicker(); setMerge({ source: item, targetId: '', preview: null }); setConfirmed(false); setKeepTarget(false); setFormError(null); key.current = crypto.randomUUID(); })}>Merge Company</button></div></article>)}</div>
        {companyCursor && <button disabled={blocked} onClick={() => void run(async () => { const page = await careerCompanies(companyApplied, companyCursor); setCompanies(old => [...old, ...page.items]); setCompanyCursor(page.nextCursor); })}>Load more Companies</button>}</>}
    </section>
    <section aria-label="Jobs"><h2>Jobs</h2><button disabled={!can('job.create') || blocked} onClick={() => void begin('job')}>New Job</button>
      {can('job.read') && <><form className="resource-form career-form" onSubmit={e => { e.preventDefault(); setApplied({ ...filter }); }}><label htmlFor={prefix+'job-query'}>Job search</label><input id={prefix+'job-query'} disabled={blocked} value={filter.query} maxLength={200} onChange={e => setFilter(old => ({ ...old, query: e.target.value }))} />
        {(['state', 'stage', 'dateField'] as const).map(name => <div key={name}><label htmlFor={prefix+'filter-'+name}>{name === 'state' ? 'Job state filter' : name === 'stage' ? 'Job stage filter' : 'Job date field'}</label><select id={prefix+'filter-'+name} disabled={blocked} value={filter[name]} onChange={e => setFilter(old => ({ ...old, [name]: e.target.value }))}>{name !== 'state' && <option value="">All</option>}{(name === 'state' ? ['Active', 'Trash'] : name === 'stage' ? careerStages : ['Discovered', 'Applied']).map(v => <option key={v}>{v}</option>)}</select></div>)}
        {(['companyId', 'location', 'from', 'to'] as const).map(name => <div key={name}><label htmlFor={prefix+'filter-'+name}>{name === 'companyId' ? 'Company ID filter' : name === 'location' ? 'Job location filter' : name === 'from' ? 'Job date from' : 'Job date before'}</label><input id={prefix+'filter-'+name} type={name === 'from' || name === 'to' ? 'date' : 'text'} disabled={blocked} maxLength={name === 'companyId' ? 36 : 200} value={filter[name]} onChange={e => setFilter(old => ({ ...old, [name]: e.target.value }))} /></div>)}
        <button disabled={blocked}>Filter Jobs</button><button type="button" disabled={blocked} onClick={() => { setFilter(emptyFilter); setApplied(emptyFilter); }}>Clear Job filters</button></form>
        <label htmlFor={prefix+'view'}>Job view</label><select id={prefix+'view'} disabled={blocked} value={view} onChange={e => setView(e.target.value as 'Table' | 'Kanban')}><option>Table</option><option>Kanban</option></select>
        <p>{jobs.length} loaded Jobs in this selection{jobCursor ? ' · More available' : ''}</p>{!loading && !jobs.length && <p>Không có Job khớp bộ lọc.</p>}
        {view === 'Table' ? <div className="career-table-scroll" tabIndex={0} role="region" aria-label="Job table"><table className="career-table"><thead><tr><th scope="col">Job</th><th scope="col">Stage</th><th scope="col">Company</th><th scope="col">Dates</th><th scope="col">Salary</th><th scope="col">Actions</th></tr></thead><tbody>{jobs.map(item => <tr key={item.id} className="resource-card"><td><h2>{item.metadata.title}</h2><p>{item.metadata.location} · {item.metadata.workMode}</p>{item.metadata.notes && <p>{item.metadata.notes}</p>}{metadataDetails(item.metadata)}</td><td>{item.stage}{item.isTrash?' · Trash':''}</td><td>{item.companyLabel ?? '—'}</td><td><p>Discovered: {item.metadata.discoveredOn ?? '—'}</p><p>Applied: {item.metadata.appliedOn ?? '—'}</p></td><td>{item.metadata.salaryText && <p>{item.metadata.salaryText}</p>}{(item.metadata.salaryMin || item.metadata.salaryMax) && <p>{item.metadata.salaryMin ?? '—'} … {item.metadata.salaryMax ?? '—'} {item.metadata.currency}</p>}</td><td>{jobActions(item)}</td></tr>)}</tbody></table></div> : <div className="career-kanban">{careerStages.map(stage => <section aria-label={stage} key={stage}><h3>{stage}</h3>{jobs.filter(j => j.stage === stage).map(jobCard)}</section>)}</div>}
        {jobCursor && <button disabled={blocked} onClick={() => void run(async () => { const page = await careerJobs(applied, jobCursor); setJobs(old => [...old, ...page.items]); setJobCursor(page.nextCursor); })}>Load more Jobs</button>}</>}
    </section>
    {history && <section aria-label="Job history" className="resource-card"><h2>History: {history.title}</h2><form className="resource-form career-form" onSubmit={e => { e.preventDefault(); void run(async () => { const page = await careerHistory(history.id, historyFilter); setHistoryApplied({ ...historyFilter }); setHistory({ ...history, items: page.items, cursor: page.nextCursor }); }); }}>
      <label htmlFor={prefix+'history-action'}>History action</label><select id={prefix+'history-action'} disabled={blocked} value={historyFilter.action} onChange={e => setHistoryFilter(old => ({ ...old, action: e.target.value }))}><option value="">All</option>{['career.job.create','career.job.update','career.job.transition','career.job.trash','career.job.restore','career.company.merge'].map(a => <option key={a}>{a}</option>)}</select>
      {(['from', 'to'] as const).map(name => <div key={name}><label htmlFor={prefix+'history-'+name}>{name === 'from' ? 'History instant from' : 'History instant before'}</label><input id={prefix+'history-'+name} disabled={blocked} placeholder="2026-10-03T00:00:00Z" value={historyFilter[name]} onChange={e => setHistoryFilter(old => ({ ...old, [name]: e.target.value }))} /></div>)}<button disabled={blocked}>Filter history</button></form>
      {history.items.map(event => <article key={event.id}><h3>Version {event.versionNumber}</h3><p>{event.actionKey} · {event.fromStage ?? 'New'} → {event.toStage} · {event.occurredAt}</p>{event.note && <p>{event.note}</p>}<p>Title: {event.snapshot.fields.metadata.title}</p>{event.snapshot.fields.companyLabelSnapshot && <p>Company: {event.snapshot.fields.companyLabelSnapshot}</p>}{Object.entries(event.snapshot.fields.metadata).filter(([name, value]) => name !== 'title' && name !== 'companyId' && value !== null).map(([name, value]) => <p key={name}>{name}: {value}</p>)}</article>)}
      {history.cursor && <button disabled={blocked} onClick={() => void run(async () => { const page = await careerHistory(history.id, historyApplied, history.cursor!); setHistory({ ...history, items: [...history.items, ...page.items], cursor: page.nextCursor }); })}>Load more Job history</button>}<button disabled={blocked} onClick={() => setHistory(null)}>Close Job history</button>
    </section>}
    <ResourceFormDialog open={!!editor} title={editor?.kind === 'company' ? 'Company details' : editor?.kind === 'stage' ? 'Change Job stage' : 'Job details'} busy={busy} dirty={dirty} onClose={clearEditor}>
      <form className="resource-form career-form" onSubmit={(e: FormEvent) => { e.preventDefault(); void persist(); }}><fieldset disabled={busy}>{formError && <p role="alert">{formError}</p>}
        {conflict && <div role="alert"><p>Revision conflict. So sánh toàn bộ bản hiện tại với bản nháp trước khi thay thế metadata.</p><dl>{Object.entries(conflict.metadata).map(([name,value]) => <div key={name}><dt>Current {name}</dt><dd>{value ?? '—'}</dd></div>)}</dl><p>Current revision: {conflict.etag}</p>{'stage' in conflict ? <><p>Current stage: {conflict.stage} · {conflict.isTrash?'Trash':'Active'}</p><p>Current Company label: {conflict.companyLabel ?? '—'}</p><p>Stage changed at: {conflict.stageChangedAt}</p></> : <p>Current merge target: {conflict.mergedIntoId ?? 'Unmerged'}</p>}<button type="button" onClick={() => void resolve(false)}>Reload current</button><button type="button" onClick={() => void resolve(true)}>Reapply draft</button></div>}
        {editor?.kind !== 'stage' && <>{field('title', editor?.kind === 'company' ? 'Company title' : 'Job title', 200, false, 'text', true)}{field('url', 'Source URL', 2048)}{field('location', 'Location')}{field('notes', editor?.kind === 'company' ? 'Company notes' : 'Job notes', 20000, true)}
          {editor?.kind === 'company' ? field('industry', 'Industry') : <>
            <label htmlFor={prefix+'company-picker'}>Job Company</label><select id={prefix+'company-picker'} disabled={!can('company.read')} value={draft.companyId} onChange={e => change('companyId', e.target.value)}><option value="">No Company</option>{draft.companyId && !picker.some(p => p.id === draft.companyId) && <option value={draft.companyId}>Current Company · {draft.companyId}</option>}{picker.map(p => <option key={p.id} value={p.id}>{p.metadata.title} · {p.id.slice(0,8)}</option>)}</select>
            <label htmlFor={prefix+'picker-query'}>Find Company</label><input id={prefix+'picker-query'} disabled={!can('company.read')} maxLength={200} value={pickerQuery} onChange={e => setPickerQuery(e.target.value)} /><button type="button" disabled={!can('company.read')} onClick={() => void run(() => loadPicker(pickerQuery))}>Search Company picker</button>{pickerCursor && <button type="button" onClick={() => void run(() => loadPicker(pickerApplied, pickerCursor))}>Load more Company choices</button>}
            <label htmlFor={prefix+'work-mode'}>Work mode</label><select id={prefix+'work-mode'} value={draft.workMode} onChange={e => change('workMode', e.target.value)}><option value="">Unspecified</option>{['Onsite', 'Hybrid', 'Remote'].map(v => <option key={v}>{v}</option>)}</select>
            {field('employmentType','Employment type',100)}{field('salaryText','Salary text',1000,true)}{field('salaryMin','Salary minimum (exact decimal)',30)}{field('salaryMax','Salary maximum (exact decimal)',30)}{field('currency','Salary currency',3)}{field('description','Job description',20000,true)}{field('source','Job source')}{field('discoveredOn','Discovered on',10,false,'date')}{field('appliedOn','Applied on',10,false,'date')}
          </>}
        </>}
        {(editor?.kind === 'stage' || (editor?.kind === 'job' && !editor.item)) && <><label htmlFor={prefix+'stage'}>Job stage</label><select id={prefix+'stage'} required value={draft.stage} onChange={e => { change('stage',e.target.value); setConfirmed(false); setReopen(false); }}><option value="">Choose a stage</option>{careerStages.map(s => <option key={s}>{s}</option>)}</select></>}
        {editor?.kind === 'stage' && <>{field('reason','Private stage reason',2000,true)}<label><input type="checkbox" checked={confirmed} required onChange={e => { setConfirmed(e.target.checked); key.current=crypto.randomUUID(); }} />Confirm stage change</label>{backwards && <><p role="alert">Đưa kết quả đã ghi nhận trở lại quy trình; cần lý do riêng tư.</p><label><input type="checkbox" required checked={reopen} onChange={e => { setReopen(e.target.checked); key.current=crypto.randomUUID(); }} />Confirm return to progress</label></>}</>}
        <button disabled={busy || !!conflict}>{editor?.kind === 'company' ? 'Save Company' : editor?.kind === 'stage' ? 'Save Job stage' : 'Save Job'}</button>
      </fieldset></form>
    </ResourceFormDialog>
    <ResourceFormDialog open={!!merge} title="Merge Company" busy={busy} dirty={mergeDirty} onClose={() => { setMerge(null); setPicker([]); setPickerCursor(null); setFormError(null); }}>
      {merge && <form className="resource-form career-form" onSubmit={e => { e.preventDefault(); void run(async () => { if (!merge.preview) return; try { await mergeCompany(merge.preview,confirmed,keepTarget,key.current); setMerge(null); await load(); } catch(e) { setFormError(textError(e)); if ([409,412].includes((e as {status?:number}).status ?? 0)) setMerge({ ...merge,preview:null }); throw e; } }); }}><fieldset disabled={busy}><p>Source: {merge.source.metadata.title}. Giữ Company nguồn ở trạng thái chỉ đọc; chuyển các Job đang hoạt động sang Company đích.</p>
        {formError && <p role="alert">{formError}</p>}<label htmlFor={prefix+'merge-target'}>Merge target Company</label><select id={prefix+'merge-target'} required value={merge.targetId} onChange={e => { setMerge({...merge,targetId:e.target.value,preview:null});setConfirmed(false);setKeepTarget(false);key.current=crypto.randomUUID(); }}><option value="">Choose a target</option>{picker.filter(p => p.id!==merge.source.id).map(p => <option key={p.id} value={p.id}>{p.metadata.title} · {p.id.slice(0,8)}</option>)}</select>
        <label htmlFor={prefix+'merge-query'}>Find merge target</label><input id={prefix+'merge-query'} value={pickerQuery} maxLength={200} onChange={e => setPickerQuery(e.target.value)} /><button type="button" onClick={() => void run(() => loadPicker(pickerQuery))}>Search merge targets</button>{pickerCursor && <button type="button" onClick={() => void run(() => loadPicker(pickerApplied,pickerCursor))}>Load more merge targets</button>}
        <button type="button" disabled={!merge.targetId} onClick={() => void run(async () => { const preview=await previewCompanyMerge(merge.source.id,merge.targetId);setMerge({...merge,preview});setConfirmed(false);setKeepTarget(false);key.current=crypto.randomUUID(); })}>Preview merge</button>
        {merge.preview && <><p>{merge.preview.jobs.length} Jobs sẽ chuyển; {merge.preview.retainedReferences.length} references giữ nguyên.</p><p>Target title: {merge.preview.targetMetadata?.title ?? merge.targetId}</p><p>Target notes: {merge.preview.targetMetadata?.notes}</p>{merge.preview.jobs.map(j => <p key={j.id}>{j.title ?? j.id}</p>)}<label><input type="checkbox" required checked={keepTarget} onChange={e => {setKeepTarget(e.target.checked);key.current=crypto.randomUUID();}} />Keep target metadata unchanged</label><label><input type="checkbox" required checked={confirmed} onChange={e => {setConfirmed(e.target.checked);key.current=crypto.randomUUID();}} />Confirm Company merge</label><button>Commit Company merge</button></>}
      </fieldset></form>}
    </ResourceFormDialog>
    {pending && <ActionDialog title={operationNames[pending.preview.operation]} description={<span>{pending.title}. {pending.preview.operation==='purge'?'Xóa vĩnh viễn Job và lịch sử; không thể hoàn tác.':'Giữ nguyên stage đã ghi nhận.'} {pending.preview.referenceCount} retained references.</span>} confirmLabel={operationNames[pending.preview.operation]} confirmDisabled={busy || (pending.preview.operation==='purge' && pending.preview.referenceCount>0)} onClose={() => { if (!flight.current) setPending(null); }} onConfirm={commitLife} />}
    {leave && <ActionDialog title="Bỏ thay đổi chưa lưu?" description="Bản nháp Career chưa lưu." confirmLabel="Bỏ bản nháp" confirmDisabled={busy} onClose={() => {if(!flight.current)setLeave(null);}} onConfirm={() => {if(flight.current)return false;const proceed=leave.proceed;clearEditor();setMerge(null);setLeave(null);proceed();return true;}}>{editor && <button disabled={busy || !!conflict} onClick={async () => {const proceed=leave.proceed;if(await persist()){setLeave(null);proceed();}else setLeave(null);}}>Save and leave</button>}</ActionDialog>}
  </section>;
}
