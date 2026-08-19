import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  Search,
  MapPin,
  Star,
  Trash2,
  ChevronLeft,
  ChevronRight,
  BookPlus,
  X,
} from "lucide-react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useClimbs } from "@/hooks/catalog-hooks";
import { SourceBadges } from "@/components/log/ResultBadges";
import type { Climb } from "@/types/catalog";

const PAGE_SIZE = 15;
const ALL_FILTER_VALUE = "all";

type RatingFilter = "all" | "unrated" | "1" | "2" | "3" | "4" | "5";

interface ColumnFilters {
  name: string;
  grade: string;
  setter: string;
  where: string;
  rating: RatingFilter;
  source: string;
}

const DEFAULT_FILTERS: ColumnFilters = {
  name: "",
  grade: ALL_FILTER_VALUE,
  setter: "",
  where: "",
  rating: "all",
  source: ALL_FILTER_VALUE,
};

const SOURCE_LABELS: Record<string, string> = {
  local: "Local",
  openbeta: "OpenBeta",
  openstreetmap: "OpenStreetMap",
};

function whereLabel(climb: Climb): string | null {
  return (
    climb.place?.name ??
    climb.boardConfiguration?.name ??
    climb.customLocation?.name ??
    null
  );
}

function effectiveSources(climb: Climb): string[] {
  return climb.sources.length > 0
    ? climb.sources.map((source) => source.toLowerCase())
    : ["local"];
}

function sourceLabel(source: string): string {
  return SOURCE_LABELS[source.toLowerCase()] ?? source;
}

function includesText(value: string | null | undefined, filter: string) {
  return value?.toLowerCase().includes(filter.trim().toLowerCase()) ?? false;
}

export default function Climbs() {
  const navigate = useNavigate();
  const { climbs, loading, error, deleteClimb } = useClimbs();

  const [query, setQuery] = useState("");
  const [filters, setFilters] = useState<ColumnFilters>(DEFAULT_FILTERS);
  const [page, setPage] = useState(1);

  const updateFilter = <K extends keyof ColumnFilters>(
    key: K,
    value: ColumnFilters[K],
  ) => {
    setFilters((current) => ({ ...current, [key]: value }));
    setPage(1);
  };

  const grades = useMemo(
    () =>
      Array.from(new Set(climbs.map((c) => c.grade))).sort((a, b) => {
        const na = parseInt(a.replace(/\D/g, ""), 10);
        const nb = parseInt(b.replace(/\D/g, ""), 10);
        return na - nb;
      }),
    [climbs],
  );

  const sources = useMemo(
    () =>
      Array.from(new Set(climbs.flatMap((climb) => effectiveSources(climb))))
        .sort((a, b) => sourceLabel(a).localeCompare(sourceLabel(b))),
    [climbs],
  );

  const filtered = useMemo(() => {
    return climbs.filter((c) => {
      const where = whereLabel(c);
      const sources = effectiveSources(c);
      const normalizedQuery = query.trim().toLowerCase();
      const queryFields = [
        c.name,
        c.grade,
        c.setterName,
        where,
        c.averageRating > 0 ? c.averageRating.toFixed(1) : "unrated",
        ...sources.map(sourceLabel),
      ];

      const matchesQuery =
        normalizedQuery.length === 0 ||
        queryFields.some((field) =>
          field?.toLowerCase().includes(normalizedQuery),
        );
      const matchesName = includesText(c.name, filters.name);
      const matchesGrade =
        filters.grade === ALL_FILTER_VALUE || c.grade === filters.grade;
      const matchesSetter =
        filters.setter.trim().length === 0 ||
        includesText(c.setterName ?? "Unassigned", filters.setter);
      const matchesWhere =
        filters.where.trim().length === 0 ||
        includesText(where ?? "Unknown", filters.where);
      const matchesRating =
        filters.rating === "all" ||
        (filters.rating === "unrated"
          ? c.averageRating <= 0
          : c.averageRating >= Number(filters.rating));
      const matchesSource =
        filters.source === ALL_FILTER_VALUE ||
        sources.includes(filters.source.toLowerCase());

      return (
        matchesQuery &&
        matchesName &&
        matchesGrade &&
        matchesSetter &&
        matchesWhere &&
        matchesRating &&
        matchesSource
      );
    });
  }, [climbs, query, filters]);

  const hasActiveFilters =
    query.trim().length > 0 ||
    filters.name.trim().length > 0 ||
    filters.grade !== ALL_FILTER_VALUE ||
    filters.setter.trim().length > 0 ||
    filters.where.trim().length > 0 ||
    filters.rating !== "all" ||
    filters.source !== ALL_FILTER_VALUE;

  const clearFilters = () => {
    setQuery("");
    setFilters(DEFAULT_FILTERS);
    setPage(1);
  };

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const paginated = filtered.slice(
    (currentPage - 1) * PAGE_SIZE,
    currentPage * PAGE_SIZE,
  );

  const handleDelete = async (id: number) => {
    if (!confirm("Delete this climb? This cannot be undone.")) return;
    try {
      await deleteClimb(id);
    } catch (err) {
      alert(err instanceof Error ? err.message : "Failed to delete climb");
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-6xl mx-auto space-y-6">
        <div className="space-y-2">
          <h1 className="text-4xl font-bold text-slate-900 tracking-tight">
            Climbs
          </h1>
          <p className="text-slate-600">Browse the canonical climb catalog</p>
        </div>

        <div className="flex flex-col sm:flex-row gap-3">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
            <Input
              placeholder="Search climbs..."
              value={query}
              onChange={(e) => {
                setQuery(e.target.value);
                setPage(1);
              }}
              className="pl-9"
            />
          </div>
          <Button
            variant="outline"
            disabled={!hasActiveFilters}
            onClick={clearFilters}
            className="gap-2"
          >
            <X className="h-4 w-4" />
            Clear filters
          </Button>
        </div>

        {error && <p className="text-red-600">Error: {error}</p>}

        <div className="overflow-x-auto bg-white rounded-lg border shadow-sm">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Grade</TableHead>
                <TableHead>Setter</TableHead>
                <TableHead>Where</TableHead>
                <TableHead>Rating</TableHead>
                <TableHead>Source</TableHead>
                <TableHead className="w-[120px]">Actions</TableHead>
              </TableRow>
              <TableRow className="bg-slate-50 hover:bg-slate-50">
                <TableHead className="min-w-[180px]">
                  <Input
                    value={filters.name}
                    onChange={(event) =>
                      updateFilter("name", event.target.value)
                    }
                    placeholder="Filter name"
                    className="h-8 bg-white"
                  />
                </TableHead>
                <TableHead className="min-w-[130px]">
                  <Select
                    value={filters.grade}
                    onValueChange={(value) => updateFilter("grade", value)}
                  >
                    <SelectTrigger className="h-8 bg-white">
                      <SelectValue placeholder="Any grade" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value={ALL_FILTER_VALUE}>
                        Any grade
                      </SelectItem>
                      {grades.map((g) => (
                        <SelectItem key={g} value={g}>
                          {g}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </TableHead>
                <TableHead className="min-w-[160px]">
                  <Input
                    value={filters.setter}
                    onChange={(event) =>
                      updateFilter("setter", event.target.value)
                    }
                    placeholder="Filter setter"
                    className="h-8 bg-white"
                  />
                </TableHead>
                <TableHead className="min-w-[180px]">
                  <Input
                    value={filters.where}
                    onChange={(event) =>
                      updateFilter("where", event.target.value)
                    }
                    placeholder="Filter where"
                    className="h-8 bg-white"
                  />
                </TableHead>
                <TableHead className="min-w-[140px]">
                  <Select
                    value={filters.rating}
                    onValueChange={(value) =>
                      updateFilter("rating", value as RatingFilter)
                    }
                  >
                    <SelectTrigger className="h-8 bg-white">
                      <SelectValue placeholder="Any rating" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">Any rating</SelectItem>
                      <SelectItem value="unrated">Unrated</SelectItem>
                      <SelectItem value="5">5+ stars</SelectItem>
                      <SelectItem value="4">4+ stars</SelectItem>
                      <SelectItem value="3">3+ stars</SelectItem>
                      <SelectItem value="2">2+ stars</SelectItem>
                      <SelectItem value="1">1+ stars</SelectItem>
                    </SelectContent>
                  </Select>
                </TableHead>
                <TableHead className="min-w-[160px]">
                  <Select
                    value={filters.source}
                    onValueChange={(value) => updateFilter("source", value)}
                  >
                    <SelectTrigger className="h-8 bg-white">
                      <SelectValue placeholder="Any source" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value={ALL_FILTER_VALUE}>
                        Any source
                      </SelectItem>
                      {sources.map((source) => (
                        <SelectItem key={source} value={source}>
                          {sourceLabel(source)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </TableHead>
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                <TableRow>
                  <TableCell
                    colSpan={7}
                    className="text-center py-8 text-slate-500"
                  >
                    Loading...
                  </TableCell>
                </TableRow>
              ) : paginated.length === 0 ? (
                <TableRow>
                  <TableCell
                    colSpan={7}
                    className="text-center py-8 text-slate-500"
                  >
                    No climbs found.
                  </TableCell>
                </TableRow>
              ) : (
                paginated.map((climb) => {
                  const where = whereLabel(climb);
                  return (
                    <TableRow key={climb.id} className="hover:bg-slate-50">
                      <TableCell className="font-medium">
                        <Link
                          to={`/climbs/${climb.id}`}
                          className="hover:underline"
                        >
                          {climb.name}
                        </Link>
                      </TableCell>
                      <TableCell>
                        <Badge variant="secondary">{climb.grade}</Badge>
                      </TableCell>
                      <TableCell className="text-slate-600">
                        {climb.setterName ?? "-"}
                      </TableCell>
                      <TableCell className="text-slate-600">
                        {where ? (
                          <span className="flex items-center gap-1">
                            <MapPin className="h-4 w-4" />
                            {where}
                          </span>
                        ) : (
                          "-"
                        )}
                      </TableCell>
                      <TableCell>
                        {climb.averageRating > 0 ? (
                          <span className="flex items-center gap-1 text-slate-600">
                            <Star className="h-4 w-4 fill-yellow-400 text-yellow-400" />
                            {climb.averageRating.toFixed(1)}
                          </span>
                        ) : (
                          "-"
                        )}
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-wrap gap-1">
                          <SourceBadges sources={climb.sources} />
                        </div>
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-1">
                          <Button
                            variant="ghost"
                            size="sm"
                            title="Log this climb"
                            onClick={() =>
                              navigate("/log/new", {
                                state: { climbId: climb.id },
                              })
                            }
                          >
                            <BookPlus className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            title="Delete"
                            className="text-red-600 hover:text-red-700 hover:bg-red-50"
                            onClick={() => handleDelete(climb.id)}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  );
                })
              )}
            </TableBody>
          </Table>
        </div>

        {filtered.length > PAGE_SIZE && (
          <div className="flex items-center justify-between bg-white rounded-lg border shadow-sm p-4">
            <p className="text-sm text-slate-600">{filtered.length} climbs</p>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={currentPage === 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <span className="text-sm text-slate-600">
                Page {currentPage} of {totalPages}
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={currentPage === totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              >
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
