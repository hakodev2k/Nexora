// A background can be hidden by several nested portals. Restore its original
// attributes only after the final modal releases it, regardless of cleanup order.
const backgrounds = new Map<Element, { count: number; hidden: string | null; inert: boolean }>();

type InlineStyle = { value: string; priority: string };
type OverflowStyle = { shorthand: InlineStyle; x: InlineStyle; y: InlineStyle };
const scrollLocks = new WeakMap<Document, {
  count: number; root: HTMLElement; body: HTMLElement;
  rootOverflow: OverflowStyle; bodyOverflow: OverflowStyle; bodyPadding: InlineStyle;
}>();
function previousStyle(element: HTMLElement, property: string): InlineStyle {
  return { value: element.style.getPropertyValue(property), priority: element.style.getPropertyPriority(property) };
}
function restoreStyle(element: HTMLElement, property: string, previous: InlineStyle) {
  if (previous.value) element.style.setProperty(property, previous.value, previous.priority);
  else element.style.removeProperty(property);
}
function previousOverflow(element: HTMLElement): OverflowStyle {
  return { shorthand: previousStyle(element, 'overflow'), x: previousStyle(element, 'overflow-x'), y: previousStyle(element, 'overflow-y') };
}
function restoreOverflow(element: HTMLElement, previous: OverflowStyle) {
  // Mixed axis priorities cannot be represented by the CSSOM shorthand. Preserve
  // each axis; also retain shorthand state for DOM implementations that do not
  // expand shorthand declarations into individual getPropertyValue results.
  element.style.removeProperty('overflow');
  restoreStyle(element, 'overflow', previous.shorthand);
  restoreStyle(element, 'overflow-x', previous.x);
  restoreStyle(element, 'overflow-y', previous.y);
}
function lockBackgroundScroll(doc: Document) {
  const active = scrollLocks.get(doc);
  if (active) active.count++;
  else {
    const root = doc.documentElement, body = doc.body;
    const rootOverflow = previousOverflow(root), bodyOverflow = previousOverflow(body), bodyPadding = previousStyle(body, 'padding-right');
    const view = doc.defaultView;
    const gutter = root.clientWidth > 0 && view ? Math.max(0, view.innerWidth - root.clientWidth) : 0;
    const padding = view ? parseFloat(view.getComputedStyle(body).paddingRight) || 0 : 0;
    scrollLocks.set(doc, { count: 1, root, body, rootOverflow, bodyOverflow, bodyPadding });
    root.style.setProperty('overflow', 'hidden', rootOverflow.shorthand.priority);
    body.style.setProperty('overflow', 'hidden', bodyOverflow.shorthand.priority);
    if (gutter > 0) body.style.setProperty('padding-right', `${padding + gutter}px`, bodyPadding.priority);
  }
  return () => {
    const current = scrollLocks.get(doc);
    if (!current || --current.count > 0) return;
    restoreOverflow(current.root, current.rootOverflow);
    restoreOverflow(current.body, current.bodyOverflow);
    restoreStyle(current.body, 'padding-right', current.bodyPadding);
    scrollLocks.delete(doc);
  };
}

export function isolateModalLayer(layer: Element) {
  const doc = layer.ownerDocument;
  const releaseScroll = lockBackgroundScroll(doc);
  const elements = Array.from(doc.body.children).filter(element => element !== layer);
  for (const element of elements) {
    const previous = backgrounds.get(element);
    if (previous) previous.count++;
    else backgrounds.set(element, { count: 1, hidden: element.getAttribute('aria-hidden'), inert: element.hasAttribute('inert') });
    element.setAttribute('aria-hidden', 'true'); element.setAttribute('inert', '');
  }
  let released = false;
  return () => {
    if (released) return; released = true;
    for (const element of elements) {
      const previous = backgrounds.get(element);
      if (!previous || --previous.count > 0) continue;
      if (previous.hidden === null) element.removeAttribute('aria-hidden'); else element.setAttribute('aria-hidden', previous.hidden);
      if (!previous.inert) element.removeAttribute('inert');
      backgrounds.delete(element);
    }
    releaseScroll();
  };
}
