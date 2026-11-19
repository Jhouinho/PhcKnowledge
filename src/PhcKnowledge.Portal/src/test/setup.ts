import "@testing-library/jest-dom/vitest";
import { afterEach } from "vitest";
import { cleanup } from "@testing-library/react";
import { resetAntiforgeryCache } from "../lib/api/httpClient";

afterEach(() => {
  cleanup();
  resetAntiforgeryCache();
});
