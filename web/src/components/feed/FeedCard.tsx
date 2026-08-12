import { useState } from "react";
import { Link } from "react-router-dom";
import { MapPin, MessageSquare, Star } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import UserAvatar from "@/components/user/UserAvatar";
import LikeButton from "@/components/feed/LikeButton";
import CommentThread from "@/components/feed/CommentThread";
import { placeLabel } from "@/lib/log-entry";
import type { LogEntry } from "@/types/catalog";

interface FeedCardProps {
  entry: LogEntry;
  onError?: (message: string) => void;
}

function relativeDay(iso: string): string {
  const days = Math.floor(
    (Date.now() - new Date(iso).getTime()) / (1000 * 60 * 60 * 24),
  );

  if (days <= 0) return "today";
  if (days === 1) return "yesterday";
  if (days < 30) return `${days} days ago`;
  return new Date(iso).toLocaleDateString();
}

export default function FeedCard({ entry, onError }: FeedCardProps) {
  const [showComments, setShowComments] = useState(false);
  const [commentCount, setCommentCount] = useState(entry.commentCount);
  const where = placeLabel(entry);
  const climber = entry.user;
  const sent = entry.status === "Completed";

  return (
    <Card>
      <CardContent className="space-y-3 pt-6">
        <div className="flex items-start gap-3">
          <Link to={climber ? `/users/${climber.username}` : "#"}>
            <UserAvatar
              displayName={climber?.displayName ?? "Unknown"}
              pictureUrl={climber?.pictureUrl}
              className="h-10 w-10"
            />
          </Link>

          <div className="min-w-0 flex-1">
            <p className="text-sm text-slate-600">
              {climber ? (
                <Link
                  to={`/users/${climber.username}`}
                  className="font-medium text-slate-900 hover:underline"
                >
                  {climber.displayName}
                </Link>
              ) : (
                <span className="font-medium text-slate-900">
                  Unknown climber
                </span>
              )}{" "}
              {sent ? "sent" : "worked"}{" "}
              <Link
                to={`/climbs/${entry.climbId}`}
                className="font-medium text-slate-900 hover:underline"
              >
                {entry.climb?.name ?? "a climb"}
              </Link>
            </p>

            <div className="flex flex-wrap items-center gap-2 pt-1 text-sm text-slate-600">
              {entry.climb?.grade && (
                <Badge variant="secondary">{entry.climb.grade}</Badge>
              )}
              <Badge variant={sent ? "default" : "outline"}>
                {entry.status}
              </Badge>
              {where && (
                <span className="flex items-center gap-1">
                  <MapPin className="h-4 w-4" />
                  {where}
                </span>
              )}
              <span>{relativeDay(entry.occurredAt)}</span>
              {entry.rating && (
                <span className="flex items-center gap-1">
                  <Star className="h-4 w-4 fill-yellow-400 text-yellow-400" />
                  {entry.rating}
                </span>
              )}
            </div>

            {entry.notes && (
              <p className="whitespace-pre-line pt-2 text-sm text-slate-700">
                {entry.notes}
              </p>
            )}
          </div>
        </div>

        <div className="flex items-center gap-1 border-t pt-2">
          <LikeButton
            logEntryId={entry.id}
            isLiked={entry.isLikedByMe}
            likeCount={entry.likeCount}
            onError={onError}
          />
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="gap-2 text-slate-600"
            aria-expanded={showComments}
            onClick={() => setShowComments((open) => !open)}
          >
            <MessageSquare className="h-4 w-4" />
            {commentCount}
          </Button>
          <Link
            to={`/climbs/${entry.climbId}`}
            className="ml-auto text-sm text-slate-500 hover:underline"
          >
            View climb
          </Link>
        </div>

        {showComments && (
          <div className="border-t pt-3">
            <CommentThread
              target={{ kind: "logEntry", id: entry.id }}
              onCountChange={setCommentCount}
            />
          </div>
        )}
      </CardContent>
    </Card>
  );
}
