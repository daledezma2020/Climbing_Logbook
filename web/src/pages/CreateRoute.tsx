import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useClimbRoutes } from "@/hooks/climbroute-hooks";
import RouteForm from "@/components/form components/RouteForm";
import type { CreateClimbRouteInput } from "@/types/climbroute";

export default function CreateRoute() {
  const navigate = useNavigate();
  const { createRoute } = useClimbRoutes();

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (payload: CreateClimbRouteInput) => {
    setError(null);
    try {
      setLoading(true);
      await createRoute(payload);
      navigate("/routes");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create route");
    } finally {
      setLoading(false);
    }
  };

  return (
    <RouteForm
      title="Create a Route"
      description="Add a new Route to the app"
      handleSave={handleSubmit}
      loading={loading}
      error={error}
    />
  );
}
