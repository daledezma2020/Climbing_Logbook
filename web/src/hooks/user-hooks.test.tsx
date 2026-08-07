import { renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useCurrentUser } from "@/hooks/user-hooks";
import { apiFetch } from "@/lib/api";
import { currentUser } from "@/test/fixtures";

const auth = vi.hoisted(() => ({
  getAccessTokenSilently: vi.fn(),
  isAuthenticated: true,
  isLoading: false,
}));

vi.mock("@auth0/auth0-react", () => ({ useAuth0: () => auth }));
vi.mock("@/lib/api", () => ({ apiFetch: vi.fn() }));

describe("useCurrentUser", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    auth.isAuthenticated = true;
    auth.isLoading = false;
    auth.getAccessTokenSilently.mockResolvedValue("access-token");
  });

  it("provisions and loads the signed-in user with a bearer token", async () => {
    vi.mocked(apiFetch).mockResolvedValue(currentUser());
    const { result } = renderHook(() => useCurrentUser());

    await waitFor(() => expect(result.current.user).not.toBeNull());
    expect(apiFetch).toHaveBeenCalledWith("/auth/me", {
      accessToken: "access-token",
    });
    expect(result.current.user?.username).toBe("alex");
    expect(result.current.error).toBeNull();
  });

  it("does not call the API while Auth0 is still resolving", async () => {
    auth.isLoading = true;
    renderHook(() => useCurrentUser());

    await waitFor(() => expect(apiFetch).not.toHaveBeenCalled());
  });

  it("stays anonymous when the visitor is signed out", async () => {
    auth.isAuthenticated = false;
    const { result } = renderHook(() => useCurrentUser());

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(apiFetch).not.toHaveBeenCalled();
    expect(result.current.user).toBeNull();
  });

  it("exposes request failures", async () => {
    vi.mocked(apiFetch).mockRejectedValue(new Error("Profile API failed"));
    const { result } = renderHook(() => useCurrentUser());

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.error).toBe("Profile API failed");
    expect(result.current.user).toBeNull();
  });
});
