import { useEffect, useRef, useState } from 'react';
import { NexoraApiError } from './api';
import { workLogHistory, type WorkLogHistoryPage, type WorkLogVersion } from './workLogApi';

const labels:Record<WorkLogVersion['event'],string>={Created:'Tạo nhật ký',Updated:'Sửa nhật ký',Archived:'Archive',Unarchived:'Unarchive',HistoryStarted:'Bắt đầu lưu lịch sử',Trashed:'Đưa vào Thùng rác',Restored:'Khôi phục'};

export function WorkLogHistory({id,timeZoneId,blocked,onFailure}:{id:string;timeZoneId:string;blocked:boolean;onFailure:(error:unknown)=>unknown}) {
  const [page,setPage]=useState<WorkLogHistoryPage|null>(null), [busy,setBusy]=useState(false), [error,setError]=useState<string|null>(null);
  const generation=useRef(0), controller=useRef<AbortController|null>(null), flight=useRef(false);
  useEffect(()=>()=>{++generation.current;controller.current?.abort();},[]);
  useEffect(()=>{
    if(blocked) {++generation.current;controller.current?.abort();flight.current=false;setBusy(false);setPage(null);setError(null);}
  },[blocked]);
  function instant(value:string) {
    try {return new Intl.DateTimeFormat('vi',{timeZone:timeZoneId,dateStyle:'medium',timeStyle:'long'}).format(new Date(value));}
    catch {return value;}
  }
  async function load(cursor:string|null=null) {
    if(blocked||flight.current) return;
    const epoch=++generation.current;controller.current?.abort();const abort=new AbortController();controller.current=abort;
    flight.current=true;setBusy(true);setPage(null);setError(null);
    try {const value=await workLogHistory(id,cursor,abort.signal);if(epoch===generation.current) setPage(value);}
    catch(value) {if(epoch===generation.current && !abort.signal.aborted) {
      setError(value instanceof NexoraApiError?value.message:'Không thể tải lịch sử nhật ký.');onFailure(value);
    }}
    finally {if(epoch===generation.current){flight.current=false;setBusy(false);}}
  }
  return <section aria-labelledby="worklog-history-title">
    <h3 id="worklog-history-title">Lịch sử nhật ký</h3>
    <p className="muted">Chỉ có các phiên bản đã lưu từ khi bật ghi lịch sử. Mốc “Bắt đầu lưu lịch sử” chụp nội dung hiện có, không khẳng định các lần sửa trước đó.</p>
    <button className="secondary-button" type="button" disabled={blocked||busy} onClick={()=>void load()}>Tải lịch sử từ đầu</button>
    {busy&&<p role="status">Đang tải lịch sử…</p>}
    {error&&<p role="alert">{error}</p>}
    {page&&(page.items.length===0?<p>Chưa có phiên bản được ghi lại. Nội dung cũ không được tạo lại thành lịch sử.</p>:<>
      <ol className="resource-cards">{page.items.map(item=><li key={item.sequence} className="resource-card"><details>
        <summary>#{item.sequence} · {labels[item.event]} · {instant(item.capturedAt)}</summary>
        <p>Thời điểm chụp: <time dateTime={item.capturedAt}>{instant(item.capturedAt)}</time> · {timeZoneId}</p>
        <p>Nguồn cập nhật: <time dateTime={item.sourceUpdatedAt}>{instant(item.sourceUpdatedAt)}</time></p>
        <p><strong>{item.title}</strong> · {item.status}</p><p>{item.recordedOn} · {item.minutes} phút</p>
        <p>Nhóm: {item.category??'—'} · Tổ chức: {item.employer??'—'}</p>
        <p style={{whiteSpace:'pre-wrap',overflowWrap:'anywhere'}}>{item.notes??'Không có ghi chú.'}</p>
      </details></li>)}</ol>
      {page.nextCursor&&<button className="secondary-button" type="button" disabled={blocked||busy} onClick={()=>void load(page.nextCursor)}>Phiên bản cũ hơn</button>}
    </>)}
  </section>;
}
