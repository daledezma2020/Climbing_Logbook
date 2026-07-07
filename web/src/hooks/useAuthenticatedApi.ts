import { useAuth0 } from "@auth0/auth0-react";
import { API_BASE_URL } from "@/lib/api";

export function useAuthenticatedApi() {
  const { getAccessTokenSilently } = useAuth0();

  const fetchWithAuth = async (path: string, init: RequestInit = {}) => {
    const token = await getAccessTokenSilently();
    const headers = new Headers(init.headers);
    headers.set("Authorization", `Bearer ${token}`);

    return fetch(`${API_BASE_URL}${path}`, {
      ...init,
      headers,
    });
  };

  return { fetchWithAuth };
}
