import { useEffect, useRef } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { Plus, Star, MapPin } from "lucide-react";
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
import { useLogEntries } from "@/hooks/catalog-hooks";
import type { LogEntry } from "@/types/catalog";

function placeLabel(entry: LogEntry): string | null {
  return (
    entry.place?.name ??
    entry.climb?.place?.name ??
    entry.climb?.boardConfiguration?.name ??
    entry.climb?.customLocation?.name ??
    null
  );
}

export default function Logbook() {
  const navigate = useNavigate();
  const location = useLocation();
  const { entries, loading, error, refetch } = useLogEntries();
  const hasRefetched = useRef(false);

  useEffect(() => {
    if (location.state?.refetch && !hasRefetched.current) {
      refetch();
      hasRefetched.current = true;
      navigate(location.pathname, { replace: true, state: {} });
    }
  }, [location.state, location.pathname, refetch, navigate]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-5xl mx-auto space-y-6">
        <div className="flex items-start justify-between gap-4">
          <div className="space-y-2">
            <h1 className="text-4xl font-bold text-slate-900 tracking-tight">
              Logbook
            </h1>
            <p className="text-slate-600">Your climbing activity</p>
          </div>
          <Button onClick={() => navigate("/log/new")} size="lg">
            <Plus className="mr-2 h-5 w-5" />
            Log a climb
          </Button>
        </div>

        {error && <p className="text-red-600">Error: {error}</p>}

        <div className="bg-white rounded-lg border shadow-sm">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Date</TableHead>
                <TableHead>Climb</TableHead>
                <TableHead>Grade</TableHead>
                <TableHead>Where</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Rating</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                <TableRow>
                  <TableCell
                    colSpan={6}
                    className="text-center py-8 text-slate-500"
                  >
                    Loading...
                  </TableCell>
                </TableRow>
              ) : entries.length === 0 ? (
                <TableRow>
                  <TableCell
                    colSpan={6}
                    className="text-center py-8 text-slate-500"
                  >
                    No entries yet. Log your first climb!
                  </TableCell>
                </TableRow>
              ) : (
                entries.map((entry) => {
                  const where = placeLabel(entry);
                  return (
                    <TableRow key={entry.id} className="hover:bg-slate-50">
                      <TableCell className="text-slate-600">
                        {new Date(entry.occurredAt).toLocaleDateString()}
                      </TableCell>
                      <TableCell className="font-medium">
                        {entry.climb?.name ?? "Unknown climb"}
                      </TableCell>
                      <TableCell>
                        {entry.climb?.grade ? (
                          <Badge variant="secondary">{entry.climb.grade}</Badge>
                        ) : (
                          "-"
                        )}
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
                        <Badge
                          variant={
                            entry.status === "Completed" ? "default" : "outline"
                          }
                        >
                          {entry.status}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        {entry.rating ? (
                          <span className="flex items-center gap-1 text-slate-600">
                            <Star className="h-4 w-4 fill-yellow-400 text-yellow-400" />
                            {entry.rating}
                          </span>
                        ) : (
                          "-"
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })
              )}
            </TableBody>
          </Table>
        </div>
      </div>
    </div>
  );
}
