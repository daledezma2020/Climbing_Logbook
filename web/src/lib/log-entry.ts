import type { LogEntry } from "@/types/catalog";

export function placeLabel(entry: LogEntry): string | null {
  return (
    entry.place?.name ??
    entry.climb?.place?.name ??
    entry.climb?.boardConfiguration?.name ??
    entry.climb?.customLocation?.name ??
    null
  );
}
