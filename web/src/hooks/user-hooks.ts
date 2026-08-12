import { useCallback, useEffect, useRef, useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { ApiError, apiFetch } from "@/lib/api";
import type { LogEntry } from "@/types/catalog";
import type {
  ConnectionKind,
  CurrentUser,
  PagedResult,
  UpdateUserProfileInput,
  UserProfile,
  UserSummary,
} from "@/types/user";

const PAGE_SIZE = 25;
const SEARCH_DEBOUNCE_MS = 300;

export function useCurrentUser() {
  const { getAccessTokenSilently, isAuthenticated, isLoading: authLoading } =
    useAuth0();
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchCurrentUser = useCallback(async () => {
    if (!isAuthenticated) {
      setUser(null);
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const accessToken = await getAccessTokenSilently();
      setUser(await apiFetch<CurrentUser>("/auth/me", { accessToken }));
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to load your profile",
      );
    } finally {
      setLoading(false);
    }
  }, [getAccessTokenSilently, isAuthenticated]);

  useEffect(() => {
    if (authLoading) {
      return;
    }
    fetchCurrentUser();
  }, [authLoading, fetchCurrentUser]);

  return { user, loading, error, refetch: fetchCurrentUser };
}

export function useUserProfile(username: string | undefined) {
  const { getAccessTokenSilently, isAuthenticated, isLoading: authLoading } =
    useAuth0();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);

  const fetchProfile = useCallback(async () => {
    if (!username) {
      setProfile(null);
      setLoading(false);
      return;
    }

    try {
      setLoading(true);
      setError(null);
      setNotFound(false);
      // The token is what lets the API report isMe and isFollowedByMe.
      const accessToken = isAuthenticated
        ? await getAccessTokenSilently()
        : undefined;
      setProfile(
        await apiFetch<UserProfile>(`/users/${encodeURIComponent(username)}`, {
          accessToken,
        }),
      );
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setNotFound(true);
        setProfile(null);
      } else {
        setError(
          err instanceof Error ? err.message : "Failed to load this profile",
        );
      }
    } finally {
      setLoading(false);
    }
  }, [username, getAccessTokenSilently, isAuthenticated]);

  useEffect(() => {
    if (authLoading) {
      return;
    }
    fetchProfile();
  }, [authLoading, fetchProfile]);

  return { profile, loading, error, notFound, refetch: fetchProfile };
}

export function useUserSearch(query: string) {
  const { getAccessTokenSilently, isAuthenticated, isLoading: authLoading } =
    useAuth0();
  const [results, setResults] = useState<UserSummary[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const requestId = useRef(0);

  const trimmed = query.trim();

  useEffect(() => {
    if (authLoading) {
      return;
    }

    if (!trimmed) {
      requestId.current++;
      setResults([]);
      setError(null);
      setLoading(false);
      return;
    }

    setLoading(true);
    const timer = setTimeout(async () => {
      const id = ++requestId.current;
      try {
        setError(null);
        const accessToken = isAuthenticated
          ? await getAccessTokenSilently()
          : undefined;
        const params = new URLSearchParams({ q: trimmed, limit: "25" });
        const found = await apiFetch<UserSummary[]>(
          `/users/search?${params}`,
          { accessToken },
        );
        if (id === requestId.current) setResults(found);
      } catch (err) {
        if (id === requestId.current) {
          setError(
            err instanceof Error ? err.message : "Failed to search climbers",
          );
          setResults([]);
        }
      } finally {
        if (id === requestId.current) setLoading(false);
      }
    }, SEARCH_DEBOUNCE_MS);

    return () => clearTimeout(timer);
  }, [trimmed, authLoading, isAuthenticated, getAccessTokenSilently]);

  return { results, loading, error };
}

export function useFollow() {
  const { getAccessTokenSilently } = useAuth0();

  const send = useCallback(
    async (username: string, method: "POST" | "DELETE") => {
      const accessToken = await getAccessTokenSilently();
      await apiFetch<void>(
        `/users/${encodeURIComponent(username)}/follow`,
        { method, accessToken },
      );
    },
    [getAccessTokenSilently],
  );

  const follow = useCallback(
    (username: string) => send(username, "POST"),
    [send],
  );

  const unfollow = useCallback(
    (username: string) => send(username, "DELETE"),
    [send],
  );

  return { follow, unfollow };
}

export function useUserConnections(
  username: string | undefined,
  kind: ConnectionKind,
) {
  const { getAccessTokenSilently, isAuthenticated, isLoading: authLoading } =
    useAuth0();
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchPage = useCallback(
    async (skip: number) => {
      if (!username) {
        setUsers([]);
        setTotal(0);
        return;
      }

      try {
        setError(null);
        const accessToken = isAuthenticated
          ? await getAccessTokenSilently()
          : undefined;
        const page = await apiFetch<PagedResult<UserSummary>>(
          `/users/${encodeURIComponent(username)}/${kind}?skip=${skip}&take=${PAGE_SIZE}`,
          { accessToken },
        );
        setUsers((current) =>
          skip === 0 ? page.items : [...current, ...page.items],
        );
        setTotal(page.total);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load climbers");
      }
    },
    [username, kind, isAuthenticated, getAccessTokenSilently],
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
    await fetchPage(users.length);
    setLoadingMore(false);
  }, [users.length, fetchPage]);

  return {
    users,
    total,
    loading,
    loadingMore,
    error,
    hasMore: users.length < total,
    loadMore,
    refetch: reload,
  };
}

export function useMyProfile() {
  const { getAccessTokenSilently, isAuthenticated, isLoading: authLoading } =
    useAuth0();
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchProfile = useCallback(async () => {
    if (!isAuthenticated) {
      setProfile(null);
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const accessToken = await getAccessTokenSilently();
      setProfile(await apiFetch<UserProfile>("/users/me", { accessToken }));
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to load your profile",
      );
    } finally {
      setLoading(false);
    }
  }, [getAccessTokenSilently, isAuthenticated]);

  useEffect(() => {
    if (authLoading) {
      return;
    }
    fetchProfile();
  }, [authLoading, fetchProfile]);

  return { profile, loading, error, refetch: fetchProfile };
}

export function useUserLogEntries(username: string | undefined) {
  const [entries, setEntries] = useState<LogEntry[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchPage = useCallback(
    async (skip: number) => {
      if (!username) {
        setEntries([]);
        setTotal(0);
        return;
      }

      try {
        setError(null);
        const page = await apiFetch<PagedResult<LogEntry>>(
          `/users/${encodeURIComponent(username)}/logentries?skip=${skip}&take=${PAGE_SIZE}`,
        );
        setEntries((current) =>
          skip === 0 ? page.items : [...current, ...page.items],
        );
        setTotal(page.total);
      } catch (err) {
        setError(
          err instanceof Error ? err.message : "Failed to load logged climbs",
        );
      }
    },
    [username],
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
    void reload();
  }, [reload]);

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

export function updateMyProfile(
  payload: UpdateUserProfileInput,
  accessToken: string,
) {
  return apiFetch<UserProfile>("/users/me", {
    method: "PUT",
    accessToken,
    body: JSON.stringify(payload),
  });
}

export function uploadMyAvatar(file: File, accessToken: string) {
  const body = new FormData();
  body.append("file", file);
  return apiFetch<UserProfile>("/users/me/avatar", {
    method: "POST",
    accessToken,
    body,
  });
}

export function useUpdateMyProfile(onSaved?: (profile: UserProfile) => void) {
  const { getAccessTokenSilently } = useAuth0();
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const saveProfile = useCallback(
    async (payload: UpdateUserProfileInput) => {
      try {
        setSaving(true);
        setError(null);
        const accessToken = await getAccessTokenSilently();
        const saved = await updateMyProfile(payload, accessToken);
        onSaved?.(saved);
        return saved;
      } catch (err) {
        setError(
          err instanceof Error
            ? `The API could not save your profile. ${err.message}`
            : "The API could not save your profile. Check the details, then try again.",
        );
        return null;
      } finally {
        setSaving(false);
      }
    },
    [getAccessTokenSilently, onSaved],
  );

  const uploadAvatar = useCallback(
    async (file: File) => {
      try {
        setUploading(true);
        setError(null);
        const accessToken = await getAccessTokenSilently();
        const saved = await uploadMyAvatar(file, accessToken);
        onSaved?.(saved);
        return saved;
      } catch (err) {
        setError(
          err instanceof Error
            ? `The API could not upload that image. ${err.message}`
            : "The API could not upload that image. Try a JPEG, PNG, or WebP file.",
        );
        return null;
      } finally {
        setUploading(false);
      }
    },
    [getAccessTokenSilently, onSaved],
  );

  return { saveProfile, uploadAvatar, saving, uploading, error, setError };
}
