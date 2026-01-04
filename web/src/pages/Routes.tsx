import { useClimbRoutes } from '@/hooks/climbroutes';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Star, MapPin } from 'lucide-react';

export default function Routes() {
  const { routes, loading, error } = useClimbRoutes();

  const renderStars = (rating: number) => {
    return (
      <div className="flex items-center gap-1">
        {[1, 2, 3, 4, 5].map((star) => (
          <Star
            key={star}
            className={`h-4 w-4 ${
              star <= rating
                ? 'fill-yellow-400 text-yellow-400'
                : 'text-slate-300'
            }`}
          />
        ))}
        <span className="ml-1 text-sm text-slate-600">
          ({rating.toFixed(1)})
        </span>
      </div>
    );
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
        <div className="max-w-7xl mx-auto">
          <div className="text-center py-12">
            <p className="text-lg text-slate-600">Loading routes...</p>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
        <div className="max-w-7xl mx-auto">
          <div className="text-center py-12">
            <p className="text-lg text-red-600">Error: {error}</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-7xl mx-auto space-y-6">
        <div className="space-y-2">
          <h1 className="text-4xl font-bold text-slate-900 tracking-tight">
            Climbing Routes
          </h1>
          <p className="text-slate-600">
            Browse and discover all climbing routes
          </p>
        </div>

        <div className="bg-white rounded-lg border shadow-sm">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-[80px]">Picture</TableHead>
                <TableHead>Route Name</TableHead>
                <TableHead>Grade</TableHead>
                <TableHead>Location</TableHead>
                <TableHead>Rating</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {routes.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="text-center py-8 text-slate-500">
                    No routes found. Add your first climbing route!
                  </TableCell>
                </TableRow>
              ) : (
                routes.map((route) => (
                  <TableRow key={route.id} className="hover:bg-slate-50">
                    <TableCell>
                      <Avatar className="h-12 w-12">
                        <AvatarImage src={route.picture || undefined} alt={route.name} />
                        <AvatarFallback>{route.name.slice(0, 2).toUpperCase()}</AvatarFallback>
                      </Avatar>
                    </TableCell>
                    <TableCell className="font-medium">{route.name}</TableCell>
                    <TableCell>
                      <Badge variant="secondary">{route.grade}</Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1 text-slate-600">
                        <MapPin className="h-4 w-4" />
                        {route.location}
                      </div>
                    </TableCell>
                    <TableCell>{renderStars(route.averageRating)}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>

        {routes.length > 0 && (
          <div className="text-center text-sm text-slate-600">
            Showing {routes.length} route{routes.length !== 1 ? 's' : ''}
          </div>
        )}
      </div>
    </div>
  );
}
