import { describe, expect, it, vi } from 'vitest';
import { isolateModalLayer } from './modalIsolation';

describe('modal background isolation', () => {
  it('restores mixed overflow axes and priorities without relying on CSSOM shorthand serialization', () => {
    const root = document.documentElement, body = document.body;
    const rootStyle = root.getAttribute('style'), bodyStyle = body.getAttribute('style');
    root.style.setProperty('overflow-x', 'scroll', 'important'); root.style.setProperty('overflow-y', 'auto');
    body.style.setProperty('overflow-x', 'clip'); body.style.setProperty('overflow-y', 'scroll', 'important');
    const layer = document.createElement('div'); body.appendChild(layer);
    const release = isolateModalLayer(layer);
    try {
      release();
      expect(root.style.getPropertyValue('overflow-x')).toBe('scroll'); expect(root.style.getPropertyPriority('overflow-x')).toBe('important');
      expect(root.style.getPropertyValue('overflow-y')).toBe('auto'); expect(root.style.getPropertyPriority('overflow-y')).toBe('');
      expect(body.style.getPropertyValue('overflow-x')).toBe('clip'); expect(body.style.getPropertyPriority('overflow-x')).toBe('');
      expect(body.style.getPropertyValue('overflow-y')).toBe('scroll'); expect(body.style.getPropertyPriority('overflow-y')).toBe('important');
    } finally {
      release(); layer.remove();
      if (rootStyle === null) root.removeAttribute('style'); else root.setAttribute('style', rootStyle);
      if (bodyStyle === null) body.removeAttribute('style'); else body.setAttribute('style', bodyStyle);
    }
  });
  it('locks document scrolling without losing pre-existing inline styles or gutter spacing', () => {
    const root = document.documentElement, body = document.body;
    const originalRoot = root.getAttribute('style'), originalBody = body.getAttribute('style');
    root.style.setProperty('overflow', 'scroll', 'important'); body.style.overflow = 'auto'; body.style.setProperty('padding-right', '7px', 'important');
    const width = vi.spyOn(root, 'clientWidth', 'get').mockReturnValue(window.innerWidth - 20);
    const layer = document.createElement('div'); body.appendChild(layer);
    const release = isolateModalLayer(layer);
    try {
      expect(root.style.overflow).toBe('hidden'); expect(body.style.overflow).toBe('hidden');
      expect(body.style.paddingRight).toBe('27px');
      release();
      expect(root.style.overflow).toBe('scroll'); expect(root.style.getPropertyPriority('overflow')).toBe('important');
      expect(body.style.overflow).toBe('auto'); expect(body.style.paddingRight).toBe('7px'); expect(body.style.getPropertyPriority('padding-right')).toBe('important');
    } finally {
      release(); width.mockRestore(); layer.remove();
      if (originalRoot === null) root.removeAttribute('style'); else root.setAttribute('style', originalRoot);
      if (originalBody === null) body.removeAttribute('style'); else body.setAttribute('style', originalBody);
    }
  });
  it('keeps the background locked until the final nested portal closes in either cleanup order', () => {
    for (const parentFirst of [false, true]) {
      const background = document.createElement('main'), parent = document.createElement('div'), child = document.createElement('div');
      background.setAttribute('aria-hidden', 'false'); background.setAttribute('inert', '');
      document.body.append(background, parent);
      const releaseParent = isolateModalLayer(parent); document.body.appendChild(child);
      const releaseChild = isolateModalLayer(child);
      try {
        const first = parentFirst ? releaseParent : releaseChild, last = parentFirst ? releaseChild : releaseParent;
        first(); first(); // Effect cleanup must be idempotent.
        expect(document.documentElement.style.overflow).toBe('hidden'); expect(document.body.style.overflow).toBe('hidden');
        expect(background).toHaveAttribute('aria-hidden', 'true'); expect(background).toHaveAttribute('inert');
        last();
        expect(document.documentElement.style.overflow).toBe(''); expect(document.body.style.overflow).toBe('');
        expect(background).toHaveAttribute('aria-hidden', 'false'); expect(background).toHaveAttribute('inert');
      } finally { releaseParent(); releaseChild(); background.remove(); parent.remove(); child.remove(); }
    }
  });
});
