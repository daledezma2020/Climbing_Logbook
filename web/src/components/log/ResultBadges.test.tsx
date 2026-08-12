import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { SourceBadges, TypeBadge } from "@/components/log/ResultBadges";

describe("result badges", () => {
  it("uses friendly provider labels while preserving unknown sources", () => {
    render(<SourceBadges sources={["openbeta", "MoonBoardSeed", "community"]} />);
    expect(screen.getByText("OpenBeta")).toBeInTheDocument();
    expect(screen.getByText("MoonBoard")).toBeInTheDocument();
    expect(screen.getByText("community")).toBeInTheDocument();
  });

  it("distinguishes climbs, gyms, and other places", () => {
    const { rerender } = render(<TypeBadge resultType="climb" />);
    expect(screen.getByText("Climb")).toBeInTheDocument();
    rerender(<TypeBadge resultType="place" placeKind="Gym" />);
    expect(screen.getByText("Gym")).toBeInTheDocument();
    rerender(<TypeBadge resultType="place" placeKind="Outdoor" />);
    expect(screen.getByText("Place")).toBeInTheDocument();
  });
});
