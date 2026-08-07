import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import Home from "@/pages/Home";
import Climbs from "@/pages/Climbs";
import Logbook from "@/pages/Logbook";
import { climb, logEntry } from "@/test/fixtures";

const catalog = vi.hoisted(() => ({
  entries: [] as ReturnType<typeof logEntry>[],
  climbs: [] as ReturnType<typeof climb>[],
  logLoading: false,
  logError: null as string | null,
  refetch: vi.fn(),
  deleteClimb: vi.fn(),
}));

vi.mock("@/hooks/catalog-hooks", () => ({
  useLogEntries: () => ({ entries: catalog.entries, loading: catalog.logLoading, error: catalog.logError, refetch: catalog.refetch }),
  useClimbs: () => ({ climbs: catalog.climbs, loading: false, error: null, deleteClimb: catalog.deleteClimb }),
}));

describe("catalog pages", () => {
  beforeEach(() => {
    catalog.entries = [];
    catalog.climbs = [];
    catalog.logLoading = false;
    catalog.logError = null;
  });

  it("summarizes completed climbs, hardest grade, and this month's activity", () => {
    const thisMonth = new Date().toISOString();
    catalog.entries = [
      logEntry({ id: 1, occurredAt: thisMonth, climb: climb({ grade: "V3" }) }),
      logEntry({ id: 2, occurredAt: thisMonth, climb: climb({ grade: "V7" }) }),
      logEntry({ id: 3, occurredAt: thisMonth, status: "Attempted", climb: climb({ grade: "V10" }) }),
    ];

    render(<Home />);

    expect(within(screen.getByText("All time Climbs").closest('[data-slot="card"]')!).getByText("2")).toBeInTheDocument();
    expect(within(screen.getByText("Hardest Grade").closest('[data-slot="card"]')!).getByText("V7")).toBeInTheDocument();
    expect(within(screen.getByText("Climbs This Month").closest('[data-slot="card"]')!).getByText("3")).toBeInTheDocument();
  });

  it("filters the climb catalog using the global search", async () => {
    catalog.climbs = [climb({ id: 1, name: "Blue Arete" }), climb({ id: 2, name: "Red Slab", grade: "V1" })];
    render(<MemoryRouter><Climbs /></MemoryRouter>);

    await userEvent.type(screen.getByPlaceholderText("Search climbs..."), "red");

    expect(screen.getByText("Red Slab")).toBeInTheDocument();
    expect(screen.queryByText("Blue Arete")).not.toBeInTheDocument();
  });

  it("paginates catalogs larger than fifteen climbs", async () => {
    catalog.climbs = Array.from({ length: 16 }, (_, index) => climb({ id: index + 1, name: `Climb ${index + 1}` }));
    render(<MemoryRouter><Climbs /></MemoryRouter>);

    expect(screen.getByText("Climb 1")).toBeInTheDocument();
    expect(screen.queryByText("Climb 16")).not.toBeInTheDocument();
    const pagination = screen.getByText("Page 1 of 2").parentElement!;
    await userEvent.click(within(pagination).getAllByRole("button")[1]!);
    expect(screen.getByText("Climb 16")).toBeInTheDocument();
  });

  it("only deletes a climb after the user confirms", async () => {
    catalog.climbs = [climb()];
    vi.stubGlobal("confirm", vi.fn().mockReturnValue(false));
    const { rerender } = render(<MemoryRouter><Climbs /></MemoryRouter>);

    await userEvent.click(screen.getByRole("button", { name: "Delete" }));
    expect(catalog.deleteClimb).not.toHaveBeenCalled();

    vi.stubGlobal("confirm", vi.fn().mockReturnValue(true));
    rerender(<MemoryRouter><Climbs /></MemoryRouter>);
    await userEvent.click(screen.getByRole("button", { name: "Delete" }));
    expect(catalog.deleteClimb).toHaveBeenCalledWith(1);
  });

  it("shows logbook empty and error states", () => {
    catalog.logError = "API unavailable";
    render(<MemoryRouter><Logbook /></MemoryRouter>);
    expect(screen.getByText("Error: API unavailable")).toBeInTheDocument();
    expect(screen.getByText("No entries yet. Log your first climb!")).toBeInTheDocument();
  });

  it("renders log entries and refreshes after a completed logging flow", async () => {
    catalog.entries = [logEntry({
      climb: climb({ name: "Roadside Arete", place: null, placeId: null, customLocation: {
        name: "Trail Pull-off", latitude: 40, longitude: -75,
      } }),
      place: null,
      placeId: null,
      status: "Attempted",
      rating: null,
    })];
    render(
      <MemoryRouter initialEntries={[{ pathname: "/logbook", state: { refetch: true } }]}>
        <Logbook />
      </MemoryRouter>,
    );

    expect(screen.getByText("Roadside Arete")).toBeInTheDocument();
    expect(screen.getByText("Trail Pull-off")).toBeInTheDocument();
    expect(screen.getByText("Attempted")).toBeInTheDocument();
    await waitFor(() => expect(catalog.refetch).toHaveBeenCalledOnce());
  });
});
