export interface ClimbRoute {
  id: number;
  name: string;
  grade: string;
  averageRating: number;
  picture: string | null;
  video: string | null;
  location: string;
  createdAt: string;
  updatedAt: string | null;
}
