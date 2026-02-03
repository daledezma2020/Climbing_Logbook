import { useState, useEffect } from "react";
import type { Setter, CreateSetterInput } from "@/types/setter";

interface UseSettersReturn {
  setters: Setter[];
  loading: boolean;
  error: string | null;
}

export function useSetters(): UseSettersReturn {
  const [setters, setSetters] = useState<Setter[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchSetters = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await fetch("http://localhost:5050/api/setters");

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      setSetters(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to fetch setters");
    } finally {
      setLoading(false);
    }
  };

  const fetchSetterById = async (id: number): Promise<Setter | null> => {
    try {
      const response = await fetch(`http://localhost:5050/api/setters/${id}`);

      if (!response.ok) {
        if (response.status === 404) {
          return null;
        }
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      return data as Setter;
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to fetch setter");
      return null;
    }
  };

  const createSetter = async (payload: CreateSetterInput) => {
    const response = await fetch("http://localhost:5050/api/setters", {
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
    await fetchSetters();
  };

  useEffect(() => {
    fetchSetters();
  }, []);

  return { setters, loading, error };
}
