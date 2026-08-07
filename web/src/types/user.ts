import type { Place } from "@/types/catalog";

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
