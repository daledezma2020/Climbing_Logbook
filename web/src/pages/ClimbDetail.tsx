import { Link, useParams } from "react-router-dom";
import { BookPlus, Hammer, MapPin, Star } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import LogEntryTable from "@/components/log/LogEntryTable";
import CommentThread from "@/components/feed/CommentThread";
import { SourceBadges } from "@/components/log/ResultBadges";
import { useClimb, useClimbLogEntries } from "@/hooks/catalog-hooks";
import type { Climb } from "@/types/catalog";

function Shell({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-5xl mx-auto space-y-6">{children}</div>
    </div>
  );
}

function whereLabel(climb: Climb): string | null {
  return (
    climb.place?.name ??
    climb.boardConfiguration?.name ??
    climb.customLocation?.name ??
    null
  );
}

export default function ClimbDetail() {
  const { id } = useParams();
  const climbId = id === undefined ? undefined : Number(id);
  const { climb, loading, error, notFound } = useClimb(climbId);
  const {
    entries,
    total,
    loading: entriesLoading,
    loadingMore,
    error: entriesError,
    hasMore,
    loadMore,
  } = useClimbLogEntries(climbId);

  if (notFound) {
    return (
      <Shell>
        <div className="rounded-lg border bg-white p-8 text-center shadow-sm">
          <h1 className="text-2xl font-bold text-slate-900">No climb found</h1>
          <p className="mt-2 text-slate-600">
            There is no climb with the id "{id}".
          </p>
          <Link to="/climbs" className="mt-4 inline-block">
            <Button variant="outline">Browse climbs</Button>
          </Link>
        </div>
      </Shell>
    );
  }

  if (loading) {
    return (
      <Shell>
        <div className="rounded-lg border bg-white p-6 text-slate-600 shadow-sm">
          Loading climb...
        </div>
      </Shell>
    );
  }

  if (error || !climb) {
    return (
      <Shell>
        <p className="text-red-600">Error: {error ?? "Climb unavailable"}</p>
      </Shell>
    );
  }

  const where = whereLabel(climb);

  return (
    <Shell>
      <div className="rounded-lg border bg-white p-6 shadow-sm">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div className="min-w-0 space-y-2">
            <h1 className="text-3xl font-bold tracking-tight text-slate-900">
              {climb.name}
            </h1>

            <div className="flex flex-wrap items-center gap-2 text-sm text-slate-600">
              <Badge variant="secondary">{climb.grade}</Badge>
              <Badge variant="outline">{climb.discipline}</Badge>
              {where && (
                <span className="flex items-center gap-1">
                  <MapPin className="h-4 w-4" />
                  {where}
                </span>
              )}
              {climb.averageRating > 0 && (
                <span className="flex items-center gap-1">
                  <Star className="h-4 w-4 fill-yellow-400 text-yellow-400" />
                  {climb.averageRating.toFixed(1)}
                </span>
              )}
              <SourceBadges sources={climb.sources} />
            </div>

            <div className="flex flex-wrap items-center gap-4 pt-1 text-sm text-slate-600">
              {climb.setterName && (
                <span className="flex items-center gap-1">
                  <Hammer className="h-4 w-4" />
                  Set by {climb.setterName}
                </span>
              )}
              {climb.firstAscentName && <span>FA: {climb.firstAscentName}</span>}
              {climb.pictureUrl && (
                <a
                  href={climb.pictureUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="hover:underline"
                >
                  Photo
                </a>
              )}
              {climb.videoUrl && (
                <a
                  href={climb.videoUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="hover:underline"
                >
                  Video
                </a>
              )}
            </div>
          </div>

          <Link to="/log/new" state={{ climbId: climb.id }}>
            <Button className="gap-2">
              <BookPlus className="h-4 w-4" />
              Log this climb
            </Button>
          </Link>
        </div>
      </div>

      <div className="space-y-3">
        <h2 className="text-xl font-semibold text-slate-900">
          Ascents{total > 0 ? ` (${total})` : ""}
        </h2>
        {entriesError && <p className="text-red-600">Error: {entriesError}</p>}
        <LogEntryTable
          entries={entries}
          loading={entriesLoading}
          emptyMessage="Nobody has logged this climb yet."
        />
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

      <div className="space-y-3">
        <h2 className="text-xl font-semibold text-slate-900">Comments</h2>
        <div className="rounded-lg border bg-white p-6 shadow-sm">
          <CommentThread target={{ kind: "climb", id: climb.id }} />
        </div>
      </div>
    </Shell>
  );
}
