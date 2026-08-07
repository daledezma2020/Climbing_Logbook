import type { UserSummary } from "@/types/user";

export type ClimbDiscipline = "Bouldering" | "Sport" | "Trad" | "Other";
export type GradeSystem = "VScale" | "Yds" | "French" | "Font" | "Other";
export type PlaceKind = "Gym" | "Outdoor" | "Custom";
export type LogEntryStatus = "Attempted" | "Completed";

export interface Place {
  id: number;
  name: string;
  kind: PlaceKind;
  latitude: number | null;
  longitude: number | null;
  address: string | null;
  city: string | null;
  state: string | null;
  country: string | null;
}

export interface BoardConfiguration {
  id: number;
  name: string;
  manufacturer: string;
  year: number;
}

export interface CustomLocation {
  name: string;
  latitude: number;
  longitude: number;
}

export interface Climb {
  id: number;
  name: string;
  discipline: ClimbDiscipline;
  gradeSystem: GradeSystem;
  grade: string;
  placeId: number | null;
  place: Place | null;
  boardConfigurationId: number | null;
  boardConfiguration: BoardConfiguration | null;
  customLocation: CustomLocation | null;
  setterId: number | null;
  setterName: string | null;
  firstAscentName: string | null;
  pictureUrl: string | null;
  videoUrl: string | null;
  averageRating: number;
  sources: string[];
}

export interface LogEntry {
  id: number;
  userId: number;
  user: UserSummary | null;
  climbId: number;
  climb: Climb | null;
  placeId: number | null;
  place: Place | null;
  occurredAt: string;
  status: LogEntryStatus;
  rating: number | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateLogEntryInput {
  climbId: number;
  placeId?: number | null;
  occurredAt?: string | null;
  status: LogEntryStatus;
  rating?: number | null;
  notes?: string | null;
}

export interface CreatePlaceInput {
  name: string;
  kind: PlaceKind;
  parentPlaceId?: number | null;
  latitude?: number | null;
  longitude?: number | null;
  address?: string | null;
  city?: string | null;
  state?: string | null;
  country?: string | null;
}

export interface CreateManualClimbInput {
  name: string;
  discipline: ClimbDiscipline;
  gradeSystem: GradeSystem;
  grade: string;
  placeId?: number | null;
  boardConfigurationId?: number | null;
  boardConfigurationName?: string | null;
  setterId?: number | null;
  setterName?: string | null;
  firstAscentName?: string | null;
  pictureUrl?: string | null;
  videoUrl?: string | null;
  customLocation?: CustomLocation | null;
}

export interface SearchGrade {
  system: string;
  value: string;
}

export interface SearchCoordinates {
  latitude: number;
  longitude: number;
}

export interface SearchResult {
  key: string;
  resultType: "climb" | "place";
  name: string;
  sources: string[];
  grade?: SearchGrade | null;
  discipline?: ClimbDiscipline | null;
  placeName?: string | null;
  placeKind?: PlaceKind | null;
  coordinates?: SearchCoordinates | null;
  localId?: number | null;
  externalId?: string | null;
}

export type ProviderStatus = "complete" | "failed" | "skipped";

export interface SearchResponse {
  results: SearchResult[];
  providers: Record<string, ProviderStatus>;
}
