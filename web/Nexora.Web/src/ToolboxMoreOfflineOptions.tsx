import type { MoreOfflineOptions } from './ToolboxMoreOffline';

export function defaultToolboxMoreOfflineOptions(toolCode: string): MoreOfflineOptions {
  if (toolCode === 'text-diff') return { mode: 'line', newline: 'normalize' };
  if (toolCode === 'color') return { operation: 'convert', scheme: 'complementary', background: '#ffffff' };
  return {};
}

/** Screen owns drafts, clears stale output, cancellation and auth-loss cleanup. */
export function ToolboxMoreOfflineOptions({ toolCode, options, onChange, secondInput, onSecondInputChange,
  disabled = false, running = false, onCancel }: {
  toolCode: string;
  options: MoreOfflineOptions;
  onChange: (options: MoreOfflineOptions) => void;
  secondInput: string;
  onSecondInputChange: (value: string) => void;
  disabled?: boolean;
  running?: boolean;
  onCancel?: () => void;
}) {
  if (toolCode !== 'text-diff' && toolCode !== 'color') return null;
  return <>
    <p role="note">Browser only. Inputs and results stay in memory; Run checks current server permission with an empty payload.</p>
    {toolCode === 'text-diff' ? <>
      <p>The main input is Before. Compare it with After below. Results list literal equal, delete and insert text in order.</p>
      <div className="field-group">
        <label htmlFor="tool-diff-after">After / Văn bản sau</label>
        <textarea id="tool-diff-after" value={secondInput} maxLength={131072} rows={10} spellCheck={false}
          disabled={disabled} onChange={event => onSecondInputChange(event.target.value)} aria-describedby="tool-diff-limits" />
      </div>
      <div className="field-group"><label htmlFor="tool-diff-mode">Compare by / Chế độ so sánh</label>
        <select id="tool-diff-mode" value={options.mode ?? 'line'} disabled={disabled}
          onChange={event => onChange({ ...options, mode: event.target.value })}>
          <option value="line">Line</option><option value="character">Unicode scalar (not grapheme)</option>
        </select>
      </div>
      <div className="field-group"><label htmlFor="tool-diff-newline">Newline handling / Xử lý xuống dòng</label>
        <select id="tool-diff-newline" value={options.newline ?? 'normalize'} disabled={disabled}
          onChange={event => onChange({ ...options, newline: event.target.value })}>
          <option value="normalize">Normalize CRLF / CR to LF</option><option value="preserve">Preserve exact terminators</option>
        </select>
      </div>
      <p id="tool-diff-limits">128 KiB UTF-8 per side; 4,096 tokens per side and 1,000,000 comparisons. Reduce input or use line mode when limits are exceeded.</p>
    </> : <>
      <p>Input: #RGB, #RGBA, #RRGGBB, #RRGGBBAA, comma rgb/rgba or hsl/hsla. RGB channels are integers 0–255; HSL uses hue 0–360 and percentages; alpha is 0–1.</p>
      <div className="field-group"><label htmlFor="tool-color-operation">Color operation / Thao tác màu</label>
        <select id="tool-color-operation" value={options.operation ?? 'convert'} disabled={disabled}
          onChange={event => onChange({ ...options, operation: event.target.value })}>
          <option value="convert">Convert HEX / RGB / HSL / alpha</option><option value="palette">Derive palette</option>
        </select>
      </div>
      {options.operation === 'palette' && <div className="field-group"><label htmlFor="tool-color-scheme">Palette scheme / Bảng màu</label>
        <select id="tool-color-scheme" value={options.scheme ?? 'complementary'} disabled={disabled}
          onChange={event => onChange({ ...options, scheme: event.target.value })}>
          <option value="complementary">Complementary (2)</option><option value="analogous">Analogous (3)</option>
          <option value="triadic">Triadic (3)</option><option value="monochromatic">Monochromatic (5 lightness levels)</option>
        </select>
      </div>}
      <div className="field-group"><label htmlFor="tool-color-background">Opaque contrast background / Màu nền tương phản</label>
        <input id="tool-color-background" value={options.background ?? '#ffffff'} maxLength={256} disabled={disabled}
          spellCheck={false} onChange={event => onChange({ ...options, background: event.target.value })}
          aria-describedby="tool-color-limits" />
      </div>
      <p id="tool-color-limits">256 characters per color. Numeric sRGB contrast preview; AA/AAA thresholds use unrounded ratios. HSL/RGB and HEX alpha can round. Derived palettes may repeat colors and do not certify accessibility.</p>
    </>}
    {running && onCancel && <button type="button" className="secondary-button" onClick={onCancel}>Cancel processing / Hủy xử lý</button>}
  </>;
}
