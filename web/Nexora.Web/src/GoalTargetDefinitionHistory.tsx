import { useEffect, useRef, useState } from 'react';
import { getGoalTargetDefinitionHistory, NexoraApiError, type GoalTargetDefinitionPage } from './api';

export function GoalTargetDefinitionHistory({goalId,timeZoneId,locale,onAuthLost}:{goalId:string;timeZoneId:string;locale:string;onAuthLost:()=>Promise<void>}) {
  const [page,setPage]=useState<GoalTargetDefinitionPage|null>(null),[busy,setBusy]=useState(false),[error,setError]=useState<string|null>(null);
  const generation=useRef(0),controller=useRef<AbortController|null>(null),flight=useRef(false);
  useEffect(()=>()=>{++generation.current;controller.current?.abort();},[]);
  async function load(cursor:string|null=null) {
    if(flight.current)return;
    const epoch=++generation.current;controller.current?.abort();const abort=new AbortController();controller.current=abort;
    flight.current=true;setBusy(true);setPage(null);setError(null);
    try{const next=await getGoalTargetDefinitionHistory(goalId,cursor,abort.signal);if(epoch===generation.current)setPage(next);}
    catch(value){if(epoch===generation.current&&!abort.signal.aborted){const api=value instanceof NexoraApiError?value:null;setError(api?.message??'Không thể tải định nghĩa target.');if(api?.status===401)await onAuthLost();}}
    finally{if(epoch===generation.current){flight.current=false;setBusy(false);}}
  }
  function time(value:string){try{return new Intl.DateTimeFormat(locale==='en'?'en-US':'vi-VN',{timeZone:timeZoneId,dateStyle:'medium',timeStyle:'long'}).format(new Date(value));}catch{return value;}}
  return <section className="form-panel" aria-labelledby="goal-target-history-title">
    <div className="section-heading"><h3 id="goal-target-history-title">Phiên bản định nghĩa target</h3><button className="secondary-button" type="button" disabled={busy} onClick={()=>void load()}>Tải phiên bản từ đầu</button></div>
    <p className="muted">Tên và định nghĩa Numeric/Boolean tại thời điểm lưu, kể cả target đã bỏ. Mốc “Bắt đầu lưu lịch sử” chụp nội dung hiện có; không tạo lại các thay đổi cũ. Giá trị tiến độ có lịch sử riêng.</p>
    {busy&&<p role="status">Đang tải phiên bản…</p>}{error&&<p role="alert">{error}</p>}
    {page&&(page.items.length===0?<p>Chưa có phiên bản định nghĩa nào được ghi lại.</p>:<>
      <ol className="resource-cards">{page.items.map(item=><li className="resource-card" key={item.id}><details>
        <summary>{item.title} · {item.event==='Created'?'Thêm target':item.event==='Updated'?'Sửa định nghĩa':item.event==='Removed'?'Bỏ target':'Bắt đầu lưu lịch sử'} · {time(item.capturedAt)}</summary>
        <p>{item.kind}{item.removed?' · Đã bỏ khỏi mẫu số hiện tại':''}</p>
        {item.kind==='Numeric'&&<p>Ban đầu: {item.initialValueExact} · Đích: {item.targetValueExact}</p>}
        <p>Thời điểm chụp: <time dateTime={item.capturedAt}>{time(item.capturedAt)}</time> · {timeZoneId}</p>
        <p>Nguồn cập nhật: <time dateTime={item.sourceUpdatedAt}>{time(item.sourceUpdatedAt)}</time></p>
      </details></li>)}</ol>
      {page.nextCursor&&<button className="secondary-button" type="button" disabled={busy} onClick={()=>void load(page.nextCursor)}>Phiên bản cũ hơn</button>}
    </>)}
  </section>;
}
