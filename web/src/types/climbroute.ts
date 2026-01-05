export interface ClimbRoute {
  id: number;
  name: string;
  grade: string;
  averageRating: number;
  setter: string | null;
  type: string | null;
  picture: string | null;
  video: string | null;
  location: string;
  createdAt: string;
  updatedAt: string | null;
}
