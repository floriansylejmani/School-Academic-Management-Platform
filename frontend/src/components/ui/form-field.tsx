export function FormField({
  label,
  htmlFor,
  required,
  hint,
  error,
  children
}: {
  label: string;
  htmlFor?: string;
  required?: boolean;
  hint?: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <label
        htmlFor={htmlFor}
        className="flex items-center gap-1 text-sm font-medium text-slate-700"
      >
        {label}
        {required ? (
          <span className="text-rose-500" aria-hidden="true">
            *
          </span>
        ) : null}
      </label>
      {hint ? <p className="text-xs text-slate-500">{hint}</p> : null}
      {children}
      {error ? <p className="text-xs text-rose-600">{error}</p> : null}
    </div>
  );
}
