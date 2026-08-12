import { useState } from "react";
import { Link } from "react-router-dom";
import { useAuth0 } from "@auth0/auth0-react";
import { Button } from "@/components/ui/button";
import StatCard from "@/components/stats/StatCard";
import ErrorToast from "@/components/log/ErrorToast";
import FeedCard from "@/components/feed/FeedCard";
import KpiCustomizer from "@/components/home/KpiCustomizer";
import { KPI_CATALOG } from "@/components/home/kpi-catalog";
import { useKpiPreferences } from "@/components/home/useKpiPreferences";
import { useFeed, useHomeStats } from "@/hooks/social-hooks";
import { useCurrentUser } from "@/hooks/user-hooks";

function Shell({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-4xl mx-auto space-y-6">{children}</div>
    </div>
  );
}

function Hero() {
  return (
    <div className="text-center space-y-2">
      <h1 className="text-5xl font-bold text-slate-900 tracking-tight">
        Climbing Logbook
      </h1>
      <p className="text-slate-600">Track your climbing journey</p>
    </div>
  );
}

function SignedOutHome() {
  const { loginWithRedirect } = useAuth0();

  return (
    <Shell>
      <Hero />
      <div className="rounded-lg border bg-white p-8 text-center shadow-sm">
        <h2 className="text-xl font-semibold text-slate-900">
          Log your climbs, follow your crew
        </h2>
        <p className="mx-auto max-w-prose pt-2 text-slate-600">
          Sign in to track sends and grades over time, and to see what the
          climbers you follow have been getting up to.
        </p>
        <Button className="mt-6" onClick={() => void loginWithRedirect()}>
          Sign in to get started
        </Button>
      </div>
    </Shell>
  );
}

function StatsRow() {
  const { user } = useCurrentUser();
  const { stats, loading, error } = useHomeStats();
  const { selectedIds, toggle, reset } = useKpiPreferences(user?.id);

  const selected = KPI_CATALOG.filter((kpi) => selectedIds.includes(kpi.id));

  return (
    <section className="space-y-3">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold text-slate-900">Your stats</h2>
        <KpiCustomizer
          selectedIds={selectedIds}
          onToggle={toggle}
          onReset={reset}
        />
      </div>

      {error && <p className="text-red-600">Error: {error}</p>}

      {loading ? (
        <p className="text-slate-600">Loading your stats...</p>
      ) : !stats ? null : selected.length === 0 ? (
        <p className="text-sm text-slate-500">
          No stat cards selected. Use Customize to pick some.
        </p>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {selected.map((kpi) => (
            <StatCard key={kpi.id} title={kpi.title}>
              {kpi.render(stats)}
            </StatCard>
          ))}
        </div>
      )}
    </section>
  );
}

function Feed() {
  const { entries, loading, loadingMore, error, hasMore, loadMore } = useFeed();
  const [actionError, setActionError] = useState<string | null>(null);

  return (
    <section className="space-y-3">
      {actionError && (
        <ErrorToast
          title="Something went wrong"
          message={actionError}
          onDismiss={() => setActionError(null)}
        />
      )}

      <h2 className="text-xl font-semibold text-slate-900">Recent activity</h2>

      {error && <p className="text-red-600">Error: {error}</p>}

      {loading ? (
        <p className="text-slate-600">Loading your feed...</p>
      ) : entries.length === 0 ? (
        <div className="rounded-lg border bg-white p-8 text-center shadow-sm">
          <p className="text-slate-700">
            Your feed is empty. Log a climb, or follow some climbers to see what
            they are sending.
          </p>
          <div className="flex justify-center gap-3 pt-4">
            <Link to="/log/new">
              <Button>Log a climb</Button>
            </Link>
            <Link to="/users">
              <Button variant="outline">Find climbers</Button>
            </Link>
          </div>
        </div>
      ) : (
        <div className="space-y-4">
          {entries.map((entry) => (
            <FeedCard key={entry.id} entry={entry} onError={setActionError} />
          ))}
        </div>
      )}

      {hasMore && (
        <div className="flex justify-center">
          <Button
            variant="outline"
            onClick={() => void loadMore()}
            disabled={loadingMore}
          >
            {loadingMore ? "Loading..." : "Load more"}
          </Button>
        </div>
      )}
    </section>
  );
}

export default function Home() {
  const { isAuthenticated, isLoading } = useAuth0();

  if (isLoading) {
    return (
      <Shell>
        <Hero />
        <p className="text-center text-slate-600">Loading...</p>
      </Shell>
    );
  }

  if (!isAuthenticated) {
    return <SignedOutHome />;
  }

  return (
    <Shell>
      <Hero />
      <StatsRow />
      <Feed />
    </Shell>
  );
}
