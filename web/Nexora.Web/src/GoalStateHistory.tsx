import { useEffect, useRef, useState } from 'react';
import { getGoalStateHistory, NexoraApiError, type GoalStatePage } from './api';

const labels:Record<string,string>={'goals.goal.start':'Bắt đầu','goals.goal.complete':'Hoàn thành','goals.goal.abandon':'Dừng mục tiêu','goals.goal.reopen':'Mở lại','goals.goal.archive':'Archive','goals.goal.unarchive':'Unarchive'};

export function GoalStateHistory({goalId,timeZoneId,locale,onAuthLost}:{goalId:string;timeZoneId:string;locale:string;onAuthLost:()=>Promise<void>}) {
  const [page,setPage]=useState<GoalStatePage|null>(null), [busy,setBusy]=useState(false), [error,setError]=useState<string|null>(null);
  const generation=useRef(0),controller=useRef<AbortController|null>(null),flight=useRef(false);
  useEffect(()=>()=>{++generation.current;controller.current?.abort();},[]);
  async function load(cursor:string|null=null) {
    if(flight.current) return;
    const epoch=++generation.current;controller.current?.abort();const abort=new AbortController();controller.current=abort;
    flight.current=true;setBusy(true);setPage(null);setError(null);
    try {const value=await getGoalStateHistory(goalId,cursor,abort.signal);if(epoch===generation.current)setPage(value);}
    catch(value) {if(epoch===generation.current&&!abort.signal.aborted){
      const api=value instanceof NexoraApiError?value:null;setError(api?.message??'Không thể tải lịch sử trạng thái.');if(api?.status===401)await onAuthLost();
    }}
    finally {if(epoch===generation.current){flight.current=false;setBusy(false);}}
  }
  function time(value:string) {try{return new Intl.DateTimeFormat(locale==='en'?'en-US':'vi-VN',{timeZone:timeZoneId,dateStyle:'medium',timeStyle:'long'}).format(new Date(value));}catch{return value;}}
  return <section className="form-panel" aria-labelledby="goal-state-history-title">
    <div className="section-heading"><h3 id="goal-state-history-title">Lịch sử trạng thái</h3><button className="secondary-button" type="button" disabled={busy} onClick={()=>void load()}>Tải lịch sử từ đầu</button></div>
    <p className="muted">Những lần đổi trạng thái được lưu từ khi bật lịch sử; không tạo lại các thao tác cũ. Lịch sử chỉnh sửa nội dung Goal chưa nằm trong bảng này. Múi giờ: {timeZoneId}.</p>
    {busy&&<p role="status">Đang tải lịch sử…</p>}{error&&<p role="alert">{error}</p>}
    {page&&(page.items.length===0?<p>Chưa có lần đổi trạng thái nào được ghi lại.</p>:<>
      <div className="table-wrap"><table><caption className="sr-only">Lịch sử chuyển trạng thái Goal</caption><thead><tr><th scope="col">Thời điểm</th><th scope="col">Thao tác</th><th scope="col">Từ</th><th scope="col">Đến</th></tr></thead><tbody>
        {page.items.map(item=><tr key={item.id}><td><time dateTime={item.recordedAt}>{time(item.recordedAt)}</time></td><td>{labels[item.actionKey]}</td><td>{item.fromStatus}</td><td>{item.toStatus}</td></tr>)}
      </tbody></table></div>
      {page.nextCursor&&<button className="secondary-button" type="button" disabled={busy} onClick={()=>void load(page.nextCursor)}>Trang tiếp</button>}
    </>)}
  </section>;
}
