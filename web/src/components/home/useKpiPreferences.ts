import { useCallback, useState } from "react";
import { DEFAULT_KPI_IDS, KPI_CATALOG } from "@/components/home/kpi-catalog";

const STORAGE_PREFIX = "climbing-logbook:home-kpis";

function storageKey(userId: number | undefined) {
  return `${STORAGE_PREFIX}:${userId ?? "anon"}`;
}

function readStored(key: string): string[] | null {
  try {
    const raw = window.localStorage.getItem(key);
    if (!raw) {
      return null;
    }

    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) {
      return null;
    }

    // Drop ids from KPIs that no longer exist so a stale preference can't blank the row.
    const known = new Set(KPI_CATALOG.map((kpi) => kpi.id));
    return parsed.filter(
      (id): id is string => typeof id === "string" && known.has(id),
    );
  } catch {
    return null;
  }
}

export function useKpiPreferences(userId: number | undefined) {
  const key = storageKey(userId);
  const [state, setState] = useState(() => ({
    key,
    ids: readStored(key) ?? DEFAULT_KPI_IDS,
  }));

  // Deliberate setState during render: the documented way to reset state when the
  // key changes, which happens once the signed-in user resolves.
  if (state.key !== key) {
    setState({ key, ids: readStored(key) ?? DEFAULT_KPI_IDS });
  }

  const selectedIds = state.key === key ? state.ids : DEFAULT_KPI_IDS;

  const persist = useCallback(
    (ids: string[]) => {
      setState({ key, ids });
      try {
        window.localStorage.setItem(key, JSON.stringify(ids));
      } catch {
        // A full or unavailable localStorage should not break the page.
      }
    },
    [key],
  );

  const toggle = useCallback(
    (id: string) => {
      persist(
        selectedIds.includes(id)
          ? selectedIds.filter((current) => current !== id)
          : // Keep catalog order so cards do not jump around as they are toggled.
            KPI_CATALOG.filter(
              (kpi) => kpi.id === id || selectedIds.includes(kpi.id),
            ).map((kpi) => kpi.id),
      );
    },
    [persist, selectedIds],
  );

  const reset = useCallback(() => persist(DEFAULT_KPI_IDS), [persist]);

  return { selectedIds, toggle, reset };
}
