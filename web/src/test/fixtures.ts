import type { Climb, LogEntry, Place } from "@/types/catalog";

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
