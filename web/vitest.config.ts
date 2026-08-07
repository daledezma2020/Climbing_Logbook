import { defineConfig, mergeConfig } from "vitest/config";
import viteConfig from "./vite.config";

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      environment: "jsdom",
      setupFiles: ["./src/test/setup.ts"],
      coverage: {
        provider: "v8",
        reporter: ["text", "json-summary", "html", "lcov"],
        reportsDirectory: "./coverage",
        include: ["src/**/*.{ts,tsx}"],
        exclude: [
          "src/**/*.test.{ts,tsx}",
          "src/test/**",
          "src/components/ui/**",
          "src/main.tsx",
          "src/vite-env.d.ts",
        ],
      },
    },
  }),
);
