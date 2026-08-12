import { useState } from "react";
import { Search } from "lucide-react";
import { Input } from "@/components/ui/input";
import ErrorToast from "@/components/log/ErrorToast";
import UserResultCard from "@/components/user/UserResultCard";
import { useUserSearch } from "@/hooks/user-hooks";

export default function Users() {
  const [query, setQuery] = useState("");
  const [followError, setFollowError] = useState<string | null>(null);
  const { results, loading, error } = useUserSearch(query);

  const trimmed = query.trim();

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="space-y-2">
          <h1 className="text-4xl font-bold tracking-tight text-slate-900">
            Find climbers
          </h1>
          <p className="text-slate-600">
            Search by name or username, then follow the people you climb with.
          </p>
        </div>

        <div className="relative">
          <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <Input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search climbers..."
            aria-label="Search climbers"
            className="pl-9"
          />
        </div>

        {error && <ErrorToast title="Search failed" message={error} />}

        {followError && (
          <ErrorToast
            title="Follow failed"
            message={followError}
            onDismiss={() => setFollowError(null)}
          />
        )}

        {!trimmed ? (
          <p className="rounded-lg border bg-white p-8 text-center text-slate-500 shadow-sm">
            Start typing to find climbers.
          </p>
        ) : loading ? (
          <p className="rounded-lg border bg-white p-8 text-center text-slate-500 shadow-sm">
            Loading...
          </p>
        ) : results.length === 0 ? (
          <p className="rounded-lg border bg-white p-8 text-center text-slate-500 shadow-sm">
            No climbers match "{trimmed}".
          </p>
        ) : (
          <div className="space-y-3">
            {results.map((user) => (
              <UserResultCard
                key={user.id}
                user={user}
                onError={setFollowError}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
