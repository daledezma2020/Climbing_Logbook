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

export default AddLocationModal;
