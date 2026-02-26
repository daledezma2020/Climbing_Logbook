import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useClimbRoutes } from "@/hooks/climbroute-hooks";
import RouteForm from "@/components/form components/RouteForm";
import type { CreateClimbRouteInput } from "@/types/climbroute";

export default function EditRoute({ id }: { id: number }) {
  const navigate = useNavigate();
  const { updateRoute } = useClimbRoutes();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (payload: CreateClimbRouteInput) => {
    setError(null);
    try {
      setLoading(true);
      await updateRoute(id, payload);
      navigate("/routes");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create route");
    } finally {
      setLoading(false);
    }
  };

  return (
    <RouteForm
      id={id}
      title="Edit This Route"
      description="Update the details for this route"
      handleSave={handleSubmit}
      loading={loading}
      error={error}
    />
  );
}
