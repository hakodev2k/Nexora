import { useEffect, useId, useRef, useState, type ReactNode } from 'react';
import { createPortal } from 'react-dom';

export function useResourceDialog<T>(value: T, restore?: (baseline: T) => void) {
  const [open, setOpen] = useState(false);
  const baseline = useRef('');
  const baselineValue = useRef(value);
  return {
    open,
    dirty: JSON.stringify(value) !== baseline.current,
    begin(next: T = value) { baselineValue.current = next; baseline.current = JSON.stringify(next); setOpen(true); },
    finish() { setOpen(false); },
    cancel() { restore?.(baselineValue.current); setOpen(false); }
  };
}

export function ResourceFormDialog(props: {
  open: boolean; title: string; busy: boolean; dirty: boolean;
  onClose: () => void; children: ReactNode;
}) {
  return props.open ? <OpenFormDialog {...props} /> : null;
}

function OpenFormDialog({ title, busy, dirty, onClose, children }: {
  title: string; busy: boolean; dirty: boolean; onClose: () => void; children: ReactNode;
}) {
  const titleId = useId();
  const layer = useRef<HTMLDivElement>(null);
  const panel = useRef<HTMLDivElement>(null);
  const trigger = useRef(document.activeElement instanceof HTMLElement ? document.activeElement : null);
  const state = useRef({ busy, dirty, onClose });
  state.current = { busy, dirty, onClose };
  const [discard, setDiscard] = useState(false);
  const resume = useRef<HTMLButtonElement>(null);
  function requestClose() {
    if (state.current.busy) return;
    if (state.current.dirty) setDiscard(true); else state.current.onClose();
  }
  useEffect(() => {
    const backgrounds = Array.from(document.body.children).filter(el => el !== layer.current);
    const previous = backgrounds.map(el => ({ el, hidden: el.getAttribute('aria-hidden'), inert: el.hasAttribute('inert') }));
    for (const { el } of previous) { el.setAttribute('inert', ''); el.setAttribute('aria-hidden', 'true'); }
    const focusable = () => Array.from(panel.current?.querySelectorAll<HTMLElement>('button:not(:disabled),input:not(:disabled),textarea:not(:disabled),select:not(:disabled),[href]') ?? []).filter(el => !el.closest('[hidden]'));
    (focusable()[0] ?? panel.current)?.focus();
    function keyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') { event.preventDefault(); requestClose(); }
      if (event.key !== 'Tab') return;
      const items = focusable(), first = items[0], last = items[items.length - 1];
      if (items.length === 0) { event.preventDefault(); panel.current?.focus(); return; }
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
    }
    const current = panel.current;
    current?.addEventListener('keydown', keyDown);
    return () => {
      current?.removeEventListener('keydown', keyDown);
      for (const { el, hidden, inert } of previous) {
        if (hidden === null) el.removeAttribute('aria-hidden'); else el.setAttribute('aria-hidden', hidden);
        if (!inert) el.removeAttribute('inert');
      }
      queueMicrotask(() => { if (trigger.current?.isConnected) trigger.current.focus(); });
    };
  }, []);
  useEffect(() => {
    if (discard) resume.current?.focus();
    else if (busy) panel.current?.focus();
    else panel.current?.querySelector<HTMLElement>('input:not(:disabled),select:not(:disabled),textarea:not(:disabled),button:not(:disabled)')?.focus();
  }, [discard, busy]);
  return createPortal(<div ref={layer} className="dialog-backdrop" role="presentation">
    <div ref={panel} className="dialog-panel resource-form-dialog" role="dialog" aria-modal="true" aria-labelledby={titleId} tabIndex={-1}>
      <h2 id={titleId}>{title}</h2>
      <fieldset className="resource-dialog-fields" hidden={discard} disabled={busy}>{children}</fieldset>
      {discard ? <div role="alert"><p>Bỏ thay đổi chưa lưu?</p><div className="dialog-actions">
        <button ref={resume} type="button" className="secondary-button" onClick={() => setDiscard(false)}>Tiếp tục sửa</button>
        <button type="button" className="danger-button" disabled={busy} onClick={onClose}>Bỏ bản nháp</button>
      </div></div> : <div className="dialog-actions"><button className="secondary-button" type="button" disabled={busy} onClick={requestClose}>Hủy</button></div>}
    </div>
  </div>, document.body);
}
