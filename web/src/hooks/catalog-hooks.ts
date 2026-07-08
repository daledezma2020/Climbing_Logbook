import { useCallback, useEffect, useRef, useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { apiFetch } from "@/lib/api";
import type {
  BoardConfiguration,
  Climb,
  CreateLogEntryInput,
  CreateManualClimbInput,
  LogEntry,
  Place,
  SearchResponse,
} from "@/types/catalog";

export function useClimbs() {
  const { getAccessTokenSilently, loginWithRedirect, isAuthenticated } =
    useAuth0();
  const [climbs, setClimbs] = useState<Climb[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchClimbs = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      setClimbs(await apiFetch<Climb[]>("/climbs"));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to fetch climbs");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchClimbs();
  }, [fetchClimbs]);

  const deleteClimb = useCallback(
    async (id: number) => {
      if (!isAuthenticated) {
        await loginWithRedirect();
        return;
      }

      const accessToken = await getAccessTokenSilently();
      await apiFetch<void>(`/climbs/${id}`, { method: "DELETE", accessToken });
      await fetchClimbs();
    },
    [fetchClimbs, getAccessTokenSilently, isAuthenticated, loginWithRedirect],
  );

  return { climbs, loading, error, refetch: fetchClimbs, deleteClimb };
}

export function useLogEntries() {
  const [entries, setEntries] = useState<LogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchEntries = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      setEntries(await apiFetch<LogEntry[]>("/logentries"));
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to fetch log entries",
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchEntries();
  }, [fetchEntries]);

  return { entries, loading, error, refetch: fetchEntries };
}

export function usePlaces() {
  const [places, setPlaces] = useState<Place[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchPlaces = useCallback(async () => {
    try {
      setLoading(true);
      setPlaces(await apiFetch<Place[]>("/places"));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchPlaces();
  }, [fetchPlaces]);

  return { places, loading, refetch: fetchPlaces };
}

export function useBoardConfigurations() {
  const [boards, setBoards] = useState<BoardConfiguration[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    apiFetch<BoardConfiguration[]>("/boardconfigurations")
      .then((data) => active && setBoards(data))
      .finally(() => active && setLoading(false));
    return () => {
      active = false;
    };
  }, []);

  return { boards, loading };
}

export function useSearch() {
  const [results, setResults] = useState<SearchResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const requestId = useRef(0);

  const search = useCallback(
    async (query: string, limit = 10) => {
      const id = ++requestId.current;
      try {
        setLoading(true);
        setError(null);
        const params = new URLSearchParams({ q: query, limit: String(limit) });
        const data = await apiFetch<SearchResponse>(`/search?${params}`);
        if (id === requestId.current) setResults(data);
      } catch (err) {
        if (id === requestId.current) {
          setError(err instanceof Error ? err.message : "Search failed");
        }
      } finally {
        if (id === requestId.current) setLoading(false);
      }
    },
    [],
  );

  const reset = useCallback(() => {
    requestId.current++;
    setResults(null);
    setError(null);
    setLoading(false);
  }, []);

  return { results, loading, error, search, reset };
}

export function createLogEntry(
  payload: CreateLogEntryInput,
  accessToken: string,
) {
  return apiFetch<LogEntry>("/logentries", {
    method: "POST",
    accessToken,
    body: JSON.stringify(payload),
  });
}

export function createManualClimb(
  payload: CreateManualClimbInput,
  accessToken: string,
) {
  return apiFetch<Climb>("/climbs/manual", {
    method: "POST",
    accessToken,
    body: JSON.stringify(payload),
  });
}

export function importOpenBetaClimb(uuid: string, accessToken: string) {
  return apiFetch<Climb>(`/climbs/openbeta/${uuid}/import`, {
    method: "POST",
    accessToken,
  });
}
