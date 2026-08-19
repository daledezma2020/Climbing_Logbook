import { useCallback, useEffect, useRef, useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { ApiError, apiFetch } from "@/lib/api";
import type {
  BoardConfiguration,
  Climb,
  CreateLogEntryInput,
  CreateManualClimbInput,
  LogEntry,
  Place,
  SearchResponse,
} from "@/types/catalog";
import type { PagedResult } from "@/types/user";

const CLIMB_PAGE_SIZE = 25;

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

export function useClimb(id: number | undefined) {
  const [climb, setClimb] = useState<Climb | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);

  const fetchClimb = useCallback(async () => {
    if (id === undefined || Number.isNaN(id)) {
      setClimb(null);
      setNotFound(true);
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);
      setNotFound(false);
      setClimb(await apiFetch<Climb>(`/climbs/${id}`));
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setNotFound(true);
        setClimb(null);
      } else {
        setError(err instanceof Error ? err.message : "Failed to load this climb");
      }
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    void fetchClimb();
  }, [fetchClimb]);

  return { climb, loading, error, notFound, refetch: fetchClimb };
}

export function useClimbLogEntries(id: number | undefined) {
  const { getAccessTokenSilently, isAuthenticated, isLoading: authLoading } =
    useAuth0();
  const [entries, setEntries] = useState<LogEntry[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchPage = useCallback(
    async (skip: number) => {
      if (id === undefined || Number.isNaN(id)) {
        setEntries([]);
        setTotal(0);
        return;
      }

      try {
        setError(null);
        const accessToken = isAuthenticated
          ? await getAccessTokenSilently()
          : undefined;
        const page = await apiFetch<PagedResult<LogEntry>>(
          `/climbs/${id}/logentries?skip=${skip}&take=${CLIMB_PAGE_SIZE}`,
          { accessToken },
        );
        setEntries((current) =>
          skip === 0 ? page.items : [...current, ...page.items],
        );
        setTotal(page.total);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load ascents");
      }
    },
    [id, isAuthenticated, getAccessTokenSilently],
  );

  const reload = useCallback(async () => {
    try {
      setLoading(true);
      await fetchPage(0);
    } finally {
      setLoading(false);
    }
  }, [fetchPage]);

  useEffect(() => {
    if (authLoading) {
      return;
    }
    void reload();
  }, [authLoading, reload]);

  const loadMore = useCallback(async () => {
    setLoadingMore(true);
    await fetchPage(entries.length);
    setLoadingMore(false);
  }, [entries.length, fetchPage]);

  return {
    entries,
    total,
    loading,
    loadingMore,
    error,
    hasMore: entries.length < total,
    loadMore,
    refetch: reload,
  };
}

export function useLogEntries(userId?: number) {
  const [entries, setEntries] = useState<LogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchEntries = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const path =
        userId === undefined ? "/logentries" : `/logentries?userId=${userId}`;
      setEntries(await apiFetch<LogEntry[]>(path));
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to fetch log entries",
      );
    } finally {
      setLoading(false);
    }
  }, [userId]);

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
