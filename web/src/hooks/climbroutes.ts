import { useState, useEffect } from 'react';
import type { ClimbRoute } from '@/types/climbroute';
 
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
      const response = await fetch('http://localhost:5050/api/routes');

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();

      // Transform PascalCase to camelCase
      const transformedRoutes = data.map((route: any) => ({
        id: route.id || route.Id,
        name: route.name || route.Name,
        grade: route.grade || route.Grade,
        averageRating: route.averageRating || route.AverageRating,
        setter: route.setter || route.Setter,
        type: route.type || route.Type,
        picture: route.picture || route.Picture,
        video: route.video || route.Video,
        location: route.location || route.Location,
        createdAt: route.createdAt || route.CreatedAt,
        updatedAt: route.updatedAt || route.UpdatedAt
      }));

      setRoutes(transformedRoutes);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch routes');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchRoutes();
  }, []);

  return { routes, loading, error, refetch: fetchRoutes };
}
