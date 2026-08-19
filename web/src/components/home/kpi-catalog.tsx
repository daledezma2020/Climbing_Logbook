import { Badge } from "@/components/ui/badge";
import {
  StatEmpty as Empty,
  StatHint as Hint,
  StatValue as Big,
} from "@/components/stats/StatText";
import type { HomeStats } from "@/types/social";

export type KpiGroup = "core" | "activity" | "places" | "social";

export interface KpiDefinition {
  id: string;
  group: KpiGroup;
  title: string;
  render: (stats: HomeStats) => React.ReactNode;
}

export const KPI_GROUP_LABELS: Record<KpiGroup, string> = {
  core: "Core send stats",
  activity: "Recent activity",
  places: "Places and disciplines",
  social: "Social",
};

function pairs(counts: Partial<Record<string, number>>) {
  return Object.entries(counts).filter(
    (entry): entry is [string, number] => entry[1] !== undefined,
  );
}

export const KPI_CATALOG: KpiDefinition[] = [
  {
    id: "total-sends",
    group: "core",
    title: "Total sends",
    render: (stats) => (
      <>
        <Big>{stats.core.totalSends}</Big>
        <Hint>
          {stats.core.distinctClimbs} distinct climb
          {stats.core.distinctClimbs === 1 ? "" : "s"}
        </Hint>
      </>
    ),
  },
  {
    id: "hardest-sent",
    group: "core",
    title: "Hardest sent",
    render: (stats) =>
      stats.core.hardestGrades.length === 0 ? (
        <Empty>No graded sends yet</Empty>
      ) : (
        <ul className="space-y-2 text-sm text-slate-700">
          {stats.core.hardestGrades.map((hardest) => (
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
      ),
  },
  {
    id: "send-rate",
    group: "core",
    title: "Send rate",
    render: (stats) => (
      <>
        <Big>{Math.round(stats.core.sendRate * 100)}%</Big>
        <Hint>
          {stats.core.totalAttempts} attempt
          {stats.core.totalAttempts === 1 ? "" : "s"} logged
        </Hint>
      </>
    ),
  },
  {
    id: "sends-this-month",
    group: "activity",
    title: "Sends this month",
    render: (stats) => <Big>{stats.activity.sendsThisMonth}</Big>,
  },
  {
    id: "days-climbed",
    group: "activity",
    title: "Days climbed (last 30)",
    render: (stats) => (
      <>
        <Big>{stats.activity.daysClimbedLast30}</Big>
        <Hint>
          {stats.activity.lastClimbedAt
            ? `Last climbed ${new Date(stats.activity.lastClimbedAt).toLocaleDateString()}`
            : "No sessions logged yet"}
        </Hint>
      </>
    ),
  },
  {
    id: "streak",
    group: "activity",
    title: "Current streak",
    render: (stats) => (
      <>
        <Big>{stats.activity.currentStreakWeeks}</Big>
        <Hint>
          consecutive week
          {stats.activity.currentStreakWeeks === 1 ? "" : "s"} with a climb
        </Hint>
      </>
    ),
  },
  {
    id: "top-places",
    group: "places",
    title: "Most visited",
    render: (stats) =>
      stats.places.topPlaces.length === 0 ? (
        <Empty>No places logged yet</Empty>
      ) : (
        <ul className="space-y-1 text-sm text-slate-700">
          {stats.places.topPlaces.map((visit) => (
            <li
              key={visit.place.id}
              className="flex items-center justify-between gap-4"
            >
              <span className="truncate">{visit.place.name}</span>
              <span className="font-medium">{visit.count}</span>
            </li>
          ))}
        </ul>
      ),
  },
  {
    id: "by-discipline",
    group: "places",
    title: "By discipline",
    render: (stats) => {
      const rows = pairs(stats.places.byDiscipline);
      return rows.length === 0 ? (
        <Empty>No entries yet</Empty>
      ) : (
        <ul className="space-y-1 text-sm text-slate-700">
          {rows.map(([discipline, count]) => (
            <li key={discipline} className="flex justify-between gap-4">
              <span>{discipline}</span>
              <span className="font-medium">{count}</span>
            </li>
          ))}
        </ul>
      );
    },
  },
  {
    id: "connections",
    group: "social",
    title: "Connections",
    render: (stats) => (
      <>
        <Big>{stats.social.followerCount}</Big>
        <Hint>
          follower{stats.social.followerCount === 1 ? "" : "s"} -{" "}
          {stats.social.followingCount} following
        </Hint>
      </>
    ),
  },
  {
    id: "active-this-week",
    group: "social",
    title: "Active this week",
    render: (stats) => (
      <>
        <Big>{stats.social.followingActiveThisWeek}</Big>
        <Hint>climbers you follow logged something</Hint>
      </>
    ),
  },
];

export const DEFAULT_KPI_IDS = KPI_CATALOG.filter(
  (kpi) => kpi.group === "core",
).map((kpi) => kpi.id);
