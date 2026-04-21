import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/__tests__/setup.ts"],
    // Force vitest to bundle react/react-dom through Vite so that
    // `define` substitutions apply. Without this, the optimizer loads
    // React's production CJS build which omits `act`, breaking
    // @testing-library/react in React 19.
    deps: {
      optimizer: {
        web: {
          include: ["react", "react-dom", "react-dom/test-utils"]
        }
      }
    }
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src")
    }
  },
  // Replace NODE_ENV so React's conditional bundles pick the dev build
  // (which exports `act`) rather than the stripped production build.
  define: {
    "process.env.NODE_ENV": JSON.stringify("test")
  }
});
