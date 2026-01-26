import { useState, useMemo, useEffect, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useClimbRoutes } from '@/hooks/climbroutes';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Star, MapPin, Search, ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight, ArrowUpDown, ArrowUp, ArrowDown, X, Filter, Plus, Trash2, Pencil } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Combobox } from '@/components/ui/combobox';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';

type SortField = 'name' | 'grade' | 'location' | 'averageRating' | 'setter' | 'type';
type SortDirection = 'asc' | 'desc';

export default function Routes() {
  const navigate = useNavigate();
  const location = useLocation();
  const { routes, loading, error, refetch } = useClimbRoutes();
  const hasRefetched = useRef(false);

  // Refetch routes when returning from create page
  useEffect(() => {
    if (location.state?.refetch && !hasRefetched.current) {
      refetch();
      hasRefetched.current = true;
      // Clear the state to prevent future refetches
      navigate(location.pathname, { replace: true, state: {} });
    }
  }, [location.state, location.pathname, refetch, navigate]);

  // Filter states
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedGrade, setSelectedGrade] = useState<string>('all');
  const [selectedLocation, setSelectedLocation] = useState<string>('all');
  const [selectedSetter, setSelectedSetter] = useState<string>('all');
  const [selectedType, setSelectedType] = useState<string>('all');
  const [minRating, setMinRating] = useState<number>(0);

  // Pagination states
  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(10);

  // Sorting states
  const [sortField, setSortField] = useState<SortField>('name');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');

  // Delete dialog state
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [routeToDelete, setRouteToDelete] = useState<number | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  // Extract unique grades and locations
  const uniqueGrades = useMemo(() => {
    const grades = Array.from(new Set(routes.map(r => r.grade))).sort((a, b) => {
      // Extract numeric part from grade strings (e.g., "V15" -> 15)
      const numA = parseInt(a.replace(/\D/g, ''), 10);
      const numB = parseInt(b.replace(/\D/g, ''), 10);
      return numA - numB;
    });
    return grades;
  }, [routes]);

  const uniqueLocations = useMemo(() => {
    const locations = Array.from(new Set(routes.map(r => r.location))).sort();
    return locations;
  }, [routes]);

  const uniqueSetters = useMemo(() => {
    const setters = Array.from(new Set(routes.map(r => r.setter).filter(Boolean))).sort();
    return setters as string[];
  }, [routes]);

  const uniqueTypes = useMemo(() => {
    const types = Array.from(new Set(routes.map(r => r.type).filter(Boolean))).sort();
    return types as string[];
  }, [routes]);

  // Filter and sort routes
  const filteredAndSortedRoutes = useMemo(() => {
    let filtered = routes.filter(route => {
      const matchesSearch = route.name.toLowerCase().includes(searchQuery.toLowerCase());
      const matchesGrade = selectedGrade === 'all' || route.grade === selectedGrade;
      const matchesLocation = selectedLocation === 'all' || route.location === selectedLocation;
      const matchesSetter = selectedSetter === 'all' || route.setter === selectedSetter;
      const matchesType = selectedType === 'all' || route.type === selectedType;
      const matchesRating = route.averageRating >= minRating;

      return matchesSearch && matchesGrade && matchesLocation && matchesSetter && matchesType && matchesRating;
    });

    // Sort routes
    filtered.sort((a, b) => {
      let aValue = a[sortField];
      let bValue = b[sortField];

      // Handle null values - treat as empty string and push to end
      if (aValue === null || aValue === undefined) aValue = '';
      if (bValue === null || bValue === undefined) bValue = '';

      if (typeof aValue === 'string' && typeof bValue === 'string') {
        aValue = aValue.toLowerCase();
        bValue = bValue.toLowerCase();
      }

      if (aValue < bValue) return sortDirection === 'asc' ? -1 : 1;
      if (aValue > bValue) return sortDirection === 'asc' ? 1 : -1;
      return 0;
    });

    return filtered;
  }, [routes, searchQuery, selectedGrade, selectedLocation, selectedSetter, selectedType, minRating, sortField, sortDirection]);

  // Pagination logic
  const totalPages = Math.ceil(filteredAndSortedRoutes.length / itemsPerPage);
  const paginatedRoutes = useMemo(() => {
    const startIndex = (currentPage - 1) * itemsPerPage;
    return filteredAndSortedRoutes.slice(startIndex, startIndex + itemsPerPage);
  }, [filteredAndSortedRoutes, currentPage, itemsPerPage]);

  // Reset to first page when filters change
  const handleFilterChange = () => {
    setCurrentPage(1);
  };

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDirection('asc');
    }
  };

  const getSortIcon = (field: SortField) => {
    if (sortField !== field) return <ArrowUpDown className="ml-2 h-4 w-4" />;
    return sortDirection === 'asc'
      ? <ArrowUp className="ml-2 h-4 w-4" />
      : <ArrowDown className="ml-2 h-4 w-4" />;
  };

  const clearFilters = () => {
    setSearchQuery('');
    setSelectedGrade('all');
    setSelectedLocation('all');
    setSelectedSetter('all');
    setSelectedType('all');
    setMinRating(0);
    setCurrentPage(1);
  };

  const hasActiveFilters = searchQuery || selectedGrade !== 'all' || selectedLocation !== 'all' || selectedSetter !== 'all' || selectedType !== 'all' || minRating > 0;

  const handleDeleteClick = (routeId: number) => {
    setRouteToDelete(routeId);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = async () => {
    if (!routeToDelete) return;

    try {
      setIsDeleting(true);
      const response = await fetch(`http://localhost:5050/api/routes/${routeToDelete}`, {
        method: 'DELETE',
      });

      if (!response.ok) {
        throw new Error(`Failed to delete route: ${response.status}`);
      }

      // Refresh the routes list
      refetch();
      setDeleteDialogOpen(false);
      setRouteToDelete(null);
    } catch (err) {
      console.error('Error deleting route:', err);
      alert('Failed to delete route. Please try again.');
    } finally {
      setIsDeleting(false);
    }
  };

  const handleDeleteCancel = () => {
    setDeleteDialogOpen(false);
    setRouteToDelete(null);
  };

  const handleEditClick = (routeId: number) => {
    navigate(`/routes/edit/${routeId}`);
  }

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
      <div className="max-w-[1600px] mx-auto">
        <div className="mb-6 flex items-start justify-between gap-4">
          <div className="space-y-2">
            <h1 className="text-4xl font-bold text-slate-900 tracking-tight">
              Climbing Routes
            </h1>
            <p className="text-slate-600">
              Browse and discover all climbing routes
            </p>
          </div>
          <Button onClick={() => navigate('/routes/create')} size="lg">
            <Plus className="mr-2 h-5 w-5" />
            Create Route
          </Button>
        </div>

        <div className="flex gap-6">
          {/* Filters Sidebar */}
          <aside className="w-72 flex-shrink-0">
            <Card>
              <CardHeader className="pb-2">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-lg flex items-center gap-2">
                    <Filter className="h-5 w-5" />
                    Filters
                  </CardTitle>
                  {hasActiveFilters && (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={clearFilters}
                      className="h-8 px-2"
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </CardHeader>
              <CardContent className="space-y-6">
                {/* Search Input */}
                <div className="space-y-2">
                  <label className="text-sm font-medium text-slate-700">Search</label>
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                    <Input
                      placeholder="Search routes..."
                      value={searchQuery}
                      onChange={(e) => {
                        setSearchQuery(e.target.value);
                        handleFilterChange();
                      }}
                      className="pl-9"
                    />
                  </div>
                </div>

                {/* Grade Filter */}
                <div className="space-y-2">
                  <label className="text-sm font-medium text-slate-700">Grade</label>
                  <Select
                    value={selectedGrade}
                    onValueChange={(value) => {
                      setSelectedGrade(value);
                      handleFilterChange();
                    }}
                  >
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="All grades" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">All grades</SelectItem>
                      {uniqueGrades.map((grade) => (
                        <SelectItem key={grade} value={grade}>
                          {grade}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                {/* Location Filter */}
                <div className="space-y-2">
                  <label className="text-sm font-medium text-slate-700">Location</label>
                  <Combobox
                    options={[
                      { value: 'all', label: 'All locations' },
                      ...uniqueLocations.map(loc => ({ value: loc, label: loc }))
                    ]}
                    value={selectedLocation}
                    onValueChange={(value) => {
                      setSelectedLocation(value);
                      handleFilterChange();
                    }}
                    placeholder="All locations"
                    searchPlaceholder="Search locations..."
                    emptyMessage="No location found."
                  />
                </div>

                {/* Setter Filter */}
                <div className="space-y-2">
                  <label className="text-sm font-medium text-slate-700">Setter</label>
                  <Combobox
                    options={[
                      { value: 'all', label: 'All setters' },
                      ...uniqueSetters.map(setter => ({ value: setter, label: setter }))
                    ]}
                    value={selectedSetter}
                    onValueChange={(value) => {
                      setSelectedSetter(value);
                      handleFilterChange();
                    }}
                    placeholder="All setters"
                    searchPlaceholder="Search setters..."
                    emptyMessage="No setter found."
                  />
                </div>

                {/* Type Filter */}
                <div className="space-y-2">
                  <label className="text-sm font-medium text-slate-700">Type</label>
                  <Select
                    value={selectedType}
                    onValueChange={(value) => {
                      setSelectedType(value);
                      handleFilterChange();
                    }}
                  >
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="All types" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">All types</SelectItem>
                      {uniqueTypes.map((type) => (
                        <SelectItem key={type} value={type}>
                          {type}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                {/* Rating Filter */}
                <div className="space-y-2">
                  <label className="text-sm font-medium text-slate-700">Min Rating</label>
                  <Select
                    value={minRating.toString()}
                    onValueChange={(value) => {
                      setMinRating(Number(value));
                      handleFilterChange();
                    }}
                  >
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Any rating" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="0">Any rating</SelectItem>
                      <SelectItem value="1">1+ stars</SelectItem>
                      <SelectItem value="2">2+ stars</SelectItem>
                      <SelectItem value="3">3+ stars</SelectItem>
                      <SelectItem value="4">4+ stars</SelectItem>
                      <SelectItem value="5">5 stars</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </CardContent>
            </Card>
          </aside>

          {/* Main Content */}
          <div className="flex-1 space-y-6">

        {/* Table Section */}
        <div className="bg-white rounded-lg border shadow-sm">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="px-0">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleSort('name')}
                    className="hover:bg-slate-100"
                  >
                    Route Name
                    {getSortIcon('name')}
                  </Button>
                </TableHead>
                <TableHead className="px-0">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleSort('grade')}
                    className="hover:bg-slate-100"
                  >
                    Grade
                    {getSortIcon('grade')}
                  </Button>
                </TableHead>
                <TableHead className="px-0">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleSort('setter')}
                    className="hover:bg-slate-100"
                  >
                    Setter
                    {getSortIcon('setter')}
                  </Button>
                </TableHead>
                <TableHead className="px-0">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleSort('type')}
                    className="hover:bg-slate-100"
                  >
                    Type
                    {getSortIcon('type')}
                  </Button>
                </TableHead>
                <TableHead className="px-0">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleSort('location')}
                    className="hover:bg-slate-100"
                  >
                    Location
                    {getSortIcon('location')}
                  </Button>
                </TableHead>
                <TableHead className="px-0">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleSort('averageRating')}
                    className="hover:bg-slate-100"
                  >
                    Rating
                    {getSortIcon('averageRating')}
                  </Button>
                </TableHead>
                <TableHead className="w-[80px]">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {paginatedRoutes.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} className="text-center py-8 text-slate-500">
                    {hasActiveFilters
                      ? 'No routes match your filters. Try adjusting your search criteria.'
                      : 'No routes found. Add your first climbing route!'}
                  </TableCell>
                </TableRow>
              ) : (
                paginatedRoutes.map((route) => (
                  <TableRow key={route.id} className="hover:bg-slate-50">
                    <TableCell className="font-medium">{route.name}</TableCell>
                    <TableCell>
                      <Badge variant="secondary">{route.grade}</Badge>
                    </TableCell>
                    <TableCell className="text-slate-600">{route.setter || '-'}</TableCell>
                    <TableCell className="text-slate-600">{route.type || '-'}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1 text-slate-600">
                        <MapPin className="h-4 w-4" />
                        {route.location}
                      </div>
                    </TableCell>
                    <TableCell>{renderStars(route.averageRating)}</TableCell>
                    <TableCell>
                      <Button
                        variant="ghost"
                        // size="sm"
                        onClick={() => handleDeleteClick(route.id)}
                        className="!p-0 text-red-600 hover:text-red-700 hover:bg-red-50"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        // size="sm"
                        onClick={() => handleEditClick(route.id)}
                        className="p-0 text-gray-600 hover:text-gray-700 hover:bg-gray-50"
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>

        {/* Pagination Section */}
        {filteredAndSortedRoutes.length > 0 && (
          <div className="flex flex-col sm:flex-row items-center justify-between gap-4 bg-white rounded-lg border shadow-sm p-4">
            <div className="flex items-center gap-4">
              <p className="text-sm text-slate-600">
                Showing {((currentPage - 1) * itemsPerPage) + 1} to{' '}
                {Math.min(currentPage * itemsPerPage, filteredAndSortedRoutes.length)} of{' '}
                {filteredAndSortedRoutes.length} routes
              </p>

              <Select
                value={itemsPerPage.toString()}
                onValueChange={(value) => {
                  setItemsPerPage(Number(value));
                  setCurrentPage(1);
                }}
              >
                <SelectTrigger className="w-[130px]">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="5">5 per page</SelectItem>
                  <SelectItem value="10">10 per page</SelectItem>
                  <SelectItem value="25">25 per page</SelectItem>
                  <SelectItem value="50">50 per page</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setCurrentPage(1)}
                disabled={currentPage === 1}
              >
                <ChevronsLeft className="h-4 w-4" />
              </Button>

              <Button
                variant="outline"
                size="sm"
                onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                disabled={currentPage === 1}
              >
                <ChevronLeft className="h-4 w-4" />
              </Button>

              <div className="flex items-center gap-2 px-4">
                <span className="text-sm text-slate-600">
                  Page {currentPage} of {totalPages}
                </span>
              </div>

              <Button
                variant="outline"
                size="sm"
                onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                disabled={currentPage === totalPages}
              >
                <ChevronRight className="h-4 w-4" />
              </Button>

              <Button
                variant="outline"
                size="sm"
                onClick={() => setCurrentPage(totalPages)}
                disabled={currentPage === totalPages}
              >
                <ChevronsRight className="h-4 w-4" />
              </Button>
            </div>
          </div>
        )}
          </div>
        </div>
      </div>

      {/* Delete Confirmation Dialog */}
      <Dialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Route</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete this route? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={handleDeleteCancel}
              disabled={isDeleting}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeleteConfirm}
              disabled={isDeleting}
            >
              {isDeleting ? 'Deleting...' : 'Delete'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
