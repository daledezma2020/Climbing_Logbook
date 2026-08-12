import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import Auth0ProviderWithConfig from "@/auth/Auth0ProviderWithConfig";

const providerProps = vi.hoisted(() => vi.fn());
vi.mock("@auth0/auth0-react", () => ({
  Auth0Provider: (props: { children: React.ReactNode }) => {
    providerProps(props);
    return <>{props.children}</>;
  },
}));

describe("Auth0ProviderWithConfig", () => {
  beforeEach(() => {
    vi.stubEnv("VITE_AUTH0_DOMAIN", "");
    vi.stubEnv("VITE_AUTH0_CLIENT_ID", "");
    vi.stubEnv("VITE_AUTH0_AUDIENCE", "");
  });

  it("shows setup guidance when required configuration is missing", () => {
    render(<Auth0ProviderWithConfig><div>Application</div></Auth0ProviderWithConfig>);
    expect(screen.getByText("Auth0 configuration required")).toBeInTheDocument();
    expect(screen.queryByText("Application")).not.toBeInTheDocument();
  });

  it("configures Auth0 and renders the application when values are present", () => {
    vi.stubEnv("VITE_AUTH0_DOMAIN", "tenant.example.test");
    vi.stubEnv("VITE_AUTH0_CLIENT_ID", "client-id");
    vi.stubEnv("VITE_AUTH0_AUDIENCE", "https://api.example.test");

    render(<Auth0ProviderWithConfig><div>Application</div></Auth0ProviderWithConfig>);

    expect(screen.getByText("Application")).toBeInTheDocument();
    expect(providerProps).toHaveBeenCalledWith(expect.objectContaining({
      domain: "tenant.example.test",
      clientId: "client-id",
      authorizationParams: {
        redirect_uri: window.location.origin,
        audience: "https://api.example.test",
      },
    }));
  });
});
