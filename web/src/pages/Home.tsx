import { useMemo } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useLogEntries } from "@/hooks/catalog-hooks";

function gradeValue(grade: string): number {
  const n = parseInt(grade.replace(/\D/g, ""), 10);
  return Number.isNaN(n) ? -1 : n;
}

export default function Home() {
  const { entries } = useLogEntries();

  const stats = useMemo(() => {
    const completed = entries.filter((e) => e.status === "Completed");
    const hardest = completed
      .map((e) => e.climb?.grade)
      .filter((g): g is string => !!g)
      .sort((a, b) => gradeValue(b) - gradeValue(a))[0];

    const now = new Date();
    const thisMonth = entries.filter((e) => {
      const d = new Date(e.occurredAt);
      return (
        d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth()
      );
    });

    return {
      total: completed.length,
      hardest: hardest ?? "-",
      thisMonth: thisMonth.length,
    };
  }, [entries]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-4xl mx-auto space-y-6">
        <div className="text-center space-y-2">
          <h1 className="text-5xl font-bold text-slate-900 tracking-tight">
            Climbing Logbook
          </h1>
          <p className="text-slate-600">Track your climbing journey</p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Climbs Sent</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-3xl font-bold text-slate-900">{stats.total}</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Hardest Grade</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-3xl font-bold text-slate-900">
                {stats.hardest}
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">This Month</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-3xl font-bold text-slate-900">
                {stats.thisMonth}
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
