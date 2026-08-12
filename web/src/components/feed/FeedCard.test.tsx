import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import FeedCard from "@/components/feed/FeedCard";
import { comment, logEntry, userSummary } from "@/test/fixtures";

const auth = vi.hoisted(() => ({
  isAuthenticated: true,
  loginWithRedirect: vi.fn(),
  getAccessTokenSilently: vi.fn().mockResolvedValue("token"),
}));

const social = vi.hoisted(() => ({
  like: vi.fn(),
  unlike: vi.fn(),
  comments: [] as ReturnType<typeof comment>[],
  // Server-wide count, which can exceed the loaded page.
  total: null as number | null,
  addComment: vi.fn(),
  deleteComment: vi.fn(),
}));

vi.mock("@auth0/auth0-react", () => ({ useAuth0: () => auth }));

vi.mock("@/hooks/social-hooks", () => ({
  useLikeToggle: () => ({ like: social.like, unlike: social.unlike }),
  useComments: () => ({
    comments: social.comments,
    total: social.total ?? social.comments.length,
    loading: false,
    loadingMore: false,
    error: null,
    hasMore: false,
    loadMore: vi.fn(),
    addComment: social.addComment,
    deleteComment: social.deleteComment,
  }),
}));

function renderCard(entry = logEntry()) {
  return render(
    <MemoryRouter>
      <FeedCard entry={entry} />
    </MemoryRouter>,
  );
}

describe("FeedCard", () => {
  beforeEach(() => {
    auth.isAuthenticated = true;
    social.like = vi.fn().mockResolvedValue(undefined);
    social.unlike = vi.fn().mockResolvedValue(undefined);
    social.comments = [];
    social.total = null;
    social.addComment = vi.fn().mockResolvedValue(comment());
    social.deleteComment = vi.fn().mockResolvedValue(undefined);
  });

  it("bumps the like count optimistically before the request settles", async () => {
    renderCard(logEntry({ id: 3, likeCount: 2, isLikedByMe: false }));

    const button = screen.getByRole("button", { name: "Like this climb" });
    expect(button).toHaveTextContent("2");

    await userEvent.click(button);

    expect(social.like).toHaveBeenCalledWith(3);
    await waitFor(() =>
      expect(
        screen.getByRole("button", { name: "Unlike this climb" }),
      ).toHaveTextContent("3"),
    );
  });

  it("rolls the like back when the request fails", async () => {
    social.like = vi.fn().mockRejectedValue(new Error("nope"));
    renderCard(logEntry({ likeCount: 5, isLikedByMe: false }));

    await userEvent.click(screen.getByRole("button", { name: "Like this climb" }));

    await waitFor(() =>
      expect(
        screen.getByRole("button", { name: "Like this climb" }),
      ).toHaveTextContent("5"),
    );
  });

  it("unlikes an entry the caller had already liked", async () => {
    renderCard(logEntry({ id: 8, likeCount: 4, isLikedByMe: true }));

    await userEvent.click(
      screen.getByRole("button", { name: "Unlike this climb" }),
    );

    expect(social.unlike).toHaveBeenCalledWith(8);
    await waitFor(() =>
      expect(
        screen.getByRole("button", { name: "Like this climb" }),
      ).toHaveTextContent("3"),
    );
  });

  it("sends the caller to sign in before liking when signed out", async () => {
    auth.isAuthenticated = false;
    renderCard();

    await userEvent.click(screen.getByRole("button", { name: "Like this climb" }));

    expect(auth.loginWithRedirect).toHaveBeenCalled();
    expect(social.like).not.toHaveBeenCalled();
  });

  it("keeps the comment thread collapsed until the count is clicked", async () => {
    renderCard(logEntry({ commentCount: 2 }));

    expect(screen.queryByLabelText("Add a comment")).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { expanded: false }));

    expect(screen.getByLabelText("Add a comment")).toBeInTheDocument();
  });

  it("posts a comment and clears the composer", async () => {
    renderCard();

    await userEvent.click(screen.getByRole("button", { expanded: false }));
    const box = screen.getByLabelText("Add a comment");
    await userEvent.type(box, "great line");
    await userEvent.click(screen.getByRole("button", { name: "Post" }));

    expect(social.addComment).toHaveBeenCalledWith({ content: "great line" });
    await waitFor(() => expect(box).toHaveValue(""));
  });

  it("counts a new comment against the server total, not the loaded page", async () => {
    // 100 comments exist server-side but only a page of 25 is loaded.
    social.comments = Array.from({ length: 25 }, (_, i) => comment({ id: i + 1 }));
    social.total = 100;
    renderCard(logEntry({ commentCount: 100 }));

    const toggle = screen.getByRole("button", { expanded: false });
    expect(toggle).toHaveTextContent("100");

    await userEvent.click(toggle);
    await userEvent.type(screen.getByLabelText("Add a comment"), "one more");
    await userEvent.click(screen.getByRole("button", { name: "Post" }));

    await waitFor(() =>
      expect(screen.getByRole("button", { expanded: true })).toHaveTextContent(
        "101",
      ),
    );
  });

  it("will not post an empty comment", async () => {
    renderCard();

    await userEvent.click(screen.getByRole("button", { expanded: false }));

    expect(screen.getByRole("button", { name: "Post" })).toBeDisabled();
  });

  it("only offers a delete control on the caller's own comments", async () => {
    social.comments = [
      comment({ id: 1, content: "mine", canDelete: true }),
      comment({
        id: 2,
        content: "theirs",
        canDelete: false,
        user: userSummary({ username: "sam", displayName: "Sam" }),
      }),
    ];
    renderCard();

    await userEvent.click(screen.getByRole("button", { expanded: false }));

    expect(screen.getAllByRole("button", { name: "Delete comment" })).toHaveLength(1);

    await userEvent.click(screen.getByRole("button", { name: "Delete comment" }));
    expect(social.deleteComment).toHaveBeenCalledWith(1);
  });

  it("labels an attempt differently from a send", () => {
    renderCard(logEntry({ status: "Attempted" }));
    expect(screen.getByText(/worked/)).toBeInTheDocument();
  });
});
