import { useEffect, useRef, useState, type FormEvent } from 'react';
import { createIdempotencyKey, NexoraApiError, type ProductivityCommittedAcknowledgement } from './api';
import { ResourceFormDialog, useResourceDialog } from './ResourceFormDialog';
import { registerDirtyLeaveGuard, type DirtyLeaveContinuation } from './navigationGuard';
import { WorkLogHistory } from './WorkLogHistory';
import { workLogCapabilities, workLogGet, workLogList, workLogReport, workLogSave, workLogTransition, workLogLifecyclePreview, workLogLifecycle, type WorkLogLifecycleOperation, type WorkLogLifecyclePreview, type WorkLog, type WorkLogFilters, type WorkLogPage, type WorkLogReport } from './workLogApi';

type Draft={title:string;recordedOn:string;minutes:string;notes:string;category:string;employer:string};
const today=(timeZone:string)=>{ try {
  const parts=new Intl.DateTimeFormat('en-CA',{timeZone,year:'numeric',month:'2-digit',day:'2-digit'}).formatToParts(new Date());
  return ['year','month','day'].map(type=>parts.find(part=>part.type===type)?.value??'').join('-');
} catch { return ''; } };
const blank=(timeZone:string):Draft=>({title:'',recordedOn:today(timeZone),minutes:'',notes:'',category:'',employer:''});
type Intent={send:()=>Promise<ProductivityCommittedAcknowledgement>;label:string};

export function WorkLogScreen({onAuthLost,timeZoneId,initialFilters,onViewChange}:{onAuthLost:()=>void;timeZoneId:string;initialFilters?:WorkLogFilters;onViewChange?:(value:WorkLogFilters)=>void}) {
  const [caps,setCaps]=useState<Record<string,boolean>|null>(null), [page,setPage]=useState<WorkLogPage|null>(null), [selected,setSelected]=useState<WorkLog|null>(null);
  const [report,setReport]=useState<WorkLogReport|null>(null), [filters,setFilters]=useState<WorkLogFilters>(()=>initialFilters??({status:'Active',query:'',from:'',to:''}));
  const [draft,setDraft]=useState<Draft>(()=>blank(timeZoneId)), [editing,setEditing]=useState<WorkLog|null>(null);
  const [confirmation,setConfirmation]=useState<WorkLog|null>(null), [busy,setBusy]=useState(false), [reading,setReading]=useState(false), [uncertain,setUncertain]=useState(false);
  const [lifecycle,setLifecycle]=useState<{row:WorkLog;preview:WorkLogLifecyclePreview}|null>(null);
  const [error,setError]=useState<string|null>(null), [notice,setNotice]=useState<string|null>(null);
  const generation=useRef(0), controller=useRef<AbortController|null>(null), pending=useRef<Intent|null>(null), flight=useRef(false);
  const [leaving,setLeaving]=useState(false), leave=useRef<DirtyLeaveContinuation|null>(null);
  const editor=useResourceDialog(draft,setDraft), transitionDialog=useResourceDialog(confirmation);
  const lifecycleDialog=useResourceDialog(lifecycle);
  const can=(action:string)=>caps?.['learning.'+action]===true;
  const blocked=busy||reading||uncertain;
  useEffect(()=>{ void reload(); return()=>{ ++generation.current; controller.current?.abort(); pending.current=null; }; },[]);
  useEffect(()=>registerDirtyLeaveGuard(proceed=>{
    if(!pending.current) return false;
    if(flight.current) return true;
    leave.current=proceed;setLeaving(true);return true;
  },-90),[]);
  useEffect(()=>{
    const warn=(event:BeforeUnloadEvent)=>{if(pending.current){event.preventDefault();event.returnValue='';}};
    window.addEventListener('beforeunload',warn);return()=>window.removeEventListener('beforeunload',warn);
  },[]);

  function clearPrivate() { setPage(null); setSelected(null); setReport(null); setEditing(null); setDraft(blank(timeZoneId)); setConfirmation(null); setLifecycle(null); editor.finish(); transitionDialog.finish(); lifecycleDialog.finish(); }
  function failure(value:unknown) {
    const api=value instanceof NexoraApiError?value:null;
    setError(api?.message??'Không thể hoàn tất yêu cầu Work Log.');
    if(api && ([401,403,404].includes(api.status)||['ModuleUnavailable','ActionDenied','DependencyUnavailable'].includes(api.code??''))) {
      clearPrivate(); if(api.status!==404) setCaps(null); if(api.status===401) onAuthLost();
    }
    return api;
  }
  async function read<T>(fetcher:(signal:AbortSignal)=>Promise<T>,apply:(value:T)=>void) {
    if(busy||pending.current) return;
    const epoch=++generation.current; controller.current?.abort(); const abort=new AbortController(); controller.current=abort;
    setReading(true); setError(null); setNotice(null);
    try { const value=await fetcher(abort.signal); if(epoch===generation.current) apply(value); }
    catch(value) { if(epoch===generation.current && !abort.signal.aborted) failure(value); }
    finally { if(epoch===generation.current) setReading(false); }
  }
  async function reload() {
    clearPrivate(); setCaps(null);
    await read(async signal=>{
      const nextCaps=await workLogCapabilities(signal);
      const nextPage=nextCaps['learning.worklog.read']?await workLogList(filters,null,signal):null;
      return {nextCaps,nextPage};
    },value=>{setCaps(value.nextCaps);setPage(value.nextPage);});
  }
  function changeFilter(next:WorkLogFilters) {
    ++generation.current; controller.current?.abort(); setReading(false); setPage(null); setSelected(null); setReport(null); setFilters(next); onViewChange?.(next); setError(null); setNotice(null);
  }
  function loadPage(cursor:string|null=null) {
    if(filters.from && filters.to && filters.to<filters.from) {setError('Ngày kết thúc phải từ ngày bắt đầu trở đi.');return;}
    setPage(null);setSelected(null);setReport(null); void read(signal=>workLogList(filters,cursor,signal),setPage);
  }
  function begin(row?:WorkLog) {
    if(blocked) return;
    const next=row?{title:row.title,recordedOn:row.recordedOn,minutes:String(row.minutes),notes:row.notes??'',category:row.category??'',employer:row.employer??''}:blank(timeZoneId);
    setEditing(row??null);setDraft(next);setError(null);editor.begin(next);
  }
  async function execute() {
    const intent=pending.current; if(!intent||flight.current) return;
    flight.current=true;setBusy(true);setError(null);setNotice(null);
    ++generation.current;controller.current?.abort();setReading(false);
    try {
      await intent.send(); if(pending.current!==intent) return;
      pending.current=null;setUncertain(false);clearPrivate();setNotice('Đã lưu thao tác. Tải lại khi bạn muốn xem dữ liệu hiện tại.');
    } catch(value) {
      if(pending.current!==intent) return;
      const api=failure(value);
      const unknown=!api||api.status===0||api.status>=500||api.status===408||['RequestInProgress','CommittedOutcomeUnavailable','IdempotencyConflict'].includes(api.code??'');
      if(unknown) {setUncertain(true);editor.finish();transitionDialog.finish();lifecycleDialog.finish();}
      else {pending.current=null;setUncertain(false);}
    } finally {flight.current=false;setBusy(false);}
  }
  async function save(event:FormEvent) {
    event.preventDefault(); if(blocked||pending.current) return;
    const minutes=/^\d{1,4}$/.test(draft.minutes)?Number(draft.minutes):0;
    if(!draft.title.trim()||draft.title.trim().length>200||!draft.recordedOn||minutes<1||minutes>1440) {setError('Nhập tiêu đề, ngày địa phương và số phút nguyên từ 1 đến 1440.');return;}
    const body={title:draft.title.trim(),recordedOn:draft.recordedOn,minutes,notes:draft.notes.trim()||null,category:draft.category.trim()||null,employer:draft.employer.trim()||null};
    const key=createIdempotencyKey(),row=editing??undefined;
    pending.current={label:'Lưu nhật ký',send:()=>workLogSave(body,key,row)}; await execute();
  }
  async function transition() {
    if(blocked||pending.current||!confirmation) return;
    const row=confirmation,op=row.status==='Active'?'archive':'unarchive',key=createIdempotencyKey();
    pending.current={label:op,send:()=>workLogTransition(row,op,key)};await execute();
  }
  function loadReport() {
    if(filters.status==='Trash'){setError('Nhật ký trong Thùng rác không thuộc báo cáo thời lượng.');return;}
    if(!filters.from||!filters.to||filters.to<filters.from||Date.parse(filters.to+'T00:00:00Z')-Date.parse(filters.from+'T00:00:00Z')>365*86400000) {setError('Chọn khoảng ngày từ 1 đến 366 ngày cho báo cáo.');return;}
    setReport(null);void read(signal=>workLogReport(filters,signal),setReport);
  }
  function previewLifecycle(row:WorkLog,operation:WorkLogLifecycleOperation) {
    if(blocked||pending.current)return;
    void read(signal=>workLogLifecyclePreview(row,operation,signal),preview=>{const next={row,preview};setLifecycle(next);lifecycleDialog.begin(next);});
  }
  async function confirmLifecycle() {
    if(blocked||pending.current||!lifecycle)return;
    const {row,preview}=lifecycle;
    if(preview.operation==='purge'&&preview.referenceCount>0)return;
    const key=createIdempotencyKey();pending.current={label:preview.operation,send:()=>workLogLifecycle(row,preview,key)};await execute();
  }

  return <section className="content-section" aria-labelledby="worklog-title">
    <div className="content-heading"><div><p className="eyebrow">LEARNING / WORK LOG</p><h1 id="worklog-title">Work Log</h1><p className="lead">Nhật ký thủ công theo ngày địa phương, với thời lượng nhập bằng phút.</p></div>
      <div className="form-actions"><button className="secondary-button" type="button" disabled={blocked} onClick={()=>void reload()}>Tải lại</button>{can('worklog.create')&&<button className="primary-button" type="button" disabled={blocked} onClick={()=>begin()}>Nhật ký mới</button>}</div>
    </div>
    {error&&<p role="alert" className="notice notice-error">{error}</p>}{notice&&<p role="status" className="notice notice-success">{notice}</p>}
    {uncertain&&<div role="alert">Chưa xác nhận được kết quả. Thử lại giữ nguyên nội dung, revision và mã yêu cầu.<button className="secondary-button" type="button" disabled={busy} onClick={()=>void execute()}>Thử lại thao tác cũ</button></div>}
    {reading&&<p role="status">Đang tải…</p>}
    <fieldset disabled={blocked} style={{border:0,margin:0,padding:0,minWidth:0}}>
      {can('worklog.read')&&<form className="form-panel" onSubmit={event=>{event.preventDefault();loadPage();}}>
        <div className="form-grid"><div className="field-group"><label htmlFor="worklog-status">Trạng thái</label><select id="worklog-status" value={filters.status} onChange={event=>changeFilter({...filters,status:event.target.value as WorkLogFilters['status']})}><option value="Active">Active</option><option value="Archived">Archived</option><option value="Trash">Thùng rác</option></select></div>
          <div className="field-group"><label htmlFor="worklog-query">Tiêu đề</label><input id="worklog-query" maxLength={200} value={filters.query} onChange={event=>changeFilter({...filters,query:event.target.value})}/></div>
          <div className="field-group"><label htmlFor="worklog-filter-category">Nhóm chính xác (tùy chọn)</label><input id="worklog-filter-category" maxLength={100} value={filters.category??''} onChange={event=>changeFilter({...filters,category:event.target.value})}/></div>
          <div className="field-group"><label htmlFor="worklog-from">Từ ngày</label><input id="worklog-from" type="date" value={filters.from} onChange={event=>changeFilter({...filters,from:event.target.value})}/></div>
          <div className="field-group"><label htmlFor="worklog-to">Đến ngày</label><input id="worklog-to" type="date" value={filters.to} onChange={event=>changeFilter({...filters,to:event.target.value})}/></div>
        </div><div className="form-actions"><button className="primary-button" type="submit">Áp dụng bộ lọc</button>{can('report.read')&&filters.status!=='Trash'&&<button className="secondary-button" type="button" onClick={loadReport}>Tổng hợp theo ngày</button>}</div>
      </form>}
      {caps&&!can('worklog.read')&&<p>Không có quyền xem nhật ký hiện tại.</p>}
      {page&&(page.items.length===0?<p>Không có nhật ký phù hợp với bộ lọc.</p>:<><div className="resource-cards">{page.items.map(row=><article className="resource-card" key={row.id}>
        <strong>{row.title}</strong><p>{row.recordedOn} · {row.minutes} phút · {row.status}</p>
        <button className="secondary-button" type="button" onClick={()=>{setSelected(null);void read(signal=>workLogGet(row.id,signal),setSelected);}}>Chi tiết</button>
      </article>)}</div>{page.nextCursor&&<button className="secondary-button" type="button" onClick={()=>loadPage(page.nextCursor)}>Trang tiếp</button>}</>)}
      {selected&&<article className="form-panel"><h2>{selected.title}</h2><p>{selected.recordedOn} · {selected.minutes} phút · {selected.status}</p><p>Nhóm: {selected.category??'—'} · Tổ chức: {selected.employer??'—'}</p><p style={{whiteSpace:'pre-wrap',overflowWrap:'anywhere'}}>{selected.notes??'Không có ghi chú.'}</p><div className="form-actions">
        {selected.status==='Active'&&can('worklog.update')&&<button className="secondary-button" type="button" onClick={()=>begin(selected)}>Sửa</button>}
        {selected.status!=='Trash'&&can(selected.status==='Active'?'worklog.archive':'worklog.unarchive')&&<button className="secondary-button" type="button" onClick={()=>{setConfirmation(selected);setError(null);transitionDialog.begin(selected);}}>{selected.status==='Active'?'Archive':'Unarchive'}</button>}
        {selected.status!=='Trash'&&can('worklog.trash')&&<button className="secondary-button" type="button" onClick={()=>previewLifecycle(selected,'trash')}>Đưa vào Thùng rác…</button>}
        {selected.status==='Trash'&&can('worklog.restore')&&<button className="secondary-button" type="button" onClick={()=>previewLifecycle(selected,'restore')}>Khôi phục về {selected.preTrashState}…</button>}
        {selected.status==='Trash'&&can('worklog.purge')&&<button className="danger-button" type="button" onClick={()=>previewLifecycle(selected,'purge')}>Xóa vĩnh viễn…</button>}
      </div><WorkLogHistory key={selected.id+selected.etag} id={selected.id} timeZoneId={timeZoneId} blocked={blocked} onFailure={failure}/></article>}
      {report&&<section className="form-panel" aria-labelledby="worklog-report-title"><h2 id="worklog-report-title">Tổng thời lượng đã nhập</h2><p>{report.from} → {report.to} · {report.status} · {report.entries} mục · {report.grossMinutes} phút</p><p>Nhóm: {report.category??'Tất cả'}</p><p className="muted">Đây là tổng phút thô; không tự loại trùng hoặc xác định thời gian chồng lấn từ ngày và thời lượng nhập tay. Không dùng làm số giờ tính lương.</p>
        {report.days.length===0?<p>Không có dữ liệu trong khoảng ngày đã chọn.</p>:<div className="table-wrap"><table><caption className="sr-only">Tổng thời lượng thủ công theo ngày</caption><thead><tr><th scope="col">Ngày địa phương</th><th scope="col">Số mục</th><th scope="col">Phút</th></tr></thead><tbody>{report.days.map(day=><tr key={day.recordedOn}><td>{day.recordedOn}</td><td>{day.entries}</td><td>{day.minutes}</td></tr>)}</tbody></table></div>}
      </section>}
    </fieldset>
    <ResourceFormDialog open={editor.open} title={editing?'Sửa nhật ký':'Nhật ký mới'} busy={busy} dirty={editor.dirty} onClose={()=>editor.cancel()}>
      {error&&<p role="alert">{error}</p>}<form className="resource-form" onSubmit={save}>
        <div className="field-group"><label htmlFor="worklog-title-input">Tiêu đề</label><input id="worklog-title-input" required maxLength={200} value={draft.title} onChange={event=>setDraft({...draft,title:event.target.value})}/></div>
        <div className="form-grid"><div className="field-group"><label htmlFor="worklog-day">Ngày địa phương</label><input id="worklog-day" type="date" required value={draft.recordedOn} onChange={event=>setDraft({...draft,recordedOn:event.target.value})}/></div><div className="field-group"><label htmlFor="worklog-minutes">Thời lượng (phút)</label><input id="worklog-minutes" type="number" min={1} max={1440} step={1} required value={draft.minutes} onChange={event=>setDraft({...draft,minutes:event.target.value})}/></div></div>
        <p className="muted">Ngày đã ghi được giữ nguyên khi đổi múi giờ.</p>
        <div className="form-grid"><div className="field-group"><label htmlFor="worklog-category">Nhóm (tùy chọn)</label><input id="worklog-category" maxLength={100} value={draft.category} onChange={event=>setDraft({...draft,category:event.target.value})}/></div><div className="field-group"><label htmlFor="worklog-employer">Tổ chức (tùy chọn)</label><input id="worklog-employer" maxLength={200} value={draft.employer} onChange={event=>setDraft({...draft,employer:event.target.value})}/></div></div>
        <div className="field-group"><label htmlFor="worklog-notes">Ghi chú (tùy chọn)</label><textarea id="worklog-notes" maxLength={20000} value={draft.notes} onChange={event=>setDraft({...draft,notes:event.target.value})}/></div>
        <div className="form-actions"><button className="secondary-button" type="button" disabled={busy} onClick={()=>editor.cancel()}>Hủy</button><button className="primary-button" type="submit" disabled={busy||uncertain}>{busy?'Đang lưu…':'Lưu'}</button></div>
      </form>
    </ResourceFormDialog>
    <ResourceFormDialog open={transitionDialog.open} title={confirmation?.status==='Active'?'Archive nhật ký?':'Unarchive nhật ký?'} busy={busy} dirty={false} onClose={()=>{transitionDialog.finish();setConfirmation(null);}}>
      {error&&<p role="alert">{error}</p>}<p><strong>{confirmation?.title}</strong></p><p>{confirmation?.status==='Active'?'Nhật ký sẽ chỉ đọc; nội dung và thời lượng được giữ lại.':'Nhật ký sẽ trở về trạng thái Active đã lưu trước archive.'}</p>
      <div className="form-actions"><button className="secondary-button" type="button" disabled={busy} onClick={()=>{transitionDialog.finish();setConfirmation(null);}}>Hủy</button><button className="primary-button" type="button" disabled={busy||uncertain} onClick={()=>void transition()}>{busy?'Đang lưu…':'Xác nhận'}</button></div>
    </ResourceFormDialog>
    <ResourceFormDialog open={lifecycleDialog.open} title={lifecycle?.preview.operation==='purge'?'Xóa vĩnh viễn nhật ký?':lifecycle?.preview.operation==='restore'?'Khôi phục nhật ký?':'Đưa nhật ký vào Thùng rác?'} busy={busy} dirty={false} onClose={()=>{lifecycleDialog.finish();setLifecycle(null);}}>
      {error&&<p role="alert">{error}</p>}
      {lifecycle&&<><p><strong>{lifecycle.row.title}</strong> · {lifecycle.row.recordedOn}</p>
        <p>{lifecycle.preview.fromStatus} → {lifecycle.preview.toStatus}. Phạm vi: một nhật ký; {lifecycle.preview.historyCount} phiên bản hiện có; {lifecycle.preview.referenceCount} tham chiếu đang giữ.</p>
        {lifecycle.preview.batchId&&<p>Mã lô xóa: {lifecycle.preview.batchId}</p>}
        {lifecycle.preview.operation==='purge'?<p role="alert">Xóa nội dung và toàn bộ phiên bản của nhật ký này. Không có hoàn tác trong ứng dụng. Nhật ký bảo mật tối thiểu vẫn được giữ.</p>:<p>{lifecycle.preview.operation==='restore'?'Khôi phục đúng trạng thái trước khi đưa vào Thùng rác. Nhật ký Archived vẫn chỉ đọc.':'Giữ nội dung và lịch sử tới khi bạn tự xóa vĩnh viễn; nhật ký không được cộng trong báo cáo khi ở Thùng rác.'}</p>}
        {lifecycle.preview.operation==='purge'&&lifecycle.preview.referenceCount>0&&<p role="alert">Chưa thể xóa vĩnh viễn vì có tham chiếu đang giữ.</p>}
        <div className="form-actions"><button className="secondary-button" type="button" disabled={busy} onClick={()=>{lifecycleDialog.finish();setLifecycle(null);}}>Hủy</button><button className={lifecycle.preview.operation==='purge'?'danger-button':'primary-button'} type="button" disabled={busy||uncertain||(lifecycle.preview.operation==='purge'&&lifecycle.preview.referenceCount>0)} onClick={()=>void confirmLifecycle()}>{busy?'Đang lưu…':lifecycle.preview.operation==='purge'?'Xóa vĩnh viễn nội dung và lịch sử':'Xác nhận'}</button></div>
      </>}
    </ResourceFormDialog>
    <ResourceFormDialog open={leaving} title="Rời trang khi chưa rõ kết quả?" busy={busy} dirty={false} onClose={()=>{leave.current=null;setLeaving(false);}}>
      <p>Thao tác cũ có thể đã được lưu. Nếu rời trang, hãy kiểm tra dữ liệu hiện tại trước khi tạo một yêu cầu mới.</p>
      <div className="form-actions"><button className="secondary-button" type="button" disabled={busy} onClick={()=>{leave.current=null;setLeaving(false);}}>Ở lại đối chiếu</button><button className="primary-button" type="button" disabled={busy} onClick={()=>{const proceed=leave.current;leave.current=null;pending.current=null;setUncertain(false);setLeaving(false);clearPrivate();proceed?.();}}>Rời trang</button></div>
    </ResourceFormDialog>
  </section>;
}
