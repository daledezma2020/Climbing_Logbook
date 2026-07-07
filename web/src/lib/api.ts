export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5050";
export const API_BASE = `${API_BASE_URL}/api`;

type ApiFetchOptions = RequestInit & {
  accessToken?: string;
};

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
  options: ApiFetchOptions = {},
): Promise<T> {
  const { accessToken, headers: optionHeaders, ...requestOptions } = options;
  const headers = new Headers(optionHeaders);

  if (!headers.has("Content-Type") && requestOptions.body) {
    headers.set("Content-Type", "application/json");
  }

  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      ...requestOptions,
      headers,
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
