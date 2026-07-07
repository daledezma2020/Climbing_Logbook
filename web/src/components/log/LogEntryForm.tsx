import { useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { Star } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Combobox } from "@/components/ui/combobox";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { createLogEntry } from "@/hooks/catalog-hooks";
import { usePlaces } from "@/hooks/catalog-hooks";
import ErrorToast from "@/components/log/ErrorToast";
import type { Climb, LogEntryStatus } from "@/types/catalog";

interface LogEntryFormProps {
  climb: Climb;
  onSubmitted: () => void;
  onCancel: () => void;
}

function todayIso() {
  return new Date().toISOString().slice(0, 10);
}

export default function LogEntryForm({
  climb,
  onSubmitted,
  onCancel,
}: LogEntryFormProps) {
  const { getAccessTokenSilently } = useAuth0();
  const isBoardClimb = climb.boardConfigurationId != null;
  const { places } = usePlaces();

  const [status, setStatus] = useState<LogEntryStatus>("Completed");
  const [occurredAt, setOccurredAt] = useState(todayIso());
  const [rating, setRating] = useState(0);
  const [notes, setNotes] = useState("");
  const [placeId, setPlaceId] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const placeOptions = places.map((p) => ({
    value: String(p.id),
    label: p.name,
  }));

  const handleSubmit = async () => {
    try {
      setSubmitting(true);
      setError(null);
      const accessToken = await getAccessTokenSilently();
      await createLogEntry(
        {
          climbId: climb.id,
          placeId: isBoardClimb && placeId ? Number(placeId) : null,
          occurredAt: new Date(occurredAt).toISOString(),
          status,
          rating: rating > 0 ? rating : null,
          notes: notes.trim() || null,
        },
        accessToken,
      );
      onSubmitted();
    } catch (err) {
      setError(
        err instanceof Error
          ? `The API could not save this log entry. ${err.message}`
          : "The API could not save this log entry. Check the climb and location details, then try again.",
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-5">
      {error && (
        <ErrorToast
          title="Could not save log entry"
          message={error}
          onDismiss={() => setError(null)}
        />
      )}

      <div className="rounded-lg border bg-slate-50 p-4">
        <p className="font-semibold text-slate-900">{climb.name}</p>
        <p className="text-sm text-slate-600">
          {climb.grade}
          {climb.boardConfiguration
            ? ` · ${climb.boardConfiguration.name}`
            : climb.place
              ? ` · ${climb.place.name}`
              : climb.customLocation
                ? ` · ${climb.customLocation.name}`
                : ""}
          {climb.setterName ? ` · set by ${climb.setterName}` : ""}
        </p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700">Status</label>
          <Select
            value={status}
            onValueChange={(v) => setStatus(v as LogEntryStatus)}
          >
            <SelectTrigger className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Completed">Completed</SelectItem>
              <SelectItem value="Attempted">Attempted</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700">Date</label>
          <Input
            type="date"
            value={occurredAt}
            max={todayIso()}
            onChange={(e) => setOccurredAt(e.target.value)}
          />
        </div>
      </div>

      {isBoardClimb && (
        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700">
            Where (optional)
          </label>
          <Combobox
            options={placeOptions}
            value={placeId}
            onValueChange={setPlaceId}
            placeholder="Gym or place you climbed it"
            searchPlaceholder="Search places..."
            emptyMessage="No place found."
          />
        </div>
      )}

      <div className="space-y-2">
        <label className="text-sm font-medium text-slate-700">
          Rating (optional)
        </label>
        <div className="flex items-center gap-1">
          {[1, 2, 3, 4, 5].map((star) => (
            <button
              key={star}
              type="button"
              onClick={() => setRating(star === rating ? 0 : star)}
              className="p-0.5"
            >
              <Star
                className={`h-6 w-6 ${
                  star <= rating
                    ? "fill-yellow-400 text-yellow-400"
                    : "text-slate-300"
                }`}
              />
            </button>
          ))}
        </div>
      </div>

      <div className="space-y-2">
        <label className="text-sm font-medium text-slate-700">
          Notes (optional)
        </label>
        <Textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="How did it go?"
        />
      </div>

      <div className="flex justify-end gap-2">
        <Button variant="outline" onClick={onCancel} disabled={submitting}>
          Back
        </Button>
        <Button onClick={handleSubmit} disabled={submitting}>
          {submitting ? "Saving..." : "Save to logbook"}
        </Button>
      </div>
    </div>
  );
}
