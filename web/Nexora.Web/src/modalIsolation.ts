// A background can be hidden by several nested portals. Restore its original
// attributes only after the final modal releases it, regardless of cleanup order.
const backgrounds = new Map<Element, { count: number; hidden: string | null; inert: boolean }>();
export function isolateModalLayer(layer: Element) {
  const elements = Array.from(document.body.children).filter(element => element !== layer);
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
  };
}
