import { useEffect, useRef, useState } from 'react';
import { getGoalMetadataHistory, NexoraApiError, type GoalMetadataPage } from './api';

export function GoalMetadataHistory({goalId,timeZoneId,locale,onAuthLost}:{goalId:string;timeZoneId:string;locale:string;onAuthLost:()=>Promise<void>}) {
  const [page,setPage]=useState<GoalMetadataPage|null>(null),[busy,setBusy]=useState(false),[error,setError]=useState<string|null>(null);
  const generation=useRef(0),controller=useRef<AbortController|null>(null),flight=useRef(false);
  useEffect(()=>()=>{++generation.current;controller.current?.abort();},[]);
  async function load(cursor:string|null=null) {
    if(flight.current)return;
    const epoch=++generation.current;controller.current?.abort();const abort=new AbortController();controller.current=abort;
    flight.current=true;setBusy(true);setPage(null);setError(null);
    try{const next=await getGoalMetadataHistory(goalId,cursor,abort.signal);if(epoch===generation.current)setPage(next);}
    catch(value){if(epoch===generation.current&&!abort.signal.aborted){const api=value instanceof NexoraApiError?value:null;setError(api?.message??'Không thể tải phiên bản Goal.');if(api?.status===401)await onAuthLost();}}
    finally{if(epoch===generation.current){flight.current=false;setBusy(false);}}
  }
  function time(value:string){try{return new Intl.DateTimeFormat(locale==='en'?'en-US':'vi-VN',{timeZone:timeZoneId,dateStyle:'medium',timeStyle:'long'}).format(new Date(value));}catch{return value;}}
  return <section className="form-panel" aria-labelledby="goal-metadata-history-title">
    <div className="section-heading"><h3 id="goal-metadata-history-title">Phiên bản thông tin Goal</h3><button className="secondary-button" type="button" disabled={busy} onClick={()=>void load()}>Tải phiên bản từ đầu</button></div>
    <p className="muted">Title, mô tả và khoảng ngày đã lưu từ khi bật lịch sử. “Bắt đầu lưu lịch sử” chụp nội dung hiện có, không tạo lại các lần sửa cũ. Không gồm phiên bản định nghĩa target.</p>
    {busy&&<p role="status">Đang tải phiên bản…</p>}{error&&<p role="alert">{error}</p>}
    {page&&(page.items.length===0?<p>Chưa có phiên bản thông tin nào được ghi lại.</p>:<>
      <ol className="resource-cards">{page.items.map(item=><li className="resource-card" key={item.id}><details><summary>{item.event==='Created'?'Tạo Goal':item.event==='Updated'?'Sửa thông tin':'Bắt đầu lưu lịch sử'} · {time(item.capturedAt)}</summary>
        <p>Thời điểm chụp: <time dateTime={item.capturedAt}>{time(item.capturedAt)}</time> · {timeZoneId}</p>
        <p>Nguồn cập nhật: <time dateTime={item.sourceUpdatedAt}>{time(item.sourceUpdatedAt)}</time></p>
        <h4>{item.title}</h4><p>{item.startDate??'Chưa đặt ngày bắt đầu'} → {item.endDate??'Chưa đặt ngày kết thúc'}</p>
        <p style={{whiteSpace:'pre-wrap',overflowWrap:'anywhere'}}>{item.description??'Không có mô tả.'}</p>
      </details></li>)}</ol>
      {page.nextCursor&&<button className="secondary-button" type="button" disabled={busy} onClick={()=>void load(page.nextCursor)}>Phiên bản cũ hơn</button>}
    </>)}
  </section>;
}
