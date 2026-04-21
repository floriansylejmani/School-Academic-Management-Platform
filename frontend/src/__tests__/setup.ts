import "@testing-library/jest-dom";
import { cleanup } from "@testing-library/react";
import { afterEach } from "vitest";

afterEach(() => {
  cleanup();
});

// Tell React's act() machinery that we are in a test environment.
// Required for React concurrent rendering under jsdom.
// @ts-expect-error intentional global assignment for the test runtime
globalThis.IS_REACT_ACT_ENVIRONMENT = true;
