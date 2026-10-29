import { useId, type ComponentPropsWithRef, type HTMLAttributes, type ReactElement, type ReactNode } from 'react';
import { Root as Label } from '@radix-ui/react-label';
import { Slot } from '@radix-ui/react-slot';

function classes(...values: (string | undefined)[]) { return values.filter(Boolean).join(' '); }

/** Native button semantics, including explicit submit and disabled behavior. */
export function Button({ variant = 'secondary', className, type = 'button', ...props }: ComponentPropsWithRef<'button'> & {
  variant?: 'primary' | 'secondary' | 'danger' | 'quiet';
}) {
  return <button type={type} className={classes('nx-button', `nx-button--${variant}`, className)} {...props} />;
}

export function Toolbar({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  // A wrapping layout, deliberately not an ARIA toolbar with arrow-key semantics.
  return <div className={classes('nx-toolbar', className)} {...props} />;
}

export function Card({ asChild = false, className, ...props }: HTMLAttributes<HTMLDivElement> & { asChild?: boolean }) {
  const Element = asChild ? Slot : 'div';
  return <Element className={classes('nx-card', className)} {...props} />;
}

type FieldControlProps = { id: string; 'aria-describedby'?: string; 'aria-invalid'?: true };

/** The consumer keeps its native input and business validation. */
export function FormField({ id, label, hint, error, optionalLabel, className, children }: {
  id?: string; label: ReactNode; hint?: ReactNode; error?: ReactNode; optionalLabel?: string;
  className?: string; children: (props: FieldControlProps) => ReactElement;
}) {
  const generated = useId();
  const controlId = id ?? `field-${generated}`;
  const describedBy = [hint ? `${controlId}-hint` : '', error ? `${controlId}-error` : ''].filter(Boolean).join(' ') || undefined;
  return <div className={classes('nx-field', className)}>
    <Label htmlFor={controlId} className="nx-field__label">{label}{optionalLabel && <span className="nx-field__optional"> {optionalLabel}</span>}</Label>
    {children({ id: controlId, 'aria-describedby': describedBy, 'aria-invalid': error ? true : undefined })}
    {hint && <p id={`${controlId}-hint`} className="nx-field__hint">{hint}</p>}
    {error && <p id={`${controlId}-error`} className="nx-field__error">{error}</p>}
  </div>;
}
