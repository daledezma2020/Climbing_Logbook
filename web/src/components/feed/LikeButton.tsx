import { useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { Heart } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useLikeToggle } from "@/hooks/social-hooks";
import { cn } from "@/lib/utils";

interface LikeButtonProps {
  logEntryId: number;
  isLiked: boolean;
  likeCount: number;
  onError?: (message: string) => void;
}

export default function LikeButton({
  logEntryId,
  isLiked,
  likeCount,
  onError,
}: LikeButtonProps) {
  const { isAuthenticated, loginWithRedirect } = useAuth0();
  const { like, unlike } = useLikeToggle();
  const [liked, setLiked] = useState(isLiked);
  const [count, setCount] = useState(likeCount);
  const [pending, setPending] = useState(false);

  const toggle = async () => {
    if (!isAuthenticated) {
      await loginWithRedirect({
        appState: { returnTo: window.location.pathname },
      });
      return;
    }

    const next = !liked;
    setLiked(next);
    setCount((current) => current + (next ? 1 : -1));
    setPending(true);

    try {
      await (next ? like(logEntryId) : unlike(logEntryId));
    } catch (err) {
      setLiked(!next);
      setCount((current) => current + (next ? -1 : 1));
      onError?.(
        err instanceof Error
          ? err.message
          : `Could not ${next ? "like" : "unlike"} this climb.`,
      );
    } finally {
      setPending(false);
    }
  };

  return (
    <Button
      type="button"
      variant="ghost"
      size="sm"
      className="gap-2 text-slate-600"
      disabled={pending}
      aria-pressed={liked}
      aria-label={liked ? "Unlike this climb" : "Like this climb"}
      onClick={() => void toggle()}
    >
      <Heart
        className={cn("h-4 w-4", liked && "fill-rose-500 text-rose-500")}
      />
      {count}
    </Button>
  );
}
