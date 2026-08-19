import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import LogClimb from "@/pages/LogClimb";
import { apiFetch } from "@/lib/api";
import { climb } from "@/test/fixtures";

const auth = vi.hoisted(() => ({
  getAccessTokenSilently: vi.fn(),
  loginWithRedirect: vi.fn(),
  isAuthenticated: true,
  isLoading: false,
}));
const searchState = vi.hoisted(() => ({
  results: null as null | {
    results: Array<Record<string, unknown>>;
    providers: Record<string, "complete" | "failed" | "skipped">;
  },
  loading: false,
  error: null as string | null,
  search: vi.fn(),
}));

vi.mock("@auth0/auth0-react", () => ({ useAuth0: () => auth }));
vi.mock("@/lib/api", () => ({ apiFetch: vi.fn() }));
vi.mock("@/hooks/catalog-hooks", () => ({
  useSearch: () => searchState,
  importOpenBetaClimb: vi.fn(),
}));
vi.mock("@/components/log/LogEntryForm", () => ({
  default: ({ climb: selected }: { climb: { name: string } }) => <div>Log form for {selected.name}</div>,
}));
vi.mock("@/components/log/ManualClimbForm", () => ({
  default: () => <div>Manual climb form</div>,
}));

describe("LogClimb workflow", () => {
  beforeEach(() => {
    auth.isAuthenticated = true;
    auth.isLoading = false;
    searchState.results = null;
    searchState.loading = false;
    searchState.error = null;
  });

  it("requires visitors to sign in before logging", async () => {
    auth.isAuthenticated = false;
    render(<MemoryRouter><LogClimb /></MemoryRouter>);

    await userEvent.click(screen.getByRole("button", { name: "Sign in to log a climb" }));
    expect(auth.loginWithRedirect).toHaveBeenCalledOnce();
  });

  it("loads a local search result into the log-entry step", async () => {
    searchState.results = {
      providers: { local: "complete" },
      results: [{
        key: "local:climb:1",
        resultType: "climb",
        name: "Blue Arete",
        sources: ["local"],
        grade: { system: "vscale", value: "V4" },
        placeName: "Test Gym",
        localId: 1,
      }],
    };
    vi.mocked(apiFetch).mockResolvedValue(climb());
    render(<MemoryRouter><LogClimb /></MemoryRouter>);

    await userEvent.click(screen.getByRole("button", { name: "Log this" }));

    expect(await screen.findByText("Log form for Blue Arete")).toBeInTheDocument();
    expect(apiFetch).toHaveBeenCalledWith("/climbs/1");
    expect(screen.getByText("Step 2 of 2: record your attempt")).toBeInTheDocument();
  });

  it("opens the manual first step for a new climb", async () => {
    render(<MemoryRouter><LogClimb /></MemoryRouter>);

    await userEvent.click(screen.getByRole("button", { name: "Add new climb" }));

    expect(screen.getByText("Manual climb form")).toBeInTheDocument();
    expect(screen.getByText("Step 1 of 2: create the climb, then you will log it")).toBeInTheDocument();
  });
});
