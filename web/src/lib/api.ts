export const API_BASE = "http://localhost:5050/api";

function formatApiError(status: number, errorData: unknown): string {
  if (typeof errorData === "string" && errorData.trim()) {
    return errorData;
  }

  if (!errorData || typeof errorData !== "object") {
    return `Request failed with HTTP ${status}.`;
  }

  const data = errorData as {
    title?: string;
    detail?: string;
    message?: string;
    errors?: Record<string, string[]>;
  };

  const parts = [
    data.message,
    data.detail,
    data.title,
    data.errors
      ? Object.entries(data.errors)
          .flatMap(([field, messages]) =>
            messages.map((message) => `${field}: ${message}`),
          )
          .join(" ")
      : null,
  ].filter((part): part is string => !!part?.trim());

  return parts.length ? parts.join(" ") : `Request failed with HTTP ${status}.`;
}

export async function apiFetch<T>(
  path: string,
  options?: RequestInit,
): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      headers: { "Content-Type": "application/json" },
      ...options,
    });
  } catch (err) {
    throw new Error(
      err instanceof Error
        ? `Could not reach the API at ${API_BASE}. ${err.message}`
        : `Could not reach the API at ${API_BASE}.`,
    );
  }

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(
      `${formatApiError(response.status, errorData)} Endpoint: ${path}.`,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}
