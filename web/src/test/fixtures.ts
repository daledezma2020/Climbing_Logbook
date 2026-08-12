import type { Climb, LogEntry, Place } from "@/types/catalog";
import type { Comment, HomeStats } from "@/types/social";
import type { CurrentUser, UserSummary } from "@/types/user";

export const testUser: UserSummary = {
  id: 5,
  username: "alex",
  displayName: "Alex Climber",
  pictureUrl: null,
  homePlace: null,
  followerCount: 0,
  isFollowedByMe: false,
  isMe: false,
};

export function userSummary(overrides: Partial<UserSummary> = {}): UserSummary {
  return { ...testUser, ...overrides };
}

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
    likeCount: 0,
    commentCount: 0,
    isLikedByMe: false,
    ...overrides,
  };
}

export function comment(overrides: Partial<Comment> = {}): Comment {
  return {
    id: 1,
    content: "Nice send!",
    user: testUser,
    createdAt: "2026-07-02T12:00:00.000Z",
    climbId: null,
    logEntryId: 1,
    canDelete: false,
    ...overrides,
  };
}

export function homeStats(overrides: Partial<HomeStats> = {}): HomeStats {
  return {
    core: {
      totalSends: 12,
      totalAttempts: 15,
      distinctClimbs: 9,
      sendRate: 0.8,
      hardestGrades: [
        { system: "VScale", grade: "V6", climbId: 1, climbName: "Blue Arete" },
      ],
    },
    activity: {
      sendsThisMonth: 4,
      daysClimbedLast30: 6,
      currentStreakWeeks: 2,
      lastClimbedAt: "2026-07-01T12:00:00.000Z",
    },
    places: {
      topPlaces: [{ place: testPlace, count: 5 }],
      byDiscipline: { Bouldering: 12 },
    },
    social: {
      followerCount: 7,
      followingCount: 4,
      followingActiveThisWeek: 2,
    },
    ...overrides,
  };
}
