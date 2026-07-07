import { Auth0Provider } from "@auth0/auth0-react";
import type { ReactNode } from "react";

interface Auth0ProviderWithConfigProps {
  children: ReactNode;
}

export default function Auth0ProviderWithConfig({
  children,
}: Auth0ProviderWithConfigProps) {
  const domain = import.meta.env.VITE_AUTH0_DOMAIN;
  const clientId = import.meta.env.VITE_AUTH0_CLIENT_ID;
  const audience = import.meta.env.VITE_AUTH0_AUDIENCE;

  if (!domain || !clientId || !audience) {
    return (
      <div className="min-h-screen bg-slate-50 p-8 text-slate-900">
        <div className="mx-auto max-w-2xl rounded-lg border bg-white p-6 shadow-sm">
          <h1 className="text-2xl font-bold">Auth0 configuration required</h1>
          <p className="mt-3 text-slate-600">
            Add VITE_AUTH0_DOMAIN, VITE_AUTH0_CLIENT_ID, and VITE_AUTH0_AUDIENCE
            to web/.env.local, then restart the Vite dev server.
          </p>
        </div>
      </div>
    );
  }

  return (
    <Auth0Provider
      domain={domain}
      clientId={clientId}
      authorizationParams={{
        redirect_uri: window.location.origin,
        audience,
      }}
    >
      {children}
    </Auth0Provider>
  );
}
