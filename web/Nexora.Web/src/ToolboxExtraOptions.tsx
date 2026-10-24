export type ToolboxExtraOptionMap = Record<string, string>;

export function defaultToolboxExtraOptions(toolCode: string): ToolboxExtraOptionMap {
  switch (toolCode) {
    case 'xml': return { indent: 'true' };
    case 'datetime': return { unit: '' };
    case 'csv': return { delimiter: 'comma', header: 'false', quote: 'double', encoding: 'utf-8' };
    default: return {};
  }
}

/** Controlled fields: the integrating screen clears previous output on every change. */
export function ToolboxExtraOptions({ toolCode, options, onChange, disabled = false }: {
  toolCode: string;
  options: ToolboxExtraOptionMap;
  onChange: (options: ToolboxExtraOptionMap) => void;
  disabled?: boolean;
}) {
  if (toolCode === 'datetime') return <>
    <div className="field-group">
      <label htmlFor="tool-epoch-unit">Epoch unit / Đơn vị epoch (bắt buộc)</label>
      <select id="tool-epoch-unit" value={options.unit ?? ''} onChange={event => onChange({ unit: event.target.value })} disabled={disabled} required>
        <option value="">Select unit / Chọn đơn vị</option>
        <option value="seconds">Seconds / Giây</option>
        <option value="milliseconds">Milliseconds / Mili giây</option>
      </select>
    </div>
    <p>Signed integer input; output UTC (Z), years 0001–9999. No automatic unit guessing.</p>
  </>;
  if (toolCode === 'xml') return <>
    <label className="check-row"><input type="checkbox" checked={(options.indent ?? 'true') === 'true'} disabled={disabled}
      onChange={event => onChange({ indent: String(event.target.checked) })} /><span>Indent XML / Thụt lề XML</span></label>
    <p>DTD and external entities prohibited. Whitespace and mixed content retained; lexical layout may change.</p>
  </>;
  if (toolCode === 'csv') return <>
    <div className="field-group"><label htmlFor="tool-csv-delimiter">CSV delimiter / Dấu phân cách</label>
      <select id="tool-csv-delimiter" value={options.delimiter ?? 'comma'} disabled={disabled}
        onChange={event => onChange({ ...options, delimiter: event.target.value })}>
        <option value="comma">Comma (,)</option><option value="semicolon">Semicolon (;)</option><option value="tab">Tab</option>
      </select>
    </div>
    <label className="check-row"><input type="checkbox" checked={options.header === 'true'} disabled={disabled}
      onChange={event => onChange({ ...options, header: String(event.target.checked) })} /><span>First record is header / Dòng đầu là tiêu đề</span></label>
    <p>UTF-8 · double-quote (&quot;) · 1,000 records / 100 columns / 50,000 cells. JSON text display; formulas are never executed. Pasting into spreadsheets can execute formulas.</p>
  </>;
  if (toolCode === 'jwt') return <p role="note">Browser only / Chỉ trình duyệt · Unverified. Decode is not signature verification. Token stays in memory and is never sent to the API. Use synthetic tokens only.</p>;
  return null;
}
