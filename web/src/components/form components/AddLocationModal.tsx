import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const AddLocationModal = () => (
  <Dialog>
    <form>
      <DialogTrigger asChild>
        <Button variant="outline">Add Location</Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>Add Location</DialogTitle>
          <DialogDescription>
            Insert location details to add a custom location to the system.
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-2">
          <Label htmlFor="name">
            Name <span className="text-red-500">*</span>
          </Label>
          <Input
            id="name"
            placeholder="Enter location name"
            maxLength={100}
            required
          />
        </div>

        <div className="flex gap-2">
          <div className="space-y-2">
            <Label htmlFor="name">Latitude</Label>
            <Input
              id="name"
              placeholder="Enter latitude"
              maxLength={100}
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="name">Longitude</Label>
            <Input
              id="name"
              placeholder="Enter longitude"
              maxLength={100}
              required
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="name">Country</Label>
          <Input
            id="name"
            placeholder="Enter country name"
            maxLength={100}
            required
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="name">Address</Label>
          <Input
            id="name"
            placeholder="Enter address"
            maxLength={100}
            required
          />
        </div>

        <div className="flex gap-2">
          <div className="space-y-2">
            <Label htmlFor="name">City</Label>
            <Input
              id="name"
              placeholder="Enter city name"
              maxLength={100}
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="name">State/Region</Label>
            <Input
              id="name"
              placeholder="Enter state/region"
              maxLength={100}
              required
            />
          </div>
        </div>

        <DialogFooter>
          <DialogClose asChild>
            <Button variant="outline">Cancel</Button>
          </DialogClose>
          <Button>Add</Button>
        </DialogFooter>
      </DialogContent>
    </form>
  </Dialog>
);

export default AddLocationModal;
