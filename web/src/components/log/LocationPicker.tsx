import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import {
  MapContainer,
  TileLayer,
  Marker,
  useMap,
  useMapEvents,
} from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { Loader2, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import ErrorToast from "@/components/log/ErrorToast";

export interface LatLng {
  lat: number;
  lng: number;
}

interface LocationPickerProps {
  value: LatLng | null;
  onChange: (position: LatLng) => void;
  onPlaceSelected?: (name: string) => void;
  mapClassName?: string;
}

interface OSMSearchResult {
  place_id: number;
  display_name: string;
  lat: string;
  lon: string;
}

function ClickHandler({ onChange }: { onChange: (position: LatLng) => void }) {
  useMapEvents({
    click(e) {
      onChange({ lat: e.latlng.lat, lng: e.latlng.lng });
    },
  });
  return null;
}

function RecenterMap({ value }: { value: LatLng | null }) {
  const map = useMap();

  useEffect(() => {
    if (value) {
      map.flyTo([value.lat, value.lng], 14, { duration: 0.5 });
    }
  }, [map, value]);

  return null;
}

function InvalidateSizeOnMount() {
  const map = useMap();

  useEffect(() => {
    window.setTimeout(() => map.invalidateSize(), 0);
  }, [map]);

  return null;
}

const US_CENTER: LatLng = { lat: 39.8283, lng: -98.5795 };

const pinIcon = L.divIcon({
  className: "",
  iconSize: [32, 42],
  iconAnchor: [16, 42],
  popupAnchor: [0, -38],
  html: `
    <div style="
      width: 32px;
      height: 32px;
      transform: rotate(45deg);
      border-radius: 50% 50% 50% 4px;
      background: #0f172a;
      border: 3px solid #ffffff;
      box-shadow: 0 8px 18px rgba(15, 23, 42, 0.28);
    ">
      <div style="
        width: 10px;
        height: 10px;
        margin: 8px;
        border-radius: 9999px;
        background: #ffffff;
      "></div>
    </div>
  `,
});

export default function LocationPicker({
  value,
  onChange,
  onPlaceSelected,
  mapClassName = "h-64",
}: LocationPickerProps) {
  const center = value ?? US_CENTER;
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<OSMSearchResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const searchPlaces = async (event: FormEvent) => {
    event.preventDefault();
    const trimmed = query.trim();
    if (!trimmed) return;

    try {
      setLoading(true);
      setError(null);
      const params = new URLSearchParams({
        format: "jsonv2",
        q: trimmed,
        limit: "5",
        addressdetails: "1",
      });
      const response = await fetch(
        `https://nominatim.openstreetmap.org/search?${params}`,
      );
      if (!response.ok) {
        throw new Error(
          `OpenStreetMap returned HTTP ${response.status} while searching for "${trimmed}".`,
        );
      }
      setResults((await response.json()) as OSMSearchResult[]);
    } catch (err) {
      setError(
        err instanceof Error
          ? `${err.message} You can try a broader search like a city name, or click the map manually to place the pin.`
          : "OpenStreetMap search failed. You can still click the map manually to place the pin.",
      );
      setResults([]);
    } finally {
      setLoading(false);
    }
  };

  const selectResult = (result: OSMSearchResult) => {
    onChange({ lat: Number(result.lat), lng: Number(result.lon) });
    onPlaceSelected?.(
      result.display_name.split(",")[0]?.trim() || result.display_name,
    );
    setQuery(result.display_name);
    setResults([]);
  };

  return (
    <div className="space-y-3">
      {error && (
        <ErrorToast
          title="Location search failed"
          message={error}
          onDismiss={() => setError(null)}
        />
      )}

      <form onSubmit={searchPlaces} className="flex gap-2">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <Input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search OpenStreetMap..."
            className="pl-9"
          />
        </div>
        <Button
          type="submit"
          variant="outline"
          disabled={loading || !query.trim()}
        >
          {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : "Search"}
        </Button>
      </form>

      {results.length > 0 && (
        <div className="rounded-md border bg-white text-sm shadow-sm">
          {results.map((result) => (
            <button
              key={result.place_id}
              type="button"
              className="block w-full border-b px-3 py-2 text-left last:border-b-0 hover:bg-slate-50"
              onClick={() => selectResult(result)}
            >
              {result.display_name}
            </button>
          ))}
        </div>
      )}

      <div
        className={`relative z-0 isolate w-full overflow-hidden rounded-md border ${mapClassName}`}
      >
        <MapContainer
          center={[center.lat, center.lng]}
          zoom={value ? 13 : 4}
          scrollWheelZoom
          className="h-full w-full"
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <ClickHandler onChange={onChange} />
          <InvalidateSizeOnMount />
          <RecenterMap value={value} />
          {value && <Marker icon={pinIcon} position={[value.lat, value.lng]} />}
        </MapContainer>
      </div>
    </div>
  );
}
