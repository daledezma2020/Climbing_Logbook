import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import Users from "@/pages/Users";
import { userSummary } from "@/test/fixtures";

const auth = vi.hoisted(() => ({
  isAuthenticated: true,
  loginWithRedirect: vi.fn(),
  getAccessTokenSilently: vi.fn(),
}));

const search = vi.hoisted(() => ({
  results: [] as ReturnType<typeof userSummary>[],
  loading: false,
  error: null as string | null,
  follow: vi.fn(),
  unfollow: vi.fn(),
}));

vi.mock("@auth0/auth0-react", () => ({ useAuth0: () => auth }));
vi.mock("@/hooks/user-hooks", () => ({
  useUserSearch: () => ({
    results: search.results,
    loading: search.loading,
    error: search.error,
  }),
  useFollow: () => ({ follow: search.follow, unfollow: search.unfollow }),
}));

function renderPage() {
  return render(
    <MemoryRouter>
      <Users />
    </MemoryRouter>,
  );
}

describe("Users search page", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    auth.isAuthenticated = true;
    search.results = [];
    search.loading = false;
    search.error = null;
    search.follow.mockResolvedValue(undefined);
  });

  it("prompts for a query before anything has been typed", () => {
    renderPage();

    expect(screen.getByText("Start typing to find climbers.")).toBeInTheDocument();
  });

  it("renders a card per match with its handle and follower count", async () => {
    search.results = [
      userSummary({ id: 9, username: "sam", displayName: "Sam Boulders", followerCount: 3 }),
    ];
    renderPage();

    await userEvent.type(screen.getByLabelText("Search climbers"), "sam");

    expect(screen.getByText("Sam Boulders")).toBeInTheDocument();
    expect(screen.getByText("@sam")).toBeInTheDocument();
    expect(screen.getByText("3 followers")).toBeInTheDocument();
  });

  it("reports when nothing matches", async () => {
    renderPage();

    await userEvent.type(screen.getByLabelText("Search climbers"), "zzz");

    expect(screen.getByText('No climbers match "zzz".')).toBeInTheDocument();
  });

  it("sends signed-out visitors to login instead of following", async () => {
    auth.isAuthenticated = false;
    search.results = [userSummary({ id: 9, username: "sam" })];
    renderPage();

    await userEvent.type(screen.getByLabelText("Search climbers"), "sam");
    await userEvent.click(screen.getByRole("button", { name: /follow/i }));

    expect(auth.loginWithRedirect).toHaveBeenCalled();
    expect(search.follow).not.toHaveBeenCalled();
  });

  it("rolls the button back and surfaces the error when following fails", async () => {
    search.results = [userSummary({ id: 9, username: "sam" })];
    search.follow.mockRejectedValue(new Error("Follow API failed"));
    renderPage();

    await userEvent.type(screen.getByLabelText("Search climbers"), "sam");
    await userEvent.click(screen.getByRole("button", { name: /follow/i }));

    await waitFor(() =>
      expect(screen.getByText("Follow API failed")).toBeInTheDocument(),
    );
    expect(screen.getByRole("button", { name: /^follow$/i })).toBeInTheDocument();
  });

  it("shows an optimistic Following state on success", async () => {
    search.results = [userSummary({ id: 9, username: "sam" })];
    renderPage();

    await userEvent.type(screen.getByLabelText("Search climbers"), "sam");
    await userEvent.click(screen.getByRole("button", { name: /follow/i }));

    await waitFor(() =>
      expect(screen.getByRole("button", { name: /following/i })).toBeInTheDocument(),
    );
    expect(search.follow).toHaveBeenCalledWith("sam");
  });
});
