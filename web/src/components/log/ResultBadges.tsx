import { Badge } from "@/components/ui/badge";

const SOURCE_LABELS: Record<string, string> = {
  local: "Local",
  openbeta: "OpenBeta",
  osm: "OpenStreetMap",
  openstreetmap: "OpenStreetMap",
  moonboardseed: "MoonBoard",
};

function sourceLabel(source: string): string {
  return SOURCE_LABELS[source.toLowerCase()] ?? source;
}

export function SourceBadges({ sources }: { sources: string[] }) {
  if (!sources.length) return null;
  return (
    <>
      {sources.map((source) => (
        <Badge key={source} variant="outline">
          {sourceLabel(source)}
        </Badge>
      ))}
    </>
  );
}

export function TypeBadge({
  resultType,
  placeKind,
}: {
  resultType: "climb" | "place";
  placeKind?: string | null;
}) {
  if (resultType === "place") {
    return (
      <Badge variant="secondary">
        {placeKind && placeKind.toLowerCase() === "gym" ? "Gym" : "Place"}
      </Badge>
    );
  }
  return <Badge variant="default">Climb</Badge>;
}
