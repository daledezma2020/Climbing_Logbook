import { act, renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useCurrentUser, useFollow, useUserSearch } from "@/hooks/user-hooks";
import { apiFetch } from "@/lib/api";
import { currentUser, userSummary } from "@/test/fixtures";

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

describe("useUserSearch", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    auth.isAuthenticated = true;
    auth.isLoading = false;
    auth.getAccessTokenSilently.mockResolvedValue("access-token");
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("waits out the debounce before issuing a single request", async () => {
    vi.useFakeTimers();
    vi.mocked(apiFetch).mockResolvedValue([userSummary({ username: "sam" })]);

    const { result, rerender } = renderHook(({ q }) => useUserSearch(q), {
      initialProps: { q: "s" },
    });

    rerender({ q: "sa" });
    rerender({ q: "sam" });

    await act(async () => { await vi.advanceTimersByTimeAsync(299); });
    expect(apiFetch).not.toHaveBeenCalled();

    await act(async () => { await vi.advanceTimersByTimeAsync(1); });
    expect(apiFetch).toHaveBeenCalledTimes(1);
    expect(vi.mocked(apiFetch).mock.calls[0][0]).toContain("q=sam");
    expect(result.current.results).toHaveLength(1);
  });

  it("sends the caller's token so follow state can be resolved", async () => {
    vi.useFakeTimers();
    vi.mocked(apiFetch).mockResolvedValue([]);

    renderHook(() => useUserSearch("sam"));
    await act(async () => { await vi.advanceTimersByTimeAsync(300); });

    expect(vi.mocked(apiFetch).mock.calls[0][1]).toMatchObject({
      accessToken: "access-token",
    });
  });

  it("searches anonymously when the visitor is signed out", async () => {
    vi.useFakeTimers();
    auth.isAuthenticated = false;
    vi.mocked(apiFetch).mockResolvedValue([]);

    renderHook(() => useUserSearch("sam"));
    await act(async () => { await vi.advanceTimersByTimeAsync(300); });

    expect(auth.getAccessTokenSilently).not.toHaveBeenCalled();
    expect(vi.mocked(apiFetch).mock.calls[0][1]).toMatchObject({
      accessToken: undefined,
    });
  });

  it("clears results and skips the API for a blank query", async () => {
    const { result } = renderHook(() => useUserSearch("   "));

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(apiFetch).not.toHaveBeenCalled();
    expect(result.current.results).toEqual([]);
  });

  it("surfaces search failures", async () => {
    vi.useFakeTimers();
    vi.mocked(apiFetch).mockRejectedValue(new Error("Search API failed"));

    const { result } = renderHook(() => useUserSearch("sam"));
    await act(async () => { await vi.advanceTimersByTimeAsync(300); });

    expect(result.current.error).toBe("Search API failed");
    expect(result.current.results).toEqual([]);
  });
});

describe("useFollow", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    auth.getAccessTokenSilently.mockResolvedValue("access-token");
  });

  it("posts and deletes the follow relationship with a bearer token", async () => {
    vi.mocked(apiFetch).mockResolvedValue(undefined);
    const { result } = renderHook(() => useFollow());

    await result.current.follow("sam");
    await result.current.unfollow("sam");

    expect(apiFetch).toHaveBeenNthCalledWith(1, "/users/sam/follow", {
      method: "POST",
      accessToken: "access-token",
    });
    expect(apiFetch).toHaveBeenNthCalledWith(2, "/users/sam/follow", {
      method: "DELETE",
      accessToken: "access-token",
    });
  });

  it("propagates failures to the caller so it can roll back", async () => {
    vi.mocked(apiFetch).mockRejectedValue(new Error("Follow API failed"));
    const { result } = renderHook(() => useFollow());

    await expect(result.current.follow("sam")).rejects.toThrow(
      "Follow API failed",
    );
  });
});
