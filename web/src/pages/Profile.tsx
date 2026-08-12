import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth0 } from "@auth0/auth0-react";
import { CalendarDays, MapPin, Pencil } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import UserAvatar from "@/components/user/UserAvatar";
import FollowButton from "@/components/user/FollowButton";
import ErrorToast from "@/components/log/ErrorToast";
import LogEntryTable from "@/components/log/LogEntryTable";
import { useCurrentUser, useUserLogEntries, useUserProfile } from "@/hooks/user-hooks";
import type { UserStats } from "@/types/user";

function Shell({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-5xl mx-auto space-y-6">{children}</div>
    </div>
  );
}

function StatCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium text-slate-600">
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent>{children}</CardContent>
    </Card>
  );
}

function countEntries(counts: Partial<Record<string, number>>) {
  return Object.entries(counts).filter(
    (entry): entry is [string, number] => entry[1] !== undefined,
  );
}

function StatsRow({ stats }: { stats: UserStats }) {
  const byStatus = countEntries(stats.byStatus);
  const byDiscipline = countEntries(stats.byDiscipline);

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      <StatCard title="Logged climbs">
        <p className="text-3xl font-bold text-slate-900">
          {stats.totalLogEntries}
        </p>
        <p className="text-sm text-slate-600">
          {stats.distinctClimbs} distinct climb
          {stats.distinctClimbs === 1 ? "" : "s"}
        </p>
      </StatCard>

      <StatCard title="By status">
        {byStatus.length === 0 ? (
          <p className="text-sm text-slate-500">No entries yet</p>
        ) : (
          <ul className="space-y-1 text-sm text-slate-700">
            {byStatus.map(([status, count]) => (
              <li key={status} className="flex justify-between gap-4">
                <span>{status}</span>
                <span className="font-medium">{count}</span>
              </li>
            ))}
          </ul>
        )}
      </StatCard>

      <StatCard title="By discipline">
        {byDiscipline.length === 0 ? (
          <p className="text-sm text-slate-500">No entries yet</p>
        ) : (
          <ul className="space-y-1 text-sm text-slate-700">
            {byDiscipline.map(([discipline, count]) => (
              <li key={discipline} className="flex justify-between gap-4">
                <span>{discipline}</span>
                <span className="font-medium">{count}</span>
              </li>
            ))}
          </ul>
        )}
      </StatCard>

      <StatCard title="Hardest sent">
        {stats.hardestGrades.length === 0 ? (
          <p className="text-sm text-slate-500">No graded sends yet</p>
        ) : (
          <ul className="space-y-2 text-sm text-slate-700">
            {stats.hardestGrades.map((hardest) => (
              <li
                key={hardest.system}
                className="flex items-center justify-between gap-4"
              >
                <span className="text-slate-600">{hardest.system}</span>
                <span className="flex min-w-0 items-center gap-2">
                  <Badge variant="secondary">{hardest.grade}</Badge>
                  <span className="truncate">{hardest.climbName}</span>
                </span>
              </li>
            ))}
          </ul>
        )}
      </StatCard>

      <StatCard title="Most visited">
        {stats.topPlaces.length === 0 ? (
          <p className="text-sm text-slate-500">No places logged yet</p>
        ) : (
          <ul className="space-y-1 text-sm text-slate-700">
            {stats.topPlaces.map((visit) => (
              <li
                key={visit.place.id}
                className="flex items-center justify-between gap-4"
              >
                <span className="truncate">{visit.place.name}</span>
                <span className="font-medium">{visit.count}</span>
              </li>
            ))}
          </ul>
        )}
      </StatCard>
    </div>
  );
}

export default function Profile() {
  const { username } = useParams();
  const { user: auth0User } = useAuth0();
  const { profile, loading, error, notFound, refetch } = useUserProfile(username);
  const { user: currentUser } = useCurrentUser();
  const [followError, setFollowError] = useState<string | null>(null);
  const {
    entries,
    loading: entriesLoading,
    loadingMore,
    error: entriesError,
    hasMore,
    loadMore,
  } = useUserLogEntries(username);

  const isOwnProfile =
    !!profile && (profile.isMe || currentUser?.id === profile.id);

  if (notFound) {
    return (
      <Shell>
        <div className="rounded-lg border bg-white p-8 text-center shadow-sm">
          <h1 className="text-2xl font-bold text-slate-900">
            No climber found
          </h1>
          <p className="mt-2 text-slate-600">
            There is no user with the username "{username}".
          </p>
        </div>
      </Shell>
    );
  }

  if (loading) {
    return (
      <Shell>
        <div className="rounded-lg border bg-white p-6 text-slate-600 shadow-sm">
          Loading profile...
        </div>
      </Shell>
    );
  }

  if (error || !profile) {
    return (
      <Shell>
        <p className="text-red-600">Error: {error ?? "Profile unavailable"}</p>
      </Shell>
    );
  }

  return (
    <Shell>
      {followError && (
        <ErrorToast
          title="Follow failed"
          message={followError}
          onDismiss={() => setFollowError(null)}
        />
      )}

      <div className="rounded-lg border bg-white p-6 shadow-sm">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div className="flex min-w-0 items-start gap-4">
            <UserAvatar
              displayName={profile.displayName}
              pictureUrl={profile.pictureUrl}
              fallbackPictureUrl={isOwnProfile ? auth0User?.picture : null}
              className="h-20 w-20 text-xl"
            />
            <div className="min-w-0 space-y-1">
              <h1 className="text-3xl font-bold tracking-tight text-slate-900">
                {profile.displayName}
              </h1>
              <p className="text-slate-500">@{profile.username}</p>
              {profile.bio && (
                <p className="max-w-prose whitespace-pre-line pt-2 text-slate-700">
                  {profile.bio}
                </p>
              )}
              <div className="flex flex-wrap items-center gap-4 pt-2 text-sm text-slate-600">
                <Link
                  to={`/users/${profile.username}/followers`}
                  className="hover:underline"
                >
                  <span className="font-medium text-slate-900">
                    {profile.followerCount}
                  </span>{" "}
                  follower{profile.followerCount === 1 ? "" : "s"}
                </Link>
                <Link
                  to={`/users/${profile.username}/following`}
                  className="hover:underline"
                >
                  <span className="font-medium text-slate-900">
                    {profile.followingCount}
                  </span>{" "}
                  following
                </Link>
                {profile.homePlace && (
                  <span className="flex items-center gap-1">
                    <MapPin className="h-4 w-4" />
                    {profile.homePlace.name}
                  </span>
                )}
                <span className="flex items-center gap-1">
                  <CalendarDays className="h-4 w-4" />
                  Joined {new Date(profile.createdAt).toLocaleDateString()}
                </span>
              </div>
            </div>
          </div>

          {isOwnProfile ? (
            <Link to="/profile/edit">
              <Button variant="outline" className="gap-2">
                <Pencil className="h-4 w-4" />
                Edit profile
              </Button>
            </Link>
          ) : (
            <FollowButton
              username={profile.username}
              isFollowing={profile.isFollowedByMe}
              onError={setFollowError}
              onChange={() => void refetch()}
            />
          )}
        </div>
      </div>

      <StatsRow stats={profile.stats} />

      <div className="space-y-3">
        <h2 className="text-xl font-semibold text-slate-900">Logged climbs</h2>
        {entriesError && <p className="text-red-600">Error: {entriesError}</p>}
        <LogEntryTable
          entries={entries}
          loading={entriesLoading}
          emptyMessage={`${profile.displayName} has not logged any climbs yet.`}
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
    </Shell>
  );
}
