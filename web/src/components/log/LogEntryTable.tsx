import { Star, MapPin } from "lucide-react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import type { LogEntry } from "@/types/catalog";

interface LogEntryTableProps {
  entries: LogEntry[];
  loading: boolean;
  emptyMessage: string;
}

function placeLabel(entry: LogEntry): string | null {
  return (
    entry.place?.name ??
    entry.climb?.place?.name ??
    entry.climb?.boardConfiguration?.name ??
    entry.climb?.customLocation?.name ??
    null
  );
}

export default function LogEntryTable({
  entries,
  loading,
  emptyMessage,
}: LogEntryTableProps) {
  return (
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
              <TableCell colSpan={6} className="text-center py-8 text-slate-500">
                Loading...
              </TableCell>
            </TableRow>
          ) : entries.length === 0 ? (
            <TableRow>
              <TableCell colSpan={6} className="text-center py-8 text-slate-500">
                {emptyMessage}
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
  );
}
