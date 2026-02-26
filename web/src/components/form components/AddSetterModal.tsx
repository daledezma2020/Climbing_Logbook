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
        <>TEST CONTENT</>
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
