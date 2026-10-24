import { apiFetch, isProductivityAcknowledgement, jsonMutationHeaders, NexoraApiError, type ProductivityCommittedAcknowledgement } from './api';

const root='/api/v1/learning/work-logs';
export type WorkLog={ id:string; title:string; recordedOn:string; minutes:number; notes:string|null; category:string|null; employer:string|null; status:'Active'|'Archived'|'Trash'; createdAt:string; updatedAt:string; etag:string;preTrashState:string|null };
export type WorkLogDraft={ title:string; recordedOn:string; minutes:number; notes:string|null; category:string|null; employer:string|null };
export type WorkLogFilters={ status:'Active'|'Archived'|'Trash'; query:string; from:string; to:string; category?:string };
export type WorkLogPage={ items:WorkLog[]; nextCursor:string|null };
export type WorkLogVersion=Omit<WorkLog,'id'|'createdAt'|'updatedAt'|'etag'|'preTrashState'>&{sequence:number;event:'Created'|'Updated'|'Archived'|'Unarchived'|'HistoryStarted'|'Trashed'|'Restored';capturedAt:string;sourceUpdatedAt:string};
export type WorkLogLifecycleOperation='trash'|'restore'|'purge';
export type WorkLogLifecyclePreview={id:string;operation:WorkLogLifecycleOperation;etag:string;fromStatus:WorkLog['status'];toStatus:WorkLog['status']|'Purged';batchId:string|null;historyCount:number;referenceCount:number;expiresAt:string;previewToken:string};
export type WorkLogHistoryPage={items:WorkLogVersion[];nextCursor:string|null};
export type WorkLogReport={ from:string; to:string; status:string; entries:number; grossMinutes:number; days:{recordedOn:string;entries:number;minutes:number}[];mode:'GrossManualMinutes';overlap:'NotDeduplicated';category:string|null };
const uuid=(value:unknown):value is string=>typeof value==='string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value) && value!=='00000000-0000-0000-0000-000000000000';
const day=(value:unknown):value is string=>typeof value==='string' && /^\d{4}-\d{2}-\d{2}$/.test(value) && Number.isFinite(Date.parse(value+'T00:00:00Z')) && new Date(value+'T00:00:00Z').toISOString().slice(0,10)===value;
const text=(value:unknown)=>value===null || typeof value==='string';
const validRow=(row:WorkLog)=>row && uuid(row.id) && typeof row.title==='string' && day(row.recordedOn) && Number.isInteger(row.minutes) && row.minutes>=1 && row.minutes<=1440 && ['Active','Archived','Trash'].includes(row.status) && (row.status==='Trash'?['Active','Archived'].includes(row.preTrashState??''):row.preTrashState===null) && text(row.notes) && text(row.category) && text(row.employer) && typeof row.etag==='string' && /^"[A-Za-z0-9+/]{11}="$/.test(row.etag) && Number.isFinite(Date.parse(row.createdAt)) && Number.isFinite(Date.parse(row.updatedAt));
function invalid():never { throw new NexoraApiError('Không thể xác nhận dữ liệu Work Log. Vui lòng tải lại.',0,'InvalidWorkLogResponse'); }
function query(filters:WorkLogFilters,cursor?:string|null) {
  const params=new URLSearchParams({status:filters.status,query:filters.query});
  if(filters.category?.trim()) params.set('category',filters.category.trim());
  if(filters.from) params.set('from',filters.from); if(filters.to) params.set('to',filters.to); if(cursor) params.set('cursor',cursor);
  return params.toString();
}
export async function workLogCapabilities(signal?:AbortSignal) {
  const caps=await apiFetch<Record<string,boolean>>(root+'/capabilities',{signal});
  if(!caps || typeof caps!=='object' || Object.values(caps).some(value=>typeof value!=='boolean')) return invalid();
  return caps;
}
export async function workLogList(filters:WorkLogFilters,cursor?:string|null,signal?:AbortSignal) {
  const page=await apiFetch<WorkLogPage>(root+'?'+query(filters,cursor),{signal});
  if(!page || !Array.isArray(page.items) || page.items.length>25 || page.items.some(row=>!validRow(row)) || (page.nextCursor!==null && typeof page.nextCursor!=='string')) return invalid();
  return page;
}
export async function workLogGet(id:string,signal?:AbortSignal) {
  const row=await apiFetch<WorkLog>(root+'/'+encodeURIComponent(id),{signal}); if(!validRow(row) || row.id.toLowerCase()!==id.toLowerCase()) return invalid(); return row;
}
export async function workLogHistory(id:string,cursor?:string|null,signal?:AbortSignal) {
  const page=await apiFetch<WorkLogHistoryPage>(root+'/'+encodeURIComponent(id)+'/history'+(cursor?'?cursor='+encodeURIComponent(cursor):''),{signal});
  if(!page || !Array.isArray(page.items) || page.items.length>25 || (page.nextCursor!==null && (typeof page.nextCursor!=='string' || !/^wh1\.[A-Za-z0-9_-]{48}$/.test(page.nextCursor)))) return invalid();
  let prior=Infinity;
  for(const item of page.items) {
    if(!item || !Number.isSafeInteger(item.sequence) || item.sequence<1 || item.sequence>=prior
        || !['Created','Updated','Archived','Unarchived','HistoryStarted','Trashed','Restored'].includes(item.event)
        || !Number.isFinite(Date.parse(item.capturedAt)) || !Number.isFinite(Date.parse(item.sourceUpdatedAt))
        || typeof item.title!=='string' || item.title.length>200 || !day(item.recordedOn)
        || !Number.isInteger(item.minutes) || item.minutes<1 || item.minutes>1440 || !['Active','Archived','Trash'].includes(item.status)
        || !text(item.notes) || !text(item.category) || !text(item.employer)) return invalid();
    prior=item.sequence;
  }
  if(page.nextCursor!==null && page.items.length!==25) return invalid();
  return page;
}
async function mutate(path:string,body:unknown,key:string,etag?:string,expectedId?:string) {
  const ack=await apiFetch<ProductivityCommittedAcknowledgement>(path,{method:etag && !['archive','unarchive','trash','restore','purge'].some(op=>path.endsWith('/'+op))?'PUT':'POST',headers:jsonMutationHeaders(key,etag?{'If-Match':etag}:{}),body:JSON.stringify(body)},expectedId?200:201);
  if(!isProductivityAcknowledgement(ack) || !uuid(ack.id) || (expectedId && ack.id.toLowerCase()!==expectedId.toLowerCase()) || Object.keys(ack).some(key=>!['id','committed','detailAvailable'].includes(key)))
    throw new NexoraApiError('Chưa xác nhận được kết quả đã lưu. Thử lại đúng thao tác cũ.',0,'UnexpectedAcknowledgement');
  return ack;
}
export const workLogSave=(body:WorkLogDraft,key:string,row?:WorkLog)=>mutate(root+(row?'/'+encodeURIComponent(row.id):''),body,key,row?.etag,row?.id);
export const workLogTransition=(row:WorkLog,operation:'archive'|'unarchive',key:string)=>mutate(root+'/'+encodeURIComponent(row.id)+'/'+operation,{confirm:true},key,row.etag,row.id);
export async function workLogLifecyclePreview(row:WorkLog,operation:WorkLogLifecycleOperation,signal?:AbortSignal) {
  const value=await apiFetch<WorkLogLifecyclePreview>(root+'/'+encodeURIComponent(row.id)+'/'+operation+'-preview',{method:'POST',headers:jsonMutationHeaders(undefined,{'If-Match':row.etag}),signal});
  const next=operation==='trash'?'Trash':operation==='restore'?row.preTrashState:'Purged';
  if(!value || value.id!==row.id || value.operation!==operation || value.etag!==row.etag || value.fromStatus!==row.status || value.toStatus!==next
      || (row.status==='Trash'?!uuid(value.batchId):value.batchId!==null) || !Number.isSafeInteger(value.historyCount) || value.historyCount<0
      || !Number.isSafeInteger(value.referenceCount) || value.referenceCount<0 || !Number.isFinite(Date.parse(value.expiresAt))
      || typeof value.previewToken!=='string' || !value.previewToken.startsWith('wlp1.') || value.previewToken.length>4096) return invalid();
  return value;
}
export const workLogLifecycle=(row:WorkLog,preview:WorkLogLifecyclePreview,key:string)=>mutate(root+'/'+encodeURIComponent(row.id)+'/'+preview.operation,{confirm:true,previewToken:preview.previewToken},key,row.etag,row.id);
export async function workLogReport(filters:WorkLogFilters,signal?:AbortSignal) {
  const report=await apiFetch<WorkLogReport>(root+'/report?'+query(filters),{signal});
  const integer=(value:number)=>Number.isSafeInteger(value) && value>=0;
  if(!report || !day(report.from) || !day(report.to) || report.from!==filters.from || report.to!==filters.to || report.status!==filters.status
      || report.mode!=='GrossManualMinutes' || report.overlap!=='NotDeduplicated' || report.category!==(filters.category?.trim()||null) || !integer(report.entries) || !integer(report.grossMinutes)
      || !Array.isArray(report.days) || report.days.length>366 || report.days.some(item=>!item || !day(item.recordedOn) || !integer(item.entries) || !integer(item.minutes))
      || report.days.reduce((sum,item)=>sum+item.entries,0)!==report.entries || report.days.reduce((sum,item)=>sum+item.minutes,0)!==report.grossMinutes) return invalid();
  return report;
}
