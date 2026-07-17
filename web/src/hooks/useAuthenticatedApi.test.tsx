import { renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useAuthenticatedApi } from "@/hooks/useAuthenticatedApi";
import { API_BASE_URL } from "@/lib/api";

const getAccessTokenSilently = vi.hoisted(() => vi.fn());
vi.mock("@auth0/auth0-react", () => ({ useAuth0: () => ({ getAccessTokenSilently }) }));

describe("useAuthenticatedApi", () => {
  it("adds the current access token without discarding caller headers", async () => {
    getAccessTokenSilently.mockResolvedValue("fresh-token");
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 204 })));
    const { result } = renderHook(() => useAuthenticatedApi());

    await result.current.fetchWithAuth("/api/auth/me", { headers: { "X-Test": "yes" } });

    const [url, init] = vi.mocked(fetch).mock.calls[0]!;
    expect(url).toBe(`${API_BASE_URL}/api/auth/me`);
    const headers = new Headers(init?.headers);
    expect(headers.get("Authorization")).toBe("Bearer fresh-token");
    expect(headers.get("X-Test")).toBe("yes");
  });
});
