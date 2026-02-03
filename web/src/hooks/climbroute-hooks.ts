import { useState, useEffect } from "react";
import type {
  ClimbRoute,
  CreateClimbRouteInput,
  UpdateClimbRouteInput,
} from "@/types/climbroute";

interface UseClimbRoutesReturn {
  routes: ClimbRoute[];
  loading: boolean;
  error: string | null;
  refetch: () => void;
  createRoute: (payload: CreateClimbRouteInput) => Promise<void>;
  updateRoute: (
    id: number,
    payload: Partial<UpdateClimbRouteInput>,
  ) => Promise<void>;
}

export function useClimbRoutes(): UseClimbRoutesReturn {
  const [routes, setRoutes] = useState<ClimbRoute[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchRoutes = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await fetch("http://localhost:5050/api/routes");

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      setRoutes(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to fetch routes");
    } finally {
      setLoading(false);
    }
  };

  const createRoute = async (payload: CreateClimbRouteInput) => {
    const response = await fetch("http://localhost:5050/api/routes", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(
        errorData.message || `HTTP error! status: ${response.status}`,
      );
    }

    // Refetch routes after successful creation
    await fetchRoutes();
  };

  const updateRoute = async (
    id: number,
    payload: Partial<UpdateClimbRouteInput>,
  ) => {
    const response = await fetch(`http://localhost:5050/api/routes/${id}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new Error(
        errorData.message || `HTTP error! status: ${response.status}`,
      );
    }
    // Refetch routes after successful update
    await fetchRoutes();
  };

  useEffect(() => {
    fetchRoutes();
  }, []);

  return {
    routes,
    loading,
    error,
    refetch: fetchRoutes,
    createRoute,
    updateRoute,
  };
}
