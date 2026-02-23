export interface ClimbRoute {
  id: number;
  name: string;
  grade: string;
  averageRating: number;
  setterId: number | null;
  type: string | null;
  picture: string | null;
  video: string | null;
  locationId: number;
  createdAt: string;
  updatedAt: string | null;
}

// For creating a new route (omit server-generated fields)
export type CreateClimbRouteInput = Omit<
  ClimbRoute,
  "id" | "createdAt" | "updatedAt"
>;

// For updating a route (all fields optional)
export type UpdateClimbRouteInput = Partial<CreateClimbRouteInput>;
