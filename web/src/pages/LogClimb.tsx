import { useEffect, useRef, useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { useNavigate, useLocation } from "react-router-dom";
import { Search, Loader2, Plus, ArrowLeft, Check, LogIn } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent } from "@/components/ui/card";
import { apiFetch } from "@/lib/api";
import {
  importOpenBetaClimb,
  useSearch,
} from "@/hooks/catalog-hooks";
import { SourceBadges, TypeBadge } from "@/components/log/ResultBadges";
import LogEntryForm from "@/components/log/LogEntryForm";
import ManualClimbForm from "@/components/log/ManualClimbForm";
import ErrorToast from "@/components/log/ErrorToast";
import type { Climb, SearchResult } from "@/types/catalog";

type Step = "search" | "manual" | "log";

const PROVIDER_LABELS: Record<string, string> = {
  local: "Local",
  openbeta: "OpenBeta",
};

export default function LogClimb() {
  const navigate = useNavigate();
  const location = useLocation();
  const { results, loading, error, search } = useSearch();
  const {
    getAccessTokenSilently,
    loginWithRedirect,
    isAuthenticated,
    isLoading,
  } = useAuth0();

  const [step, setStep] = useState<Step>("search");
  const [query, setQuery] = useState("");
  const [selectedClimb, setSelectedClimb] = useState<Climb | null>(null);
  const [manualSeed, setManualSeed] = useState<{
    name?: string;
    placeId?: number | null;
  }>({});
  const [resolving, setResolving] = useState<string | null>(null);
  const [resolveError, setResolveError] = useState<string | null>(null);
  const seededClimb = useRef(false);

  useEffect(() => {
    const climbId = location.state?.climbId as number | undefined;
    if (climbId && !seededClimb.current) {
      seededClimb.current = true;
      navigate(location.pathname, { replace: true, state: {} });
      apiFetch<Climb>(`/climbs/${climbId}`)
        .then((climb) => {
          setSelectedClimb(climb);
          setStep("log");
        })
        .catch(() => setResolveError("Could not load that climb"));
    }
  }, [location.state, location.pathname, navigate]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (query.trim()) search(query.trim());
  };

  const goToLog = (climb: Climb) => {
    setSelectedClimb(climb);
    setStep("log");
  };

  const resolveClimb = async (result: SearchResult) => {
    setResolveError(null);
    setResolving(result.key);
    try {
      if (result.localId) {
        goToLog(await apiFetch<Climb>(`/climbs/${result.localId}`));
      } else if (result.externalId) {
        const accessToken = await getAccessTokenSilently();
        goToLog(await importOpenBetaClimb(result.externalId, accessToken));
      }
    } catch (err) {
      setResolveError(
        err instanceof Error
          ? `The API could not load or import that climb. ${err.message}`
          : "The API could not load or import that climb. Try searching again or add it manually.",
      );
    } finally {
      setResolving(null);
    }
  };

  const resolvePlace = async (result: SearchResult) => {
    setResolveError(null);
    setResolving(result.key);
    try {
      setManualSeed({ placeId: result.localId ?? null });
      setStep("manual");
    } catch (err) {
      setResolveError(
        err instanceof Error
          ? `The API could not prepare that place. ${err.message}`
          : "The API could not prepare that place. Try a custom location instead.",
      );
    } finally {
      setResolving(null);
    }
  };

  if (isLoading) {
    return (
      <Shell title="Log a climb" subtitle="Checking sign-in status">
        <div className="rounded-lg border bg-white p-6 shadow-sm text-slate-600">
          Loading...
        </div>
      </Shell>
    );
  }

  if (!isAuthenticated) {
    return (
      <Shell
        title="Log a climb"
        subtitle="Sign in before creating climbs or logbook entries"
      >
        <div className="rounded-lg border bg-white p-6 shadow-sm">
          <Button className="gap-2" onClick={() => loginWithRedirect()}>
            <LogIn className="h-4 w-4" />
            Sign in to log a climb
          </Button>
        </div>
      </Shell>
    );
  }

  if (step === "log" && selectedClimb) {
    return (
      <Shell title="Log a climb" subtitle="Step 2 of 2: record your attempt">
        <TwoStepProgress currentStep={2} />
        <Card>
          <CardContent className="pt-6">
            <LogEntryForm
              climb={selectedClimb}
              onSubmitted={() =>
                navigate("/logbook", { state: { refetch: true } })
              }
              onCancel={() => setStep("search")}
            />
          </CardContent>
        </Card>
      </Shell>
    );
  }

  if (step === "manual") {
    return (
      <Shell
        title="Add a climb"
        subtitle="Step 1 of 2: create the climb, then you will log it"
      >
        <TwoStepProgress currentStep={1} />
        <Card>
          <CardContent className="pt-6">
            <ManualClimbForm
              initialName={manualSeed.name}
              initialPlaceId={manualSeed.placeId}
              onCreated={(climb) => goToLog(climb)}
              onCancel={() => {
                setManualSeed({});
                setStep("search");
              }}
            />
          </CardContent>
        </Card>
      </Shell>
    );
  }

  return (
    <Shell
      title="Log a climb"
      subtitle="Find an existing climb, or create a new climb first and log it next"
    >
      <form onSubmit={handleSearch} className="flex gap-2">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
          <Input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search for a climb or place..."
            className="pl-9"
          />
        </div>
        <Button type="submit" disabled={loading || !query.trim()}>
          {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : "Search"}
        </Button>
      </form>

      <div className="flex flex-wrap items-center gap-2">
        <Button
          variant="outline"
          onClick={() => {
            setManualSeed({ name: query.trim() });
            setStep("manual");
          }}
        >
          <Plus className="mr-2 h-4 w-4" />
          Add new climb
        </Button>
        <span className="text-sm text-slate-500">
          New climbs use a two-step flow: create the climb, then save the log
          entry.
        </span>
      </div>

      {results && (
        <div className="flex flex-wrap gap-2 text-xs text-slate-500">
          {Object.entries(results.providers).map(([provider, status]) => (
            <span key={provider} className="flex items-center gap-1">
              <span
                className={`h-2 w-2 rounded-full ${
                  status === "complete"
                    ? "bg-green-500"
                    : status === "failed"
                      ? "bg-red-500"
                      : "bg-slate-300"
                }`}
              />
              {PROVIDER_LABELS[provider] ?? provider}: {status}
            </span>
          ))}
        </div>
      )}

      {error && (
        <ErrorToast
          title="Search failed"
          message={`The API could not complete the climb search. ${error}`}
        />
      )}
      {resolveError && (
        <ErrorToast
          title="Could not prepare climb"
          message={resolveError}
          onDismiss={() => setResolveError(null)}
        />
      )}

      <div className="space-y-3">
        {results?.results.length === 0 && (
          <p className="text-sm text-slate-500">
            No results. Try a different search or add the climb manually.
          </p>
        )}
        {results?.results.map((result) => (
          <Card key={result.key}>
            <CardContent className="flex items-center justify-between gap-4 py-4">
              <div className="space-y-1">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="font-medium text-slate-900">
                    {result.name}
                  </span>
                  <TypeBadge
                    resultType={result.resultType}
                    placeKind={result.placeKind}
                  />
                  <SourceBadges sources={result.sources} />
                </div>
                <p className="text-sm text-slate-500">
                  {result.grade ? `${result.grade.value}` : null}
                  {result.grade && result.placeName ? " · " : null}
                  {result.placeName}
                </p>
              </div>
              <Button
                size="sm"
                disabled={resolving !== null}
                onClick={() =>
                  result.resultType === "climb"
                    ? resolveClimb(result)
                    : resolvePlace(result)
                }
              >
                {resolving === result.key ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : result.resultType === "climb" ? (
                  "Log this"
                ) : (
                  "Add climb here"
                )}
              </Button>
            </CardContent>
          </Card>
        ))}
      </div>
    </Shell>
  );
}

function TwoStepProgress({ currentStep }: { currentStep: 1 | 2 }) {
  const steps = [
    { number: 1, label: "Add climb" },
    { number: 2, label: "Log entry" },
  ] as const;

  return (
    <div className="rounded-lg border bg-white p-4 shadow-sm">
      <div className="flex items-center gap-3">
        {steps.map((step, index) => {
          const isComplete = currentStep > step.number;
          const isActive = currentStep === step.number;

          return (
            <div key={step.number} className="flex flex-1 items-center gap-3">
              <div
                className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full border text-sm font-medium ${
                  isComplete
                    ? "border-slate-900 bg-slate-900 text-white"
                    : isActive
                      ? "border-slate-900 bg-white text-slate-900"
                      : "border-slate-300 bg-white text-slate-400"
                }`}
              >
                {isComplete ? <Check className="h-4 w-4" /> : step.number}
              </div>
              <p
                className={`text-sm font-medium ${
                  isActive || isComplete ? "text-slate-900" : "text-slate-500"
                }`}
              >
                {step.label}
              </p>
              {index === 0 && <div className="h-px flex-1 bg-slate-200" />}
            </div>
          );
        })}
      </div>
    </div>
  );
}

function Shell({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle: string;
  children: React.ReactNode;
}) {
  const navigate = useNavigate();
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex items-center justify-between gap-4">
          <div className="space-y-1">
            <h1 className="text-3xl font-bold text-slate-900 tracking-tight">
              {title}
            </h1>
            <p className="text-slate-600">{subtitle}</p>
          </div>
          <Button variant="ghost" onClick={() => navigate("/logbook")}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Logbook
          </Button>
        </div>
        {children}
      </div>
    </div>
  );
}
