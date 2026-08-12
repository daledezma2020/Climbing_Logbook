import { useState } from "react";
import { SlidersHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  KPI_CATALOG,
  KPI_GROUP_LABELS,
  type KpiGroup,
} from "@/components/home/kpi-catalog";

interface KpiCustomizerProps {
  selectedIds: string[];
  onToggle: (id: string) => void;
  onReset: () => void;
}

const GROUP_ORDER: KpiGroup[] = ["core", "activity", "places", "social"];

export default function KpiCustomizer({
  selectedIds,
  onToggle,
  onReset,
}: KpiCustomizerProps) {
  const [open, setOpen] = useState(false);

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button type="button" variant="outline" size="sm" className="gap-2">
          <SlidersHorizontal className="h-4 w-4" />
          Customize
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Customize your stats</DialogTitle>
          <DialogDescription>
            Pick which cards show at the top of your home page.
          </DialogDescription>
        </DialogHeader>

        <div className="max-h-96 space-y-4 overflow-y-auto">
          {GROUP_ORDER.map((group) => (
            <fieldset key={group} className="space-y-2">
              <legend className="text-sm font-medium text-slate-900">
                {KPI_GROUP_LABELS[group]}
              </legend>
              {KPI_CATALOG.filter((kpi) => kpi.group === group).map((kpi) => (
                <label
                  key={kpi.id}
                  className="flex items-center gap-2 text-sm text-slate-700"
                >
                  <input
                    type="checkbox"
                    className="h-4 w-4 rounded border-slate-300"
                    checked={selectedIds.includes(kpi.id)}
                    onChange={() => onToggle(kpi.id)}
                  />
                  {kpi.title}
                </label>
              ))}
            </fieldset>
          ))}
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={onReset}>
            Reset to default
          </Button>
          <Button type="button" onClick={() => setOpen(false)}>
            Done
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
