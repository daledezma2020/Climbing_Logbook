import { useState, useEffect } from "react";
import type { Location, CreateLocationInput } from "@/types/location";

interface UseLocationsReturn {
  locations: Location[];
  loading: boolean;
  error: string | null;
}

export function useLocations(): UseLocationsReturn {
  const [locations, setLocations] = useState<Location[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchLocations = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await fetch("http://localhost:5050/api/locations");

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      setLocations(data);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to fetch locations",
      );
    } finally {
      setLoading(false);
    }
  };

  const fetchLocationById = async (id: number): Promise<Location | null> => {
    try {
      const response = await fetch(`http://localhost:5050/api/locations/${id}`);

      if (!response.ok) {
        if (response.status === 404) {
          return null;
        }
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      return data as Location;
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to fetch locations",
      );
      return null;
    }
  };

  const createLocation = async (payload: CreateLocationInput) => {
    const response = await fetch("http://localhost:5050/api/locations", {
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
    await fetchLocations();
  };

  useEffect(() => {
    fetchLocations();
  }, []);

  return { locations, loading, error };
}
