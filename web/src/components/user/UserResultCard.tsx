import { useState } from "react";
import { Link } from "react-router-dom";
import { MapPin } from "lucide-react";
import UserAvatar from "@/components/user/UserAvatar";
import FollowButton from "@/components/user/FollowButton";
import type { UserSummary } from "@/types/user";

interface UserResultCardProps {
  user: UserSummary;
  onError?: (message: string) => void;
}

export default function UserResultCard({ user, onError }: UserResultCardProps) {
  const [followerCount, setFollowerCount] = useState(user.followerCount);

  return (
    <div className="flex items-center justify-between gap-4 rounded-lg border bg-white p-4 shadow-sm">
      <Link
        to={`/users/${user.username}`}
        className="flex min-w-0 flex-1 items-center gap-4"
      >
        <UserAvatar
          displayName={user.displayName}
          pictureUrl={user.pictureUrl}
          className="h-12 w-12"
        />
        <div className="min-w-0 space-y-1">
          <p className="truncate font-medium text-slate-900">
            {user.displayName}
          </p>
          <p className="truncate text-sm text-slate-500">@{user.username}</p>
          <div className="flex flex-wrap items-center gap-3 text-sm text-slate-600">
            {user.homePlace && (
              <span className="flex items-center gap-1">
                <MapPin className="h-4 w-4" />
                {user.homePlace.name}
              </span>
            )}
            <span>
              {followerCount} follower{followerCount === 1 ? "" : "s"}
            </span>
          </div>
        </div>
      </Link>

      {!user.isMe && (
        <FollowButton
          username={user.username}
          isFollowing={user.isFollowedByMe}
          onError={onError}
          onChange={(isFollowing) =>
            setFollowerCount((count) => count + (isFollowing ? 1 : -1))
          }
        />
      )}
    </div>
  );
}
