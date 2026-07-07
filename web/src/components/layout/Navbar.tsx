import { useAuth0 } from "@auth0/auth0-react";
import { Link, useLocation } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import {
  Mountain,
  Home as HomeIcon,
  LogIn,
  LogOut,
  UserPlus,
} from "lucide-react";

export default function Navbar() {
  const location = useLocation();
  const { loginWithRedirect, logout, user, isAuthenticated, isLoading } =
    useAuth0();

  const isActive = (path: string) => location.pathname === path;
  const displayName = user?.name ?? user?.email ?? "Signed in";
  const initials = displayName
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();

  return (
    <nav className="border-b bg-white/80 backdrop-blur-sm sticky top-0 z-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          <Link
            to="/"
            className="flex items-center space-x-2 hover:opacity-80 transition"
          >
            <Mountain className="h-6 w-6 text-slate-900" />
            <span className="font-bold text-xl text-slate-900">
              Climbing Logbook
            </span>
          </Link>

          <div className="flex items-center space-x-1">
            <Link to="/">
              <Button
                variant={isActive("/") ? "default" : "ghost"}
                className="gap-2"
              >
                <HomeIcon className="h-4 w-4" />
                Home
              </Button>
            </Link>

            <Link to="/routes">
              <Button
                variant={isActive("/routes") ? "default" : "ghost"}
                className="gap-2"
              >
                <Mountain className="h-4 w-4" />
                Routes
              </Button>
            </Link>
          </div>

          <div className="flex items-center gap-2">
            {isLoading ? (
              <div className="h-9 w-36 rounded-md bg-slate-100" />
            ) : isAuthenticated ? (
              <>
                <div className="hidden items-center gap-2 sm:flex">
                  <Avatar>
                    <AvatarImage src={user?.picture} alt={displayName} />
                    <AvatarFallback>{initials}</AvatarFallback>
                  </Avatar>
                  <span className="max-w-40 truncate text-sm font-medium text-slate-700">
                    {displayName}
                  </span>
                </div>
                <Button
                  variant="outline"
                  className="gap-2"
                  onClick={() =>
                    logout({
                      logoutParams: {
                        returnTo: window.location.origin,
                      },
                    })
                  }
                >
                  <LogOut className="h-4 w-4" />
                  Sign out
                </Button>
              </>
            ) : (
              <>
                <Button
                  variant="ghost"
                  className="gap-2"
                  onClick={() => loginWithRedirect()}
                >
                  <LogIn className="h-4 w-4" />
                  Sign in
                </Button>
                <Button
                  className="gap-2"
                  onClick={() =>
                    loginWithRedirect({
                      authorizationParams: {
                        screen_hint: "signup",
                      },
                    })
                  }
                >
                  <UserPlus className="h-4 w-4" />
                  Sign up
                </Button>
              </>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
}
