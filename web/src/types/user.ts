import type {
  ClimbDiscipline,
  GradeSystem,
  LogEntryStatus,
  Place,
} from "@/types/catalog";

export interface UserSummary {
  id: number;
  username: string;
  displayName: string;
  pictureUrl: string | null;
}

export interface CurrentUser {
  id: number;
  username: string;
  displayName: string;
  email: string | null;
  bio: string | null;
  pictureUrl: string | null;
  homePlaceId: number | null;
  homePlace: Place | null;
  createdAt: string;
}

export interface HardestGrade {
  system: GradeSystem;
  grade: string;
  climbId: number;
  climbName: string;
}

export interface PlaceVisit {
  place: Place;
  count: number;
}

export interface UserStats {
  totalLogEntries: number;
  distinctClimbs: number;
  byStatus: Partial<Record<LogEntryStatus, number>>;
  byDiscipline: Partial<Record<ClimbDiscipline, number>>;
  hardestGrades: HardestGrade[];
  topPlaces: PlaceVisit[];
}

export interface UserProfile {
  id: number;
  username: string;
  displayName: string;
  email: string | null;
  bio: string | null;
  pictureUrl: string | null;
  homePlaceId: number | null;
  homePlace: Place | null;
  createdAt: string;
  stats: UserStats;
}

export interface UpdateUserProfileInput {
  displayName: string;
  username: string;
  bio?: string | null;
  pictureUrl?: string | null;
  homePlaceId?: number | null;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  skip: number;
  take: number;
}
