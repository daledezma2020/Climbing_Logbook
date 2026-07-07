import { AlertCircle, X } from "lucide-react";
import { Button } from "@/components/ui/button";

interface ErrorToastProps {
  title: string;
  message: string;
  onDismiss?: () => void;
}

export default function ErrorToast({
  title,
  message,
  onDismiss,
}: ErrorToastProps) {
  return (
    <div
      role="alert"
      className="flex items-start gap-3 rounded-md border border-red-200 bg-red-50 p-4 text-red-900 shadow-sm"
    >
      <AlertCircle className="mt-0.5 h-5 w-5 shrink-0 text-red-600" />
      <div className="min-w-0 flex-1 space-y-1">
        <p className="font-medium">{title}</p>
        <p className="text-sm leading-5 text-red-800">{message}</p>
      </div>
      {onDismiss && (
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="-mr-2 -mt-2 h-8 w-8 p-0 text-red-700 hover:bg-red-100 hover:text-red-900"
          onClick={onDismiss}
        >
          <X className="h-4 w-4" />
          <span className="sr-only">Dismiss error</span>
        </Button>
      )}
    </div>
  );
}
