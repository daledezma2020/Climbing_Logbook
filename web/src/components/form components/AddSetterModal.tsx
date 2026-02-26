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

const AddSetterModal = () => (
  <Dialog>
    <form>
      <DialogTrigger asChild>
        <Button variant="outline">Add Setter</Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>Add Setter</DialogTitle>
          <DialogDescription>
            Insert setter details to add a custom setter to the system.
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-2">
          <Label htmlFor="name">
            Name <span className="text-red-500">*</span>
          </Label>
          <Input
            id="name"
            placeholder="Enter setter name"
            maxLength={100}
            required
          />
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

export default AddSetterModal;
