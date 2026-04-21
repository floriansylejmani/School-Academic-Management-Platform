import { LogoMark } from "@/components/layout/logo-mark";

export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="grid min-h-screen lg:grid-cols-[0.95fr_1.05fr]">
      <section className="relative hidden overflow-hidden bg-slate-950 px-10 py-12 text-white lg:flex lg:flex-col lg:justify-between">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(28,130,242,0.35),transparent_28%),radial-gradient(circle_at_bottom_right,rgba(224,210,173,0.18),transparent_24%)]" />
        <div className="relative">
          <LogoMark />
        </div>
        <div className="relative max-w-xl">
          <p className="text-sm font-semibold uppercase tracking-[0.32em] text-brand-300">Modern School Ops</p>
          <h2 className="mt-4 text-5xl font-semibold leading-[1.05]">
            One workspace for students, teachers, parents, and school leadership.
          </h2>
          <p className="mt-6 text-base leading-8 text-white/72">
            Track attendance, manage classes, publish results, and coordinate academic operations from a responsive,
            role-aware dashboard.
          </p>
        </div>
      </section>

      <section className="flex min-h-screen items-center justify-center px-4 py-10 sm:px-6 lg:px-12">{children}</section>
    </div>
  );
}
