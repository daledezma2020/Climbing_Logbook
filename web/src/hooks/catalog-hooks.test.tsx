import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  createLogEntry,
  createManualClimb,
  useBoardConfigurations,
  useClimbs,
  useLogEntries,
  usePlaces,
  useSearch,
} from "@/hooks/catalog-hooks";
import { apiFetch } from "@/lib/api";
import { climb } from "@/test/fixtures";

const auth = vi.hoisted(() => ({
  getAccessTokenSilently: vi.fn(),
  loginWithRedirect: vi.fn(),
  isAuthenticated: true,
}));

vi.mock("@auth0/auth0-react", () => ({ useAuth0: () => auth }));
vi.mock("@/lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("@/lib/api")>();
  return { ...original, apiFetch: vi.fn() };
});

describe("catalog hooks", () => {
  beforeEach(() => {
    auth.isAuthenticated = true;
    auth.getAccessTokenSilently.mockResolvedValue("access-token");
  });

  it("loads climbs and exposes the completed state", async () => {
    vi.mocked(apiFetch).mockResolvedValueOnce([climb()]);
    const { result } = renderHook(() => useClimbs());

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.climbs).toEqual([climb()]);
    expect(result.current.error).toBeNull();
  });

  it("redirects unauthenticated deletion attempts", async () => {
    auth.isAuthenticated = false;
    vi.mocked(apiFetch).mockResolvedValueOnce([climb()]);
    const { result } = renderHook(() => useClimbs());
    await waitFor(() => expect(result.current.loading).toBe(false));

    await act(() => result.current.deleteClimb(1));

    expect(auth.loginWithRedirect).toHaveBeenCalledOnce();
    expect(apiFetch).toHaveBeenCalledTimes(1);
  });

  it("authenticates deletion and refreshes the catalog", async () => {
    vi.mocked(apiFetch)
      .mockResolvedValueOnce([climb()])
      .mockResolvedValueOnce(undefined)
      .mockResolvedValueOnce([]);
    const { result } = renderHook(() => useClimbs());
    await waitFor(() => expect(result.current.loading).toBe(false));

    await act(() => result.current.deleteClimb(1));

    expect(apiFetch).toHaveBeenNthCalledWith(2, "/climbs/1", {
      method: "DELETE",
      accessToken: "access-token",
    });
    await waitFor(() => expect(result.current.climbs).toEqual([]));
  });

  it("keeps the newest search result when an older request finishes last", async () => {
    let resolveOld: (value: unknown) => void = () => undefined;
    let resolveNew: (value: unknown) => void = () => undefined;
    vi.mocked(apiFetch)
      .mockReturnValueOnce(new Promise((resolve) => { resolveOld = resolve; }))
      .mockReturnValueOnce(new Promise((resolve) => { resolveNew = resolve; }));
    const { result } = renderHook(() => useSearch());

    act(() => {
      void result.current.search("old");
      void result.current.search("new");
    });
    const newest = { results: [], providers: { local: "complete" as const } };
    await act(async () => resolveNew(newest));
    await waitFor(() => expect(result.current.results).toEqual(newest));
    await act(async () => resolveOld({ results: [{ key: "stale" }], providers: {} }));

    expect(result.current.results).toEqual(newest);
    expect(result.current.loading).toBe(false);
  });

  it("exposes logbook loading failures", async () => {
    vi.mocked(apiFetch).mockRejectedValueOnce(new Error("API unavailable"));
    const { result } = renderHook(() => useLogEntries());

    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.entries).toEqual([]);
    expect(result.current.error).toBe("API unavailable");
  });

  it("loads places and board configurations", async () => {
    vi.mocked(apiFetch)
      .mockResolvedValueOnce([{ id: 1, name: "Gym" }])
      .mockResolvedValueOnce([{ id: 2, name: "Board" }]);
    const places = renderHook(() => usePlaces());
    const boards = renderHook(() => useBoardConfigurations());

    await waitFor(() => expect(places.result.current.loading).toBe(false));
    await waitFor(() => expect(boards.result.current.loading).toBe(false));
    expect(places.result.current.places).toEqual([{ id: 1, name: "Gym" }]);
    expect(boards.result.current.boards).toEqual([{ id: 2, name: "Board" }]);
  });

  it("reports search failures and reset clears the state", async () => {
    vi.mocked(apiFetch).mockRejectedValueOnce(new Error("Search unavailable"));
    const { result } = renderHook(() => useSearch());

    await act(() => result.current.search("test"));
    expect(result.current.error).toBe("Search unavailable");
    act(() => result.current.reset());
    expect(result.current.error).toBeNull();
    expect(result.current.results).toBeNull();
    expect(result.current.loading).toBe(false);
  });

  it("sends authenticated create payloads to their API endpoints", async () => {
    vi.mocked(apiFetch).mockResolvedValue({});
    await createManualClimb({
      name: "Test", discipline: "Bouldering", gradeSystem: "VScale", grade: "V2", boardConfigurationName: "Home Wall",
    }, "token");
    await createLogEntry({ climbId: 1, status: "Attempted", rating: 2 }, "token");

    expect(apiFetch).toHaveBeenNthCalledWith(1, "/climbs/manual", {
      method: "POST", accessToken: "token", body: JSON.stringify({
        name: "Test", discipline: "Bouldering", gradeSystem: "VScale", grade: "V2", boardConfigurationName: "Home Wall",
      }),
    });
    expect(apiFetch).toHaveBeenNthCalledWith(2, "/logentries", {
      method: "POST", accessToken: "token", body: JSON.stringify({ climbId: 1, status: "Attempted", rating: 2 }),
    });
  });
});
