import { apiRequest } from "../../lib/api/httpClient";
import type {
  AnalysisDecisionHistory,
  AnalysisDecisionState,
  AnalysisDecisionWriteRequest,
  AnalysisDecisionWriteResponse
} from "./analysisDecisionTypes";

export function decisionStatePath(analysisId: string): string {
  return `/api/portal/package-import-analyses/${analysisId}/decision`;
}

export function decisionHistoryPath(analysisId: string): string {
  return `/api/portal/package-import-analyses/${analysisId}/decisions`;
}

export async function fetchDecisionState(
  analysisId: string,
  signal?: AbortSignal
): Promise<AnalysisDecisionState> {
  return apiRequest<AnalysisDecisionState>(decisionStatePath(analysisId), { signal });
}

export async function fetchDecisionHistory(
  analysisId: string,
  signal?: AbortSignal
): Promise<AnalysisDecisionHistory> {
  return apiRequest<AnalysisDecisionHistory>(decisionHistoryPath(analysisId), { signal });
}

export async function postDecision(
  analysisId: string,
  body: AnalysisDecisionWriteRequest
): Promise<AnalysisDecisionWriteResponse> {
  return apiRequest<AnalysisDecisionWriteResponse>(decisionHistoryPath(analysisId), {
    method: "POST",
    requireAntiforgery: true,
    body: {
      decision: body.decision,
      ...(body.rationale !== undefined ? { rationale: body.rationale } : {})
    }
  });
}
