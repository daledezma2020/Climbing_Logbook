import { useState, useEffect } from "react";
import { fetchJson } from "@/lib/api";
import type { ClimbRoute } from "@/types/climbroute";

type ApiClimbRoute = Partial<ClimbRoute> & {
  Id?: number;
  Name?: string;
  Grade?: string;
  AverageRating?: number;
  Setter?: string | null;
  Type?: string | null;
  Picture?: string | null;
  Video?: string | null;
  Location?: string;
  CreatedAt?: string;
  UpdatedAt?: string | null;
};

interface UseClimbRoutesReturn {
  routes: ClimbRoute[];
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

export function useClimbRoutes(): UseClimbRoutesReturn {
  const [routes, setRoutes] = useState<ClimbRoute[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchRoutes = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await fetchJson<ApiClimbRoute[]>("/api/routes");

      // Transform PascalCase to camelCase
      const transformedRoutes: ClimbRoute[] = data.map((route) => ({
        id: route.id ?? route.Id ?? 0,
        name: route.name ?? route.Name ?? "",
        grade: route.grade ?? route.Grade ?? "",
        averageRating: route.averageRating ?? route.AverageRating ?? 0,
        setter: route.setter ?? route.Setter ?? null,
        type: route.type ?? route.Type ?? null,
        picture: route.picture ?? route.Picture ?? null,
        video: route.video ?? route.Video ?? null,
        location: route.location ?? route.Location ?? "",
        createdAt: route.createdAt ?? route.CreatedAt ?? "",
        updatedAt: route.updatedAt ?? route.UpdatedAt ?? null,
      }));

      setRoutes(transformedRoutes);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to fetch routes");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRoutes();
  }, []);

  return { routes, loading, error, refetch: fetchRoutes };
}
