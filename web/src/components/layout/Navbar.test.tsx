import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import Navbar from "@/components/layout/Navbar";

const auth = vi.hoisted(() => ({
  loginWithRedirect: vi.fn(),
  logout: vi.fn(),
  user: undefined as { name?: string; email?: string; picture?: string } | undefined,
  isAuthenticated: false,
  isLoading: false,
}));

vi.mock("@auth0/auth0-react", () => ({ useAuth0: () => auth }));

describe("Navbar authentication", () => {
  beforeEach(() => {
    auth.isAuthenticated = false;
    auth.isLoading = false;
    auth.user = undefined;
  });

  it("starts sign-in and sign-up flows for visitors", async () => {
    render(<MemoryRouter><Navbar /></MemoryRouter>);

    await userEvent.click(screen.getByRole("button", { name: "Sign in" }));
    await userEvent.click(screen.getByRole("button", { name: "Sign up" }));

    expect(auth.loginWithRedirect).toHaveBeenNthCalledWith(1, undefined);
    expect(auth.loginWithRedirect).toHaveBeenNthCalledWith(2, {
      authorizationParams: { screen_hint: "signup" },
    });
  });

  it("shows the user and returns to this origin on sign-out", async () => {
    auth.isAuthenticated = true;
    auth.user = { name: "Alex Climber" };
    render(<MemoryRouter><Navbar /></MemoryRouter>);

    expect(screen.getByText("Alex Climber")).toBeInTheDocument();
    expect(screen.getByText("AC")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Sign out" }));
    expect(auth.logout).toHaveBeenCalledWith({ logoutParams: { returnTo: window.location.origin } });
  });
});
