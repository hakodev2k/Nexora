import { useId, useState } from 'react';

export type FileFilters = { query: string; mediaType: string; scanState: string; lifecycle: string };
export function FileListFilters({ initial, disabled, onApply }: {
  initial: FileFilters; disabled: boolean; onApply: (value: FileFilters) => void;
}) {
  const prefix = useId(); const [draft, setDraft] = useState(initial);
  return <form className="form-panel" onSubmit={event => { event.preventDefault(); onApply({ ...draft }); }}>
    <fieldset className="resource-dialog-fields" disabled={disabled}><h2>Lọc files</h2><div className="form-grid">
      <div className="field-group"><label htmlFor={prefix + '-name'}>Tìm tên file</label><input id={prefix + '-name'} maxLength={255} value={draft.query} onChange={event => setDraft({ ...draft, query: event.target.value })} /></div>
      <div className="field-group"><label htmlFor={prefix + '-mime'}>Loại file</label><select id={prefix + '-mime'} value={draft.mediaType} onChange={event => setDraft({ ...draft, mediaType: event.target.value })}>
        <option value="">Tất cả loại</option>{['application/pdf', 'image/png', 'image/jpeg', 'image/webp', 'text/plain', 'text/markdown', 'text/csv', 'text/calendar', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'].map(value => <option key={value} value={value}>{value}</option>)}
      </select></div>
      <div className="field-group"><label htmlFor={prefix + '-scan'}>Scan state</label><select id={prefix + '-scan'} value={draft.scanState} onChange={event => setDraft({ ...draft, scanState: event.target.value })}><option value="">Tất cả scan states</option>{['Pending', 'Clean', 'Quarantined', 'Failed'].map(value => <option key={value}>{value}</option>)}</select></div>
      <div className="field-group"><label htmlFor={prefix + '-life'}>File lifecycle</label><select id={prefix + '-life'} value={draft.lifecycle} onChange={event => setDraft({ ...draft, lifecycle: event.target.value })}><option>Active</option><option>Trash</option></select></div>
    </div><button className="secondary-button" type="submit">Lọc files</button></fieldset>
  </form>;
}
