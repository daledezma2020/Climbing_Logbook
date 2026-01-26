import { useState, useEffect } from 'react';
import type { ClimbRoute } from '@/types/climbroute';

interface CreateRoutePayload {
  Name: string;
  Grade: string;
  Location: string;
  Setter: string | null;
  Type: string | null;
  Picture: string | null;
  Video: string | null;
  AverageRating: number;
}

interface UseClimbRoutesReturn {
  routes: ClimbRoute[];
  loading: boolean;
  error: string | null;
  refetch: () => void;
  createRoute: (payload: CreateRoutePayload) => Promise<void>;
  updateRoute: (id: number, payload: Partial<CreateRoutePayload>) => Promise<void>;
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
        id: route.id ?? route.Id,
        name: route.name ?? route.Name,
        grade: route.grade ?? route.Grade,
        averageRating: route.averageRating ?? route.AverageRating,
        setter: route.setter ?? route.Setter,
        type: route.type ?? route.Type,
        picture: route.picture ?? route.Picture,
        video: route.video ?? route.Video,
        location: route.location ?? route.Location,
        createdAt: route.createdAt ?? route.CreatedAt,
        updatedAt: route.updatedAt ?? route.UpdatedAt
      }));

      setRoutes(transformedRoutes);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch routes');
    } finally {
      setLoading(false);
    }
  };

  const createRoute = async (payload: CreateRoutePayload) => {
    const response = await fetch('http://localhost:5050/api/routes', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }

    // Refetch routes after successful creation
    await fetchRoutes();
  };

  const updateRoute = async(id: number, payload: Partial<CreateRoutePayload>) => {
    const response = await fetch(`http://localhost:5050/api/routes/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(payload),
    });

    if(!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
    }
    // Refetch routes after successful update
    await fetchRoutes();
  };

    useEffect(() => {
    fetchRoutes();
  }, []);

  return { routes, loading, error, refetch: fetchRoutes, createRoute, updateRoute };
}
