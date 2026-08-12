import { useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { UserMinus, UserPlus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useFollow } from "@/hooks/user-hooks";

interface FollowButtonProps {
  username: string;
  isFollowing: boolean;
  onError?: (message: string) => void;
  onChange?: (isFollowing: boolean) => void;
  className?: string;
}

export default function FollowButton({
  username,
  isFollowing,
  onError,
  onChange,
  className,
}: FollowButtonProps) {
  const { isAuthenticated, loginWithRedirect } = useAuth0();
  const { follow, unfollow } = useFollow();
  const [following, setFollowing] = useState(isFollowing);
  const [pending, setPending] = useState(false);

  const toggle = async () => {
    if (!isAuthenticated) {
      await loginWithRedirect({
        appState: { returnTo: window.location.pathname },
      });
      return;
    }

    const next = !following;
    setFollowing(next);
    setPending(true);

    try {
      await (next ? follow(username) : unfollow(username));
      onChange?.(next);
    } catch (err) {
      setFollowing(!next);
      onError?.(
        err instanceof Error
          ? err.message
          : `Could not ${next ? "follow" : "unfollow"} @${username}.`,
      );
    } finally {
      setPending(false);
    }
  };

  return (
    <Button
      type="button"
      variant={following ? "outline" : "default"}
      className={className}
      disabled={pending}
      onClick={() => void toggle()}
    >
      {following ? (
        <UserMinus className="mr-2 h-4 w-4" />
      ) : (
        <UserPlus className="mr-2 h-4 w-4" />
      )}
      {following ? "Following" : "Follow"}
    </Button>
  );
}
