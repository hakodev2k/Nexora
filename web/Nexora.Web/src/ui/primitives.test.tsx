import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { Button, Card, FormField, Toolbar } from './primitives';

describe('shared UI primitives', () => {
  it('associates the native control with label, hint and authoritative validation error', () => {
    render(<FormField id="title" label="Tiêu đề" hint="Tối đa 200 ký tự" error="Bắt buộc nhập">{props => <input {...props} />}</FormField>);
    const input = screen.getByRole('textbox', { name: 'Tiêu đề' });
    expect(input).toHaveAccessibleDescription('Tối đa 200 ký tự Bắt buộc nhập');
    expect(input).toHaveAttribute('aria-invalid', 'true');
  });
  it('generates distinct label associations and does not announce a nonexistent error', () => {
    render(<><FormField label="Tên">{props => <input {...props} />}</FormField><FormField label="Ghi chú">{props => <textarea {...props} />}</FormField></>);
    expect(screen.getByRole('textbox', { name: 'Tên' }).id).not.toBe(screen.getByRole('textbox', { name: 'Ghi chú' }).id);
    expect(screen.getByRole('textbox', { name: 'Tên' })).not.toHaveAttribute('aria-invalid');
  });
  it('does not accidentally submit a parent form and preserves explicit submit and disabled behavior', () => {
    const submit = vi.fn(e => e.preventDefault()), click = vi.fn();
    render(<form onSubmit={submit}><Button>Hủy</Button><Button type="submit">Lưu</Button><Button disabled onClick={click}>Không khả dụng</Button></form>);
    fireEvent.click(screen.getByRole('button', { name: 'Hủy' })); expect(submit).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Không khả dụng' })); expect(click).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: 'Lưu' })); expect(submit).toHaveBeenCalledOnce();
  });
  it('composes a semantic article without an extra wrapper or invented toolbar keyboard contract', () => {
    render(<Card asChild><article aria-label="Mục"><Toolbar aria-label="Tác vụ"><Button>Sửa</Button></Toolbar></article></Card>);
    expect(screen.getByRole('article', { name: 'Mục' })).toHaveClass('nx-card');
    expect(screen.queryByRole('toolbar')).not.toBeInTheDocument();
  });
});
