import { useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Combobox } from "@/components/ui/combobox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  createManualClimb,
  useBoardConfigurations,
  usePlaces,
} from "@/hooks/catalog-hooks";
import { useSetters } from "@/hooks/setter-hooks";
import ErrorToast from "@/components/log/ErrorToast";
import LocationPicker from "@/components/log/LocationPicker";
import type {
  Climb,
  CreateManualClimbInput,
  CustomLocation,
} from "@/types/catalog";

interface ManualClimbFormProps {
  onCreated: (climb: Climb) => void;
  onCancel: () => void;
  initialName?: string;
  initialPlaceId?: number | null;
}

type ContextMode = "existing-place" | "board" | "custom-location";
type CustomLocationDraft = {
  name: string;
  latitude: number | null;
  longitude: number | null;
};

const V_GRADES = Array.from({ length: 18 }, (_, i) => `V${i}`);
const emptyCustomLocationDraft: CustomLocationDraft = {
  name: "",
  latitude: null,
  longitude: null,
};

export default function ManualClimbForm({
  onCreated,
  onCancel,
  initialName = "",
  initialPlaceId = null,
}: ManualClimbFormProps) {
  const { getAccessTokenSilently } = useAuth0();
  const { places } = usePlaces();
  const { boards } = useBoardConfigurations();
  const { setters } = useSetters();

  const [name, setName] = useState(initialName);
  const [grade, setGrade] = useState("V0");
  const [mode, setMode] = useState<ContextMode>(
    initialPlaceId ? "existing-place" : "existing-place",
  );
  const [placeId, setPlaceId] = useState(
    initialPlaceId ? String(initialPlaceId) : "",
  );
  const [boardId, setBoardId] = useState("");
  const [setterId, setSetterId] = useState("");

  const [customLocation, setCustomLocation] = useState<CustomLocation | null>(
    null,
  );
  const [customLocationDraft, setCustomLocationDraft] =
    useState<CustomLocationDraft>(emptyCustomLocationDraft);
  const [customLocationDialogOpen, setCustomLocationDialogOpen] =
    useState(false);

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const placeOptions = places.map((p) => ({
    value: String(p.id),
    label: p.name,
  }));
  const boardOptions = boards.map((b) => ({
    value: String(b.id),
    label: b.name,
  }));
  const setterOptions = setters.map((s) => ({
    value: String(s.id),
    label: s.name,
  }));

  const validate = (): string | null => {
    if (!name.trim()) return "Enter a climb name before continuing.";
    if (!grade) return "Choose a grade before continuing.";
    if (mode === "existing-place" && !placeId) {
      return "Select an existing place, or switch Where to Board configuration or Custom Location.";
    }
    if (mode === "board" && !boardId) {
      return "Select an existing board configuration or type a custom board name.";
    }
    if (mode === "custom-location") {
      if (!customLocation?.name.trim()) {
        return "Choose a custom location, search OpenStreetMap or drop a pin, then click Use this location.";
      }
    }
    return null;
  };

  const openCustomLocationDialog = () => {
    setCustomLocationDraft(customLocation ?? emptyCustomLocationDraft);
    setCustomLocationDialogOpen(true);
  };

  const confirmCustomLocation = () => {
    if (
      !customLocationDraft.name.trim() ||
      customLocationDraft.latitude == null ||
      customLocationDraft.longitude == null
    ) {
      return;
    }
    setCustomLocation({
      name: customLocationDraft.name.trim(),
      latitude: customLocationDraft.latitude,
      longitude: customLocationDraft.longitude,
    });
    setCustomLocationDialogOpen(false);
  };

  const handleSubmit = async () => {
    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }

    const payload: CreateManualClimbInput = {
      name: name.trim(),
      discipline: "Bouldering",
      gradeSystem: "VScale",
      grade,
    };

    if (setterId) {
      if (setterOptions.some((o) => o.value === setterId)) {
        payload.setterId = Number(setterId);
      } else {
        payload.setterName = setterId;
      }
    }

    if (mode === "existing-place") {
      payload.placeId = Number(placeId);
    } else if (mode === "board") {
      if (boardOptions.some((o) => o.value === boardId)) {
        payload.boardConfigurationId = Number(boardId);
      } else {
        payload.boardConfigurationName = boardId;
      }
    } else {
      payload.customLocation = customLocation;
    }

    try {
      setSubmitting(true);
      setError(null);
      const accessToken = await getAccessTokenSilently();
      const climb = await createManualClimb(payload, accessToken);
      onCreated(climb);
    } catch (err) {
      setError(
        err instanceof Error
          ? `The API could not create this climb. ${err.message}`
          : "The API could not create this climb. Check the selected location and try again.",
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-5">
      {error && (
        <ErrorToast
          title="Could not save climb"
          message={error}
          onDismiss={() => setError(null)}
        />
      )}

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700">Name</label>
          <Input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Climb name"
          />
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium text-slate-700">Grade</label>
          <Select value={grade} onValueChange={setGrade}>
            <SelectTrigger className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {V_GRADES.map((g) => (
                <SelectItem key={g} value={g}>
                  {g}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="space-y-2">
        <label className="text-sm font-medium text-slate-700">Where</label>
        <Select value={mode} onValueChange={(v) => setMode(v as ContextMode)}>
          <SelectTrigger className="w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="existing-place">Existing place</SelectItem>
            <SelectItem value="board">Board configuration</SelectItem>
            <SelectItem value="custom-location">Custom Location</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {mode === "existing-place" && (
        <Combobox
          options={placeOptions}
          value={placeId}
          onValueChange={setPlaceId}
          placeholder="Select a place"
          searchPlaceholder="Search places..."
          emptyMessage="No place found."
        />
      )}

      {mode === "board" && (
        <Combobox
          options={boardOptions}
          value={boardId}
          onValueChange={setBoardId}
          placeholder="Select a board"
          searchPlaceholder="Search or type a custom board..."
          emptyMessage="No board found."
          allowCustomValue
        />
      )}

      {mode === "custom-location" && (
        <div className="flex flex-col gap-3 rounded-lg border bg-slate-50 p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="space-y-1">
            <p className="font-medium text-slate-900">
              {customLocation?.name ?? "No custom location selected"}
            </p>
            <p className="text-sm text-slate-600">
              {customLocation
                ? `${customLocation.latitude.toFixed(6)}, ${customLocation.longitude.toFixed(6)}`
                : "Search OpenStreetMap or drop a pin on the map."}
            </p>
          </div>
          <Button
            type="button"
            variant="outline"
            onClick={openCustomLocationDialog}
          >
            {customLocation ? "Edit location" : "Choose location"}
          </Button>
        </div>
      )}

      <div className="space-y-2">
        <label className="text-sm font-medium text-slate-700">
          Setter (optional)
        </label>
        <Combobox
          options={setterOptions}
          value={setterId}
          onValueChange={setSetterId}
          placeholder="Select a setter"
          searchPlaceholder="Search or type a custom setter..."
          emptyMessage="No setter found."
          allowCustomValue
        />
      </div>

      <div className="flex justify-end gap-2">
        <Button variant="outline" onClick={onCancel} disabled={submitting}>
          Back
        </Button>
        <Button onClick={handleSubmit} disabled={submitting}>
          {submitting ? "Creating..." : "Create & continue"}
        </Button>
      </div>

      <Dialog
        open={customLocationDialogOpen}
        onOpenChange={setCustomLocationDialogOpen}
      >
        <DialogContent className="max-h-[calc(100vh-2rem)] overflow-y-auto sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>Choose Custom Location</DialogTitle>
            <DialogDescription>
              Search for a place or click the map to choose where this climb is.
              This will be saved on the climb, not as a reusable place.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-2">
              <label className="text-sm font-medium text-slate-700">
                Location name
              </label>
              <Input
                value={customLocationDraft.name}
                onChange={(e) =>
                  setCustomLocationDraft((current) => ({
                    ...current,
                    name: e.target.value,
                  }))
                }
                placeholder="Crag, gym, city, or landmark"
              />
            </div>

            <LocationPicker
              value={
                customLocationDraft.latitude != null &&
                customLocationDraft.longitude != null
                  ? {
                      lat: customLocationDraft.latitude,
                      lng: customLocationDraft.longitude,
                    }
                  : null
              }
              onPlaceSelected={(selectedName) =>
                setCustomLocationDraft((current) => ({
                  ...current,
                  name: selectedName,
                }))
              }
              onChange={({ lat, lng }) =>
                setCustomLocationDraft((current) => ({
                  ...current,
                  latitude: lat,
                  longitude: lng,
                }))
              }
              mapClassName="h-[320px] sm:h-[420px]"
            />
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => setCustomLocationDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              type="button"
              disabled={
                !customLocationDraft.name.trim() ||
                customLocationDraft.latitude == null ||
                customLocationDraft.longitude == null
              }
              onClick={confirmCustomLocation}
            >
              Use this location
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
