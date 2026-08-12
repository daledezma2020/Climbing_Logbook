import { useCallback, useEffect, useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { apiFetch } from "@/lib/api";
import type { LogEntry } from "@/types/catalog";
import type {
  Comment,
  CommentTarget,
  CreateCommentInput,
  HomeStats,
} from "@/types/social";
import type { PagedResult } from "@/types/user";

const PAGE_SIZE = 25;

function targetPath(target: CommentTarget): string {
  return target.kind === "logEntry"
    ? `/logentries/${target.id}`
    : `/climbs/${target.id}`;
}

export function useFeed() {
  const { getAccessTokenSilently, isAuthenticated, isLoading: authLoading } =
    useAuth0();
  const [entries, setEntries] = useState<LogEntry[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchPage = useCallback(
    async (skip: number) => {
      if (!isAuthenticated) {
        setEntries([]);
        setTotal(0);
        return;
      }

      try {
        setError(null);
        const accessToken = await getAccessTokenSilently();
        const page = await apiFetch<PagedResult<LogEntry>>(
          `/feed?skip=${skip}&take=${PAGE_SIZE}`,
          { accessToken },
        );
        setEntries((current) =>
          skip === 0 ? page.items : [...current, ...page.items],
        );
        setTotal(page.total);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load your feed");
      }
    },
    [isAuthenticated, getAccessTokenSilently],
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

export function useHomeStats() {
  const { getAccessTokenSilently, isAuthenticated, isLoading: authLoading } =
    useAuth0();
  const [stats, setStats] = useState<HomeStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchStats = useCallback(async () => {
    if (!isAuthenticated) {
      setStats(null);
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const accessToken = await getAccessTokenSilently();
      setStats(await apiFetch<HomeStats>("/users/me/stats", { accessToken }));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load your stats");
    } finally {
      setLoading(false);
    }
  }, [isAuthenticated, getAccessTokenSilently]);

  useEffect(() => {
    if (authLoading) {
      return;
    }
    void fetchStats();
  }, [authLoading, fetchStats]);

  return { stats, loading, error, refetch: fetchStats };
}

export function useLikeToggle() {
  const { getAccessTokenSilently } = useAuth0();

  const send = useCallback(
    async (logEntryId: number, method: "POST" | "DELETE") => {
      const accessToken = await getAccessTokenSilently();
      await apiFetch<void>(`/logentries/${logEntryId}/like`, {
        method,
        accessToken,
      });
    },
    [getAccessTokenSilently],
  );

  const like = useCallback((id: number) => send(id, "POST"), [send]);
  const unlike = useCallback((id: number) => send(id, "DELETE"), [send]);

  return { like, unlike };
}

export function useComments(target: CommentTarget) {
  const { getAccessTokenSilently, isAuthenticated, isLoading: authLoading } =
    useAuth0();
  const [comments, setComments] = useState<Comment[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { kind, id } = target;
  const path = targetPath({ kind, id });

  const fetchPage = useCallback(
    async (skip: number) => {
      try {
        setError(null);
        const accessToken = isAuthenticated
          ? await getAccessTokenSilently()
          : undefined;
        const page = await apiFetch<PagedResult<Comment>>(
          `${path}/comments?skip=${skip}&take=${PAGE_SIZE}`,
          { accessToken },
        );
        setComments((current) =>
          skip === 0 ? page.items : [...current, ...page.items],
        );
        setTotal(page.total);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load comments");
      }
    },
    [path, isAuthenticated, getAccessTokenSilently],
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
    await fetchPage(comments.length);
    setLoadingMore(false);
  }, [comments.length, fetchPage]);

  const addComment = useCallback(
    async (input: CreateCommentInput) => {
      const accessToken = await getAccessTokenSilently();
      const created = await apiFetch<Comment>(`${path}/comments`, {
        method: "POST",
        accessToken,
        body: JSON.stringify(input),
      });
      setComments((current) => [created, ...current]);
      setTotal((current) => current + 1);
      return created;
    },
    [path, getAccessTokenSilently],
  );

  const deleteComment = useCallback(
    async (commentId: number) => {
      const accessToken = await getAccessTokenSilently();
      await apiFetch<void>(`/comments/${commentId}`, {
        method: "DELETE",
        accessToken,
      });
      setComments((current) => current.filter((c) => c.id !== commentId));
      setTotal((current) => Math.max(current - 1, 0));
    },
    [getAccessTokenSilently],
  );

  return {
    comments,
    total,
    loading,
    loadingMore,
    error,
    hasMore: comments.length < total,
    loadMore,
    addComment,
    deleteComment,
    refetch: reload,
  };
}
