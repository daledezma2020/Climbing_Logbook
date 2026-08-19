import { useState } from "react";
import { Link } from "react-router-dom";
import { useAuth0 } from "@auth0/auth0-react";
import { Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import UserAvatar from "@/components/user/UserAvatar";
import { useComments } from "@/hooks/social-hooks";
import type { CommentTarget } from "@/types/social";

interface CommentThreadProps {
  target: CommentTarget;
  onCountChange?: (total: number) => void;
  emptyMessage?: string;
}

export default function CommentThread({
  target,
  onCountChange,
  emptyMessage = "No comments yet. Be the first to say something.",
}: CommentThreadProps) {
  const { isAuthenticated, loginWithRedirect } = useAuth0();
  const {
    comments,
    total,
    loading,
    loadingMore,
    error,
    hasMore,
    loadMore,
    addComment,
    deleteComment,
  } = useComments(target);
  const [draft, setDraft] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!isAuthenticated) {
      await loginWithRedirect({
        appState: { returnTo: window.location.pathname },
      });
      return;
    }

    const content = draft.trim();
    if (!content) {
      return;
    }

    try {
      setSubmitting(true);
      setActionError(null);
      await addComment({ content });
      setDraft("");
      // total is the server-wide count, not just the loaded page.
      onCountChange?.(total + 1);
    } catch (err) {
      setActionError(
        err instanceof Error ? err.message : "Your comment could not be posted.",
      );
    } finally {
      setSubmitting(false);
    }
  };

  const remove = async (id: number) => {
    try {
      setActionError(null);
      await deleteComment(id);
      onCountChange?.(Math.max(total - 1, 0));
    } catch (err) {
      setActionError(
        err instanceof Error ? err.message : "That comment could not be deleted.",
      );
    }
  };

  return (
    <div className="space-y-4">
      <form onSubmit={(event) => void submit(event)} className="space-y-2">
        <Textarea
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          placeholder="Add a comment..."
          rows={2}
          maxLength={1000}
          aria-label="Add a comment"
        />
        <div className="flex justify-end">
          <Button
            type="submit"
            size="sm"
            disabled={submitting || draft.trim().length === 0}
          >
            {submitting ? "Posting..." : "Post"}
          </Button>
        </div>
      </form>

      {(error || actionError) && (
        <p className="text-sm text-red-600">{actionError ?? error}</p>
      )}

      {loading ? (
        <p className="text-sm text-slate-500">Loading comments...</p>
      ) : comments.length === 0 ? (
        <p className="text-sm text-slate-500">{emptyMessage}</p>
      ) : (
        <ul className="space-y-3">
          {comments.map((comment) => (
            <li key={comment.id} className="flex gap-3">
              <UserAvatar
                displayName={comment.user?.displayName ?? "Unknown"}
                pictureUrl={comment.user?.pictureUrl}
                className="h-8 w-8 text-xs"
              />
              <div className="min-w-0 flex-1">
                <div className="flex items-baseline gap-2">
                  {comment.user ? (
                    <Link
                      to={`/users/${comment.user.username}`}
                      className="text-sm font-medium text-slate-900 hover:underline"
                    >
                      {comment.user.displayName}
                    </Link>
                  ) : (
                    <span className="text-sm font-medium text-slate-900">
                      Unknown climber
                    </span>
                  )}
                  <span className="text-xs text-slate-500">
                    {new Date(comment.createdAt).toLocaleDateString()}
                  </span>
                </div>
                <p className="whitespace-pre-line text-sm text-slate-700">
                  {comment.content}
                </p>
              </div>
              {comment.canDelete && (
                <Button
                  type="button"
                  variant="ghost"
                  size="icon-sm"
                  aria-label="Delete comment"
                  onClick={() => void remove(comment.id)}
                >
                  <Trash2 className="h-4 w-4 text-slate-500" />
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}

      {hasMore && (
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => void loadMore()}
          disabled={loadingMore}
        >
          {loadingMore ? "Loading..." : "Load more comments"}
        </Button>
      )}
    </div>
  );
}
