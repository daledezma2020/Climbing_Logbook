import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ClimbDetail from "@/pages/ClimbDetail";
import { climb, comment, logEntry } from "@/test/fixtures";

const auth = vi.hoisted(() => ({
  isAuthenticated: true,
  loginWithRedirect: vi.fn(),
  getAccessTokenSilently: vi.fn().mockResolvedValue("token"),
}));

const catalog = vi.hoisted(() => ({
  climb: null as ReturnType<typeof climb> | null,
  loading: false,
  error: null as string | null,
  notFound: false,
  entries: [] as ReturnType<typeof logEntry>[],
}));

const social = vi.hoisted(() => ({
  comments: [] as ReturnType<typeof comment>[],
  addComment: vi.fn(),
  lastTarget: null as unknown,
}));

vi.mock("@auth0/auth0-react", () => ({ useAuth0: () => auth }));

vi.mock("@/hooks/catalog-hooks", () => ({
  useClimb: () => ({
    climb: catalog.climb,
    loading: catalog.loading,
    error: catalog.error,
    notFound: catalog.notFound,
  }),
  useClimbLogEntries: () => ({
    entries: catalog.entries,
    total: catalog.entries.length,
    loading: false,
    loadingMore: false,
    error: null,
    hasMore: false,
    loadMore: vi.fn(),
  }),
}));

vi.mock("@/hooks/social-hooks", () => ({
  useComments: (target: unknown) => {
    social.lastTarget = target;
    return {
      comments: social.comments,
      total: social.comments.length,
      loading: false,
      loadingMore: false,
      error: null,
      hasMore: false,
      loadMore: vi.fn(),
      addComment: social.addComment,
      deleteComment: vi.fn(),
    };
  },
}));

function renderPage(id = "1") {
  return render(
    <MemoryRouter initialEntries={[`/climbs/${id}`]}>
      <Routes>
        <Route path="/climbs/:id" element={<ClimbDetail />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("ClimbDetail", () => {
  beforeEach(() => {
    catalog.climb = climb({ id: 1, name: "Blue Arete", grade: "V4" });
    catalog.loading = false;
    catalog.error = null;
    catalog.notFound = false;
    catalog.entries = [];
    social.comments = [];
    social.addComment = vi.fn().mockResolvedValue(comment());
  });

  it("shows the climb header with its grade, discipline, and place", () => {
    renderPage();

    expect(
      screen.getByRole("heading", { name: "Blue Arete" }),
    ).toBeInTheDocument();
    expect(screen.getByText("V4")).toBeInTheDocument();
    expect(screen.getByText("Bouldering")).toBeInTheDocument();
    expect(screen.getByText("Test Gym")).toBeInTheDocument();
  });

  it("explains when the climb does not exist", () => {
    catalog.notFound = true;
    renderPage("404");

    expect(
      screen.getByRole("heading", { name: "No climb found" }),
    ).toBeInTheDocument();
  });

  it("lists the ascents logged against the climb", () => {
    catalog.entries = [logEntry({ id: 2, status: "Completed" })];
    renderPage();

    expect(screen.getByText("Ascents (1)")).toBeInTheDocument();
    expect(screen.getByText("Completed")).toBeInTheDocument();
  });

  it("says so when nobody has logged the climb", () => {
    renderPage();
    expect(
      screen.getByText("Nobody has logged this climb yet."),
    ).toBeInTheDocument();
  });

  it("threads comments against the climb rather than a log entry", async () => {
    renderPage();

    expect(social.lastTarget).toEqual({ kind: "climb", id: 1 });

    await userEvent.type(screen.getByLabelText("Add a comment"), "sandbagged");
    await userEvent.click(screen.getByRole("button", { name: "Post" }));

    expect(social.addComment).toHaveBeenCalledWith({ content: "sandbagged" });
  });

  it("offers a shortcut to log the climb", () => {
    renderPage();
    expect(
      screen.getByRole("link", { name: /Log this climb/ }),
    ).toHaveAttribute("href", "/log/new");
  });
});
