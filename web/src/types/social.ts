import type { ClimbDiscipline } from "@/types/catalog";
import type { HardestGrade, PlaceVisit, UserSummary } from "@/types/user";

export interface Comment {
  id: number;
  content: string;
  user: UserSummary | null;
  createdAt: string;
  climbId: number | null;
  logEntryId: number | null;
  canDelete: boolean;
}

export interface CreateCommentInput {
  content: string;
}

export type CommentTargetKind = "logEntry" | "climb";

export interface CommentTarget {
  kind: CommentTargetKind;
  id: number;
}

export interface CoreStats {
  totalSends: number;
  totalAttempts: number;
  distinctClimbs: number;
  sendRate: number;
  hardestGrades: HardestGrade[];
}

export interface ActivityStats {
  sendsThisMonth: number;
  daysClimbedLast30: number;
  currentStreakWeeks: number;
  lastClimbedAt: string | null;
}

export interface PlacesStats {
  topPlaces: PlaceVisit[];
  byDiscipline: Partial<Record<ClimbDiscipline, number>>;
}

export interface SocialStats {
  followerCount: number;
  followingCount: number;
  followingActiveThisWeek: number;
}

export interface HomeStats {
  core: CoreStats;
  activity: ActivityStats;
  places: PlacesStats;
  social: SocialStats;
}
