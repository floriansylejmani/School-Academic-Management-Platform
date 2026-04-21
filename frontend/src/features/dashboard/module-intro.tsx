import { Card } from "@/components/ui/card";

interface ModuleIntroProps {
  eyebrow: string;
  title: string;
  description: string;
  bullets: string[];
}

export function ModuleIntro({ eyebrow, title, description, bullets }: ModuleIntroProps) {
  return (
    <div className="space-y-6">
      <Card className="bg-dashboard-glow p-6 lg:p-8">
        <p className="text-xs font-semibold uppercase tracking-[0.28em] text-brand-700">{eyebrow}</p>
        <h2 className="mt-3 text-3xl font-semibold text-slate-950">{title}</h2>
        <p className="mt-4 max-w-3xl text-sm leading-7 text-slate-600">{description}</p>
      </Card>

      <div className="grid gap-4 lg:grid-cols-3">
        {bullets.map((bullet) => (
          <Card key={bullet} className="p-5">
            <p className="text-sm leading-7 text-slate-600">{bullet}</p>
          </Card>
        ))}
      </div>
    </div>
  );
}
