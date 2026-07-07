import { Link, useLocation } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Mountain, Home as HomeIcon, BookOpen } from "lucide-react";

export default function Navbar() {
  const location = useLocation();

  const isActive = (path: string) => location.pathname === path;

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
              Vertigo Climbing
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

            <Link to="/logbook">
              <Button
                variant={isActive("/logbook") ? "default" : "ghost"}
                className="gap-2"
              >
                <BookOpen className="h-4 w-4" />
                Logbook
              </Button>
            </Link>

            <Link to="/climbs">
              <Button
                variant={isActive("/climbs") ? "default" : "ghost"}
                className="gap-2"
              >
                <Mountain className="h-4 w-4" />
                Climbs
              </Button>
            </Link>
          </div>
        </div>
      </div>
    </nav>
  );
}
