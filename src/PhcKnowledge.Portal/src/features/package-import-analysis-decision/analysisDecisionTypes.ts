export type DecisionKind = "confirmed" | "rejected" | "unconfirmed";

export type MaterializationReadiness =
  | "Ready"
  | "ReadyWithWarnings"
  | "Blocked"
  | string;

export type AnalysisDecisionState = {
  analysisId: string;
  packageId: string;
  decision: DecisionKind | string;
  decisionSequence: number | null;
  decidedAtUtc: string | null;
  decidedBySubject: string | null;
  decidedByIssuer: string | null;
  rationale: string | null;
  semanticFingerprintSnapshot: string | null;
  materializationReadiness: MaterializationReadiness;
};

export type AnalysisDecisionHistoryItem = {
  decisionId: string;
  decision: string;
  decisionSequence: number;
  decidedAtUtc: string;
  decidedBySubject: string;
  decidedByIssuer: string;
  rationale: string | null;
  semanticFingerprintSnapshot: string;
};

export type AnalysisDecisionHistory = {
  analysisId: string;
  packageId: string;
  items: AnalysisDecisionHistoryItem[];
};

export type AnalysisDecisionWriteRequest = {
  decision: "confirmed" | "rejected";
  rationale?: string | null;
};

export type AnalysisDecisionWriteResponse = {
  analysisId: string;
  packageId: string;
  decision: string;
  decisionSequence: number;
  decidedAtUtc: string;
  decidedBySubject: string;
  decidedByIssuer: string;
  rationale: string | null;
  semanticFingerprintSnapshot: string;
  idempotentReplay: boolean;
};

export function canConfirm(readiness: MaterializationReadiness): boolean {
  return readiness === "Ready" || readiness === "ReadyWithWarnings";
}

export function canReject(readiness: MaterializationReadiness): boolean {
  return (
    readiness === "Ready" ||
    readiness === "ReadyWithWarnings" ||
    readiness === "Blocked"
  );
}

export function hasNonWhitespaceRationale(value: string | null | undefined): boolean {
  return typeof value === "string" && value.trim().length > 0;
}

export function decisionLabel(decision: string): string {
  switch (decision.toLowerCase()) {
    case "confirmed":
      return "Confirmed";
    case "rejected":
      return "Rejected";
    case "unconfirmed":
      return "Unconfirmed";
    default:
      return decision;
  }
}
