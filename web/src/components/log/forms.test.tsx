import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ManualClimbForm from "@/components/log/ManualClimbForm";
import LogEntryForm from "@/components/log/LogEntryForm";
import { createLogEntry, createManualClimb } from "@/hooks/catalog-hooks";
import { climb, testPlace } from "@/test/fixtures";

const auth = vi.hoisted(() => ({ getAccessTokenSilently: vi.fn() }));

vi.mock("@auth0/auth0-react", () => ({ useAuth0: () => auth }));
vi.mock("@/hooks/catalog-hooks", async (importOriginal) => {
  const original = await importOriginal<typeof import("@/hooks/catalog-hooks")>();
  return {
    ...original,
    createManualClimb: vi.fn(),
    createLogEntry: vi.fn(),
    usePlaces: () => ({ places: [testPlace], loading: false, refetch: vi.fn() }),
    useBoardConfigurations: () => ({ boards: [], loading: false }),
  };
});
vi.mock("@/hooks/setter-hooks", () => ({
  useSetters: () => ({ setters: [], loading: false, error: null }),
}));
vi.mock("@/components/log/LocationPicker", () => ({ default: () => <div>Map picker</div> }));

describe("climb entry forms", () => {
  beforeEach(() => auth.getAccessTokenSilently.mockResolvedValue("access-token"));

  it("creates a manual climb at the preselected place", async () => {
    const created = climb();
    vi.mocked(createManualClimb).mockResolvedValue(created);
    const onCreated = vi.fn();
    render(
      <ManualClimbForm
        initialName="  Blue Arete  "
        initialPlaceId={testPlace.id}
        onCreated={onCreated}
        onCancel={vi.fn()}
      />,
    );

    await userEvent.click(screen.getByRole("button", { name: "Create & continue" }));

    await waitFor(() => expect(createManualClimb).toHaveBeenCalledWith({
      name: "Blue Arete",
      discipline: "Bouldering",
      gradeSystem: "VScale",
      grade: "V0",
      placeId: testPlace.id,
    }, "access-token"));
    expect(onCreated).toHaveBeenCalledWith(created);
  });

  it("shows actionable validation instead of submitting an incomplete climb", async () => {
    render(<ManualClimbForm onCreated={vi.fn()} onCancel={vi.fn()} />);

    await userEvent.click(screen.getByRole("button", { name: "Create & continue" }));

    expect(screen.getByRole("alert")).toHaveTextContent("Enter a climb name before continuing.");
    expect(createManualClimb).not.toHaveBeenCalled();
  });

  it("keeps manual climb details visible when creation fails", async () => {
    vi.mocked(createManualClimb).mockRejectedValue(new Error("Duplicate climb"));
    render(
      <ManualClimbForm
        initialName="Blue Arete"
        initialPlaceId={testPlace.id}
        onCreated={vi.fn()}
        onCancel={vi.fn()}
      />,
    );

    await userEvent.click(screen.getByRole("button", { name: "Create & continue" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "The API could not create this climb. Duplicate climb",
    );
    expect(screen.getByDisplayValue("Blue Arete")).toBeInTheDocument();
  });

  it("saves a completed log entry with empty optional fields omitted", async () => {
    vi.mocked(createLogEntry).mockResolvedValue({} as never);
    const onSubmitted = vi.fn();
    render(<LogEntryForm climb={climb()} onSubmitted={onSubmitted} onCancel={vi.fn()} />);

    await userEvent.click(screen.getByRole("button", { name: "Save to logbook" }));

    await waitFor(() => expect(createLogEntry).toHaveBeenCalledWith(
      expect.objectContaining({
        climbId: 1,
        placeId: null,
        status: "Completed",
        rating: null,
        notes: null,
      }),
      "access-token",
    ));
    expect(onSubmitted).toHaveBeenCalledOnce();
  });

  it("keeps the form open and explains API failures", async () => {
    vi.mocked(createLogEntry).mockRejectedValue(new Error("Request failed."));
    const onSubmitted = vi.fn();
    render(<LogEntryForm climb={climb()} onSubmitted={onSubmitted} onCancel={vi.fn()} />);

    await userEvent.click(screen.getByRole("button", { name: "Save to logbook" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "The API could not save this log entry. Request failed.",
    );
    expect(onSubmitted).not.toHaveBeenCalled();
  });
});
