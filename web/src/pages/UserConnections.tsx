import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import ErrorToast from "@/components/log/ErrorToast";
import UserResultCard from "@/components/user/UserResultCard";
import { useUserConnections } from "@/hooks/user-hooks";
import type { ConnectionKind } from "@/types/user";

interface UserConnectionsProps {
  kind: ConnectionKind;
}

export default function UserConnections({ kind }: UserConnectionsProps) {
  const { username } = useParams();
  const [followError, setFollowError] = useState<string | null>(null);
  const { users, total, loading, loadingMore, error, hasMore, loadMore } =
    useUserConnections(username, kind);

  const heading = kind === "followers" ? "Followers" : "Following";

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="space-y-2">
          <h1 className="text-4xl font-bold tracking-tight text-slate-900">
            {heading}
          </h1>
          <p className="text-slate-600">
            <Link to={`/users/${username}`} className="hover:underline">
              @{username}
            </Link>
            {!loading && ` - ${total} climber${total === 1 ? "" : "s"}`}
          </p>
        </div>

        {error && <ErrorToast title={`Could not load ${heading.toLowerCase()}`} message={error} />}

        {followError && (
          <ErrorToast
            title="Follow failed"
            message={followError}
            onDismiss={() => setFollowError(null)}
          />
        )}

        {loading ? (
          <p className="rounded-lg border bg-white p-8 text-center text-slate-500 shadow-sm">
            Loading...
          </p>
        ) : users.length === 0 ? (
          <p className="rounded-lg border bg-white p-8 text-center text-slate-500 shadow-sm">
            {kind === "followers"
              ? "No one is following this climber yet."
              : "This climber is not following anyone yet."}
          </p>
        ) : (
          <div className="space-y-3">
            {users.map((user) => (
              <UserResultCard
                key={user.id}
                user={user}
                onError={setFollowError}
              />
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
      </div>
    </div>
  );
}
