import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import LocationPicker from "@/components/log/LocationPicker";

vi.mock("leaflet", () => ({ default: { divIcon: vi.fn(() => ({})) } }));
vi.mock("react-leaflet", () => ({
  MapContainer: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  TileLayer: () => null,
  Marker: () => <div>Selected marker</div>,
  useMap: () => ({ flyTo: vi.fn(), invalidateSize: vi.fn() }),
  useMapEvents: () => null,
}));

describe("LocationPicker", () => {
  beforeEach(() => vi.stubGlobal("fetch", vi.fn()));

  it("searches for a place and returns its name and coordinates", async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(JSON.stringify([{
      place_id: 1,
      display_name: "Ralph Stover State Park, Pennsylvania, USA",
      lat: "40.4400",
      lon: "-75.1000",
    }]), { status: 200 }));
    const onChange = vi.fn();
    const onPlaceSelected = vi.fn();
    render(<LocationPicker value={null} onChange={onChange} onPlaceSelected={onPlaceSelected} />);

    await userEvent.type(screen.getByPlaceholderText("Search for a map location..."), "Ralph Stover");
    await userEvent.click(screen.getByRole("button", { name: "Search" }));
    await userEvent.click(await screen.findByText("Ralph Stover State Park, Pennsylvania, USA"));

    expect(onChange).toHaveBeenCalledWith({ lat: 40.44, lng: -75.1 });
    expect(onPlaceSelected).toHaveBeenCalledWith("Ralph Stover State Park");
  });

  it("explains that manual map selection remains available after search failure", async () => {
    vi.mocked(fetch).mockResolvedValue(new Response(null, { status: 503 }));
    render(<LocationPicker value={null} onChange={vi.fn()} />);

    await userEvent.type(screen.getByPlaceholderText("Search for a map location..."), "Unavailable");
    await userEvent.click(screen.getByRole("button", { name: "Search" }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "You can still click the map manually to place the pin.",
    );
  });
});
