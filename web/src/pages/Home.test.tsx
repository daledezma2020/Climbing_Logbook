import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import Home from "@/pages/Home";
import { currentUser, homeStats, logEntry, userSummary } from "@/test/fixtures";

const auth = vi.hoisted(() => ({
  isAuthenticated: true,
  isLoading: false,
  loginWithRedirect: vi.fn(),
  getAccessTokenSilently: vi.fn().mockResolvedValue("token"),
  user: undefined as { picture?: string } | undefined,
}));

const social = vi.hoisted(() => ({
  stats: null as ReturnType<typeof homeStats> | null,
  statsLoading: false,
  statsError: null as string | null,
  entries: [] as ReturnType<typeof logEntry>[],
  feedLoading: false,
  feedError: null as string | null,
  hasMore: false,
  loadMore: vi.fn(),
}));

vi.mock("@auth0/auth0-react", () => ({ useAuth0: () => auth }));

vi.mock("@/hooks/social-hooks", () => ({
  useHomeStats: () => ({
    stats: social.stats,
    loading: social.statsLoading,
    error: social.statsError,
  }),
  useFeed: () => ({
    entries: social.entries,
    total: social.entries.length,
    loading: social.feedLoading,
    loadingMore: false,
    error: social.feedError,
    hasMore: social.hasMore,
    loadMore: social.loadMore,
  }),
  useLikeToggle: () => ({ like: vi.fn(), unlike: vi.fn() }),
  useComments: () => ({
    comments: [],
    total: 0,
    loading: false,
    loadingMore: false,
    error: null,
    hasMore: false,
    loadMore: vi.fn(),
    addComment: vi.fn(),
    deleteComment: vi.fn(),
  }),
}));

vi.mock("@/hooks/user-hooks", () => ({
  useCurrentUser: () => ({ user: currentUser(), loading: false, error: null }),
}));

function renderHome() {
  return render(
    <MemoryRouter>
      <Home />
    </MemoryRouter>,
  );
}

function cardFor(title: string) {
  return within(screen.getByText(title).closest('[data-slot="card"]')!);
}

describe("Home", () => {
  beforeEach(() => {
    auth.isAuthenticated = true;
    auth.isLoading = false;
    social.stats = homeStats();
    social.statsLoading = false;
    social.statsError = null;
    social.entries = [];
    social.feedLoading = false;
    social.feedError = null;
    social.hasMore = false;
    window.localStorage.clear();
  });

  it("shows a sign-in call to action instead of a feed when signed out", () => {
    auth.isAuthenticated = false;
    renderHome();

    expect(
      screen.getByRole("button", { name: "Sign in to get started" }),
    ).toBeInTheDocument();
    expect(screen.queryByText("Recent activity")).not.toBeInTheDocument();
  });

  it("defaults to the core send stats and leaves the other groups off", () => {
    renderHome();

    expect(cardFor("Total sends").getByText("12")).toBeInTheDocument();
    expect(cardFor("Hardest sent").getByText("V6")).toBeInTheDocument();
    expect(cardFor("Send rate").getByText("80%")).toBeInTheDocument();

    expect(screen.queryByText("Sends this month")).not.toBeInTheDocument();
    expect(screen.queryByText("Most visited")).not.toBeInTheDocument();
    expect(screen.queryByText("Connections")).not.toBeInTheDocument();
  });

  it("ranks the hardest grade off the server's ladder rather than a digit sort", () => {
    social.stats = homeStats({
      core: {
        ...homeStats().core,
        hardestGrades: [
          { system: "Yds", grade: "5.14a", climbId: 2, climbName: "Hard Route" },
        ],
      },
    });
    renderHome();

    expect(cardFor("Hardest sent").getByText("5.14a")).toBeInTheDocument();
  });

  it("adds a KPI through the customizer and remembers it", async () => {
    const { unmount } = renderHome();

    await userEvent.click(screen.getByRole("button", { name: /Customize/ }));
    await userEvent.click(screen.getByRole("checkbox", { name: "Connections" }));
    await userEvent.click(screen.getByRole("button", { name: "Done" }));

    expect(cardFor("Connections").getByText("7")).toBeInTheDocument();

    unmount();
    renderHome();
    expect(cardFor("Connections").getByText("7")).toBeInTheDocument();
  });

  it("restores the default card set when reset", async () => {
    renderHome();

    await userEvent.click(screen.getByRole("button", { name: /Customize/ }));
    await userEvent.click(screen.getByRole("checkbox", { name: "Total sends" }));
    await userEvent.click(
      screen.getByRole("button", { name: "Reset to default" }),
    );
    await userEvent.click(screen.getByRole("button", { name: "Done" }));

    expect(cardFor("Total sends").getByText("12")).toBeInTheDocument();
  });

  it("points an empty feed at logging and finding climbers", () => {
    renderHome();

    expect(screen.getByRole("link", { name: "Log a climb" })).toBeInTheDocument();
    expect(
      screen.getByRole("link", { name: "Find climbers" }),
    ).toBeInTheDocument();
  });

  it("renders feed cards that link to the climber and the climb", () => {
    social.entries = [
      logEntry({
        id: 4,
        climbId: 9,
        user: userSummary({ username: "sam", displayName: "Sam Stone" }),
        likeCount: 3,
        commentCount: 1,
      }),
    ];
    renderHome();

    expect(screen.getByRole("link", { name: "Sam Stone" })).toHaveAttribute(
      "href",
      "/users/sam",
    );
    expect(screen.getByRole("link", { name: "Blue Arete" })).toHaveAttribute(
      "href",
      "/climbs/9",
    );
    expect(
      screen.getByRole("button", { name: "Like this climb" }),
    ).toHaveTextContent("3");
  });

  it("surfaces a feed error", async () => {
    social.feedError = "API unavailable";
    renderHome();

    await waitFor(() =>
      expect(screen.getByText("Error: API unavailable")).toBeInTheDocument(),
    );
  });
});
