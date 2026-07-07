import { useEffect, useState } from "react";
import { apiFetch } from "@/lib/api";
import type { Setter } from "@/types/setter";

interface UseSettersReturn {
  setters: Setter[];
  loading: boolean;
  error: string | null;
}

export function useSetters(): UseSettersReturn {
  const [setters, setSetters] = useState<Setter[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    apiFetch<Setter[]>("/setters")
      .then((data) => active && setSetters(data))
      .catch(
        (err) =>
          active &&
          setError(err instanceof Error ? err.message : "Failed to fetch setters"),
      )
      .finally(() => active && setLoading(false));
    return () => {
      active = false;
    };
  }, []);

  return { setters, loading, error };
}
