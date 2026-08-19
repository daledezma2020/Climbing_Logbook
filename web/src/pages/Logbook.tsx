import { useEffect, useRef } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import LogEntryTable from "@/components/log/LogEntryTable";
import { useLogEntries } from "@/hooks/catalog-hooks";

export default function Logbook() {
  const navigate = useNavigate();
  const location = useLocation();
  const { entries, loading, error, refetch } = useLogEntries();
  const hasRefetched = useRef(false);

  useEffect(() => {
    if (location.state?.refetch && !hasRefetched.current) {
      refetch();
      hasRefetched.current = true;
      navigate(location.pathname, { replace: true, state: {} });
    }
  }, [location.state, location.pathname, refetch, navigate]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-5xl mx-auto space-y-6">
        <div className="flex items-start justify-between gap-4">
          <div className="space-y-2">
            <h1 className="text-4xl font-bold text-slate-900 tracking-tight">
              Logbook
            </h1>
            <p className="text-slate-600">Your climbing activity</p>
          </div>
          <Button onClick={() => navigate("/log/new")} size="lg">
            <Plus className="mr-2 h-5 w-5" />
            Log a climb
          </Button>
        </div>

        {error && <p className="text-red-600">Error: {error}</p>}

        <LogEntryTable
          entries={entries}
          loading={loading}
          emptyMessage="No entries yet. Log your first climb!"
        />
      </div>
    </div>
  );
}
