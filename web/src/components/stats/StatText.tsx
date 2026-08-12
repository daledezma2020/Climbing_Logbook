export function StatValue({ children }: { children: React.ReactNode }) {
  return <p className="text-3xl font-bold text-slate-900">{children}</p>;
}

export function StatHint({ children }: { children: React.ReactNode }) {
  return <p className="text-sm text-slate-600">{children}</p>;
}

export function StatEmpty({ children }: { children: React.ReactNode }) {
  return <p className="text-sm text-slate-500">{children}</p>;
}
