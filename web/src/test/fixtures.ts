import type { Climb, LogEntry, Place } from "@/types/catalog";
import type { CurrentUser, UserSummary } from "@/types/user";

export const testUser: UserSummary = {
  id: 5,
  username: "alex",
  displayName: "Alex Climber",
  pictureUrl: null,
};

export function currentUser(overrides: Partial<CurrentUser> = {}): CurrentUser {
  return {
    id: testUser.id,
    username: testUser.username,
    displayName: testUser.displayName,
    email: "alex@example.test",
    bio: null,
    pictureUrl: null,
    homePlaceId: null,
    homePlace: null,
    createdAt: "2026-07-01T12:00:00.000Z",
    ...overrides,
  };
}

export const testPlace: Place = {
  id: 10,
  name: "Test Gym",
  kind: "Gym",
  latitude: 40,
  longitude: -75,
  address: null,
  city: null,
  state: null,
  country: null,
};

export function climb(overrides: Partial<Climb> = {}): Climb {
  return {
    id: 1,
    name: "Blue Arete",
    discipline: "Bouldering",
    gradeSystem: "VScale",
    grade: "V4",
    placeId: testPlace.id,
    place: testPlace,
    boardConfigurationId: null,
    boardConfiguration: null,
    customLocation: null,
    setterId: 3,
    setterName: "Alex",
    firstAscentName: null,
    pictureUrl: null,
    videoUrl: null,
    averageRating: 4,
    sources: [],
    ...overrides,
  };
}

export function logEntry(overrides: Partial<LogEntry> = {}): LogEntry {
  return {
    id: 1,
    userId: testUser.id,
    user: testUser,
    climbId: 1,
    climb: climb(),
    placeId: testPlace.id,
    place: testPlace,
    occurredAt: "2026-07-01T12:00:00.000Z",
    status: "Completed",
    rating: 4,
    notes: null,
    createdAt: "2026-07-01T12:00:00.000Z",
    updatedAt: null,
    ...overrides,
  };
}
