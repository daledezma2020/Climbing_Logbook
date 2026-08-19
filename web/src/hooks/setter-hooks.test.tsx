import { renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { useSetters } from "@/hooks/setter-hooks";
import { apiFetch } from "@/lib/api";

vi.mock("@/lib/api", () => ({ apiFetch: vi.fn() }));

describe("useSetters", () => {
  it("loads setters", async () => {
    vi.mocked(apiFetch).mockResolvedValue([{ id: 1, name: "Alex" }]);
    const { result } = renderHook(() => useSetters());
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.setters).toEqual([{ id: 1, name: "Alex" }]);
    expect(result.current.error).toBeNull();
  });

  it("exposes request failures", async () => {
    vi.mocked(apiFetch).mockRejectedValue(new Error("Setter API failed"));
    const { result } = renderHook(() => useSetters());
    await waitFor(() => expect(result.current.loading).toBe(false));
    expect(result.current.error).toBe("Setter API failed");
  });
});
