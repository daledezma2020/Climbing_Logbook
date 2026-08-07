import { useCallback, useEffect, useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { apiFetch } from "@/lib/api";
import type { CurrentUser } from "@/types/user";

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
