import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  useComments,
  useFeed,
  useHomeStats,
  useLikeToggle,
} from "@/hooks/social-hooks";
import { apiFetch } from "@/lib/api";
import { comment, homeStats, logEntry } from "@/test/fixtures";

const auth = vi.hoisted(() => ({
  getAccessTokenSilently: vi.fn(),
  isAuthenticated: true,
  isLoading: false,
}));

vi.mock("@auth0/auth0-react", () => ({ useAuth0: () => auth }));
vi.mock("@/lib/api", () => ({ apiFetch: vi.fn() }));

beforeEach(() => {
  vi.clearAllMocks();
  auth.isAuthenticated = true;
  auth.isLoading = false;
  auth.getAccessTokenSilently.mockResolvedValue("access-token");
});

describe("useFeed", () => {
  it("loads the first page with a bearer token", async () => {
    vi.mocked(apiFetch).mockResolvedValue({
      items: [logEntry()],
      total: 1,
      skip: 0,
      take: 25,
    });
    const { result } = renderHook(() => useFeed());

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(apiFetch).toHaveBeenCalledWith("/feed?skip=0&take=25", {
      accessToken: "access-token",
    });
    expect(result.current.entries).toHaveLength(1);
    expect(result.current.hasMore).toBe(false);
  });

  it("appends the next page rather than replacing it", async () => {
    vi.mocked(apiFetch).mockResolvedValueOnce({
      items: [logEntry({ id: 1 })],
      total: 2,
      skip: 0,
      take: 25,
    });
    const { result } = renderHook(() => useFeed());
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.hasMore).toBe(true);

    vi.mocked(apiFetch).mockResolvedValueOnce({
      items: [logEntry({ id: 2 })],
      total: 2,
      skip: 1,
      take: 25,
    });
    await act(() => result.current.loadMore());

    expect(apiFetch).toHaveBeenLastCalledWith("/feed?skip=1&take=25", {
      accessToken: "access-token",
    });
    expect(result.current.entries.map((e) => e.id)).toEqual([1, 2]);
    expect(result.current.hasMore).toBe(false);
  });

  it("stays empty for a signed-out visitor", async () => {
    auth.isAuthenticated = false;
    const { result } = renderHook(() => useFeed());

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(apiFetch).not.toHaveBeenCalled();
    expect(result.current.entries).toEqual([]);
  });

  it("exposes request failures", async () => {
    vi.mocked(apiFetch).mockRejectedValue(new Error("Feed API failed"));
    const { result } = renderHook(() => useFeed());

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.error).toBe("Feed API failed");
  });
});

describe("useHomeStats", () => {
  it("loads the caller's stats", async () => {
    vi.mocked(apiFetch).mockResolvedValue(homeStats());
    const { result } = renderHook(() => useHomeStats());

    await waitFor(() => expect(result.current.stats).not.toBeNull());
    expect(apiFetch).toHaveBeenCalledWith("/users/me/stats", {
      accessToken: "access-token",
    });
    expect(result.current.stats?.core.totalSends).toBe(12);
  });

  it("skips the request while Auth0 is still resolving", async () => {
    auth.isLoading = true;
    renderHook(() => useHomeStats());

    await waitFor(() => expect(apiFetch).not.toHaveBeenCalled());
  });
});

describe("useLikeToggle", () => {
  it("posts and deletes against the log entry's like route", async () => {
    vi.mocked(apiFetch).mockResolvedValue(undefined);
    const { result } = renderHook(() => useLikeToggle());

    await act(() => result.current.like(7));
    expect(apiFetch).toHaveBeenCalledWith("/logentries/7/like", {
      method: "POST",
      accessToken: "access-token",
    });

    await act(() => result.current.unlike(7));
    expect(apiFetch).toHaveBeenLastCalledWith("/logentries/7/like", {
      method: "DELETE",
      accessToken: "access-token",
    });
  });
});

describe("useComments", () => {
  it("reads a log entry thread", async () => {
    vi.mocked(apiFetch).mockResolvedValue({
      items: [comment()],
      total: 1,
      skip: 0,
      take: 25,
    });
    const { result } = renderHook(() =>
      useComments({ kind: "logEntry", id: 3 }),
    );

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(apiFetch).toHaveBeenCalledWith(
      "/logentries/3/comments?skip=0&take=25",
      { accessToken: "access-token" },
    );
  });

  it("reads a climb thread from the climb route instead", async () => {
    vi.mocked(apiFetch).mockResolvedValue({
      items: [],
      total: 0,
      skip: 0,
      take: 25,
    });
    const { result } = renderHook(() => useComments({ kind: "climb", id: 9 }));

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(apiFetch).toHaveBeenCalledWith("/climbs/9/comments?skip=0&take=25", {
      accessToken: "access-token",
    });
  });

  it("puts a new comment at the top and bumps the total", async () => {
    vi.mocked(apiFetch).mockResolvedValueOnce({
      items: [comment({ id: 1, content: "older" })],
      total: 1,
      skip: 0,
      take: 25,
    });
    const { result } = renderHook(() =>
      useComments({ kind: "logEntry", id: 3 }),
    );
    await waitFor(() => expect(result.current.loading).toBe(false));

    vi.mocked(apiFetch).mockResolvedValueOnce(
      comment({ id: 2, content: "newer" }),
    );
    await act(() => result.current.addComment({ content: "newer" }));

    expect(result.current.comments.map((c) => c.content)).toEqual([
      "newer",
      "older",
    ]);
    expect(result.current.total).toBe(2);
  });

  it("deletes through the flat comment route and drops it from the list", async () => {
    vi.mocked(apiFetch).mockResolvedValueOnce({
      items: [comment({ id: 5 })],
      total: 1,
      skip: 0,
      take: 25,
    });
    const { result } = renderHook(() =>
      useComments({ kind: "logEntry", id: 3 }),
    );
    await waitFor(() => expect(result.current.loading).toBe(false));

    vi.mocked(apiFetch).mockResolvedValueOnce(undefined);
    await act(() => result.current.deleteComment(5));

    expect(apiFetch).toHaveBeenLastCalledWith("/comments/5", {
      method: "DELETE",
      accessToken: "access-token",
    });
    expect(result.current.comments).toEqual([]);
    expect(result.current.total).toBe(0);
  });

  it("reads a thread anonymously without a token", async () => {
    auth.isAuthenticated = false;
    vi.mocked(apiFetch).mockResolvedValue({
      items: [],
      total: 0,
      skip: 0,
      take: 25,
    });
    const { result } = renderHook(() => useComments({ kind: "climb", id: 9 }));

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(apiFetch).toHaveBeenCalledWith("/climbs/9/comments?skip=0&take=25", {
      accessToken: undefined,
    });
  });
});
