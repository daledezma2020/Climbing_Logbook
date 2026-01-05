import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { ArrowLeft, Save } from 'lucide-react';
import { Combobox } from '@/components/ui/combobox';
import { useClimbRoutes } from '@/hooks/climbroutes';

const GRADE_OPTIONS = Array.from({ length: 18 }, (_, i) => `V${i}`);
const TYPE_OPTIONS = ['Board', 'Gym', 'Outdoor', 'Urban', 'Other'];

export default function CreateRoute() {
  const navigate = useNavigate();
  const { routes } = useClimbRoutes();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Form state
  const [name, setName] = useState('');
  const [grade, setGrade] = useState('');
  const [location, setLocation] = useState('');
  const [setter, setSetter] = useState('');
  const [type, setType] = useState('');
  const [picture, setPicture] = useState('');
  const [video, setVideo] = useState('');

  // Extract unique setters and locations from existing routes
  const uniqueSetters = Array.from(
    new Set(routes.map(r => r.setter).filter(Boolean))
  ).sort() as string[];

  const uniqueLocations = Array.from(
    new Set(routes.map(r => r.location).filter(Boolean))
  ).sort() as string[];

  const setterOptions = [
    ...uniqueSetters.map(setter => ({ value: setter, label: setter }))
  ];

  const locationOptions = [
    ...uniqueLocations.map(loc => ({ value: loc, label: loc }))
  ];

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    // Validation
    if (!name.trim()) {
      setError('Name is required');
      return;
    }
    if (!grade) {
      setError('Grade is required');
      return;
    }
    if (!location.trim()) {
      setError('Location is required');
      return;
    }

    try {
      setLoading(true);

      const payload = {
        Name: name.trim(),
        Grade: grade,
        Location: location.trim(),
        Setter: setter.trim() || null,
        Type: type || null,
        Picture: picture.trim() || null,
        Video: video.trim() || null,
        AverageRating: 0, // Default rating
      };

      console.log('Payload being sent:', payload);
      console.log('Raw values:', { name, grade, location, setter, type, picture, video });

      const response = await fetch('http://localhost:5050/api/routes', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
      }

      // Success - navigate back to routes page with refetch flag
      navigate('/routes', { state: { refetch: true } });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create route');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      <div className="max-w-3xl mx-auto">
        <div className="mb-6">
          <Button
            variant="ghost"
            onClick={() => navigate('/routes')}
            className="mb-4"
          >
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Routes
          </Button>
          <h1 className="text-4xl font-bold text-slate-900 tracking-tight">
            Create New Route
          </h1>
          <p className="text-slate-600 mt-2">
            Add a new climbing route to your logbook
          </p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Route Details</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-6">
              {error && (
                <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                  {error}
                </div>
              )}

              {/* Name */}
              <div className="space-y-2">
                <Label htmlFor="name">
                  Route Name <span className="text-red-500">*</span>
                </Label>
                <Input
                  id="name"
                  placeholder="Enter route name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  maxLength={100}
                  required
                />
              </div>

              {/* Grade */}
              <div className="space-y-2">
                <Label htmlFor="grade">
                  Grade <span className="text-red-500">*</span>
                </Label>
                <Select value={grade} onValueChange={setGrade} required>
                  <SelectTrigger>
                    <SelectValue placeholder="Select grade" />
                  </SelectTrigger>
                  <SelectContent>
                    {GRADE_OPTIONS.map((g) => (
                      <SelectItem key={g} value={g}>
                        {g}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Location */}
              <div className="space-y-2">
                <Label htmlFor="location">
                  Location <span className="text-red-500">*</span>
                </Label>
                <Combobox
                  options={locationOptions}
                  value={location}
                  onValueChange={setLocation}
                  placeholder="Select or enter location"
                  searchPlaceholder="Search locations..."
                  emptyMessage="No location found."
                  allowCustomValue
                />
              </div>

              {/* Setter */}
              <div className="space-y-2">
                <Label htmlFor="setter">Setter</Label>
                <Combobox
                  options={setterOptions}
                  value={setter}
                  onValueChange={setSetter}
                  placeholder="Select or enter setter"
                  searchPlaceholder="Search setters..."
                  emptyMessage="No setter found."
                  allowCustomValue
                />
              </div>

              {/* Type */}
              <div className="space-y-2">
                <Label htmlFor="type">Type</Label>
                <Select value={type} onValueChange={setType}>
                  <SelectTrigger>
                    <SelectValue placeholder="Select type" />
                  </SelectTrigger>
                  <SelectContent>
                    {TYPE_OPTIONS.map((t) => (
                      <SelectItem key={t} value={t}>
                        {t}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Picture URL */}
              <div className="space-y-2">
                <Label htmlFor="picture">Picture URL</Label>
                <Input
                  id="picture"
                  type="url"
                  placeholder="https://example.com/image.jpg"
                  value={picture}
                  onChange={(e) => setPicture(e.target.value)}
                  maxLength={500}
                />
              </div>

              {/* Video URL */}
              <div className="space-y-2">
                <Label htmlFor="video">Video URL</Label>
                <Input
                  id="video"
                  type="url"
                  placeholder="https://example.com/video.mp4"
                  value={video}
                  onChange={(e) => setVideo(e.target.value)}
                  maxLength={500}
                />
              </div>

              {/* Submit Button */}
              <div className="flex gap-3 pt-4">
                <Button
                  type="submit"
                  disabled={loading}
                  className="flex-1"
                >
                  <Save className="mr-2 h-4 w-4" />
                  {loading ? 'Creating...' : 'Create Route'}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => navigate('/routes')}
                  disabled={loading}
                >
                  Cancel
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
