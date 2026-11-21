import { describe, expect, it } from "vitest";
import {
  canConfirm,
  canReject,
  decisionLabel,
  hasNonWhitespaceRationale
} from "./analysisDecisionTypes";

describe("analysis decision eligibility helpers", () => {
  it("allows confirm and reject for Ready", () => {
    expect(canConfirm("Ready")).toBe(true);
    expect(canReject("Ready")).toBe(true);
  });

  it("allows confirm and reject for ReadyWithWarnings", () => {
    expect(canConfirm("ReadyWithWarnings")).toBe(true);
    expect(canReject("ReadyWithWarnings")).toBe(true);
  });

  it("blocks confirm and allows reject for Blocked", () => {
    expect(canConfirm("Blocked")).toBe(false);
    expect(canReject("Blocked")).toBe(true);
  });

  it("rejects whitespace-only rationale", () => {
    expect(hasNonWhitespaceRationale("")).toBe(false);
    expect(hasNonWhitespaceRationale("   ")).toBe(false);
    expect(hasNonWhitespaceRationale("motivo")).toBe(true);
  });

  it("formats decision labels", () => {
    expect(decisionLabel("unconfirmed")).toBe("Unconfirmed");
    expect(decisionLabel("confirmed")).toBe("Confirmed");
    expect(decisionLabel("rejected")).toBe("Rejected");
  });
});
