import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  fetchDecisionHistory,
  fetchDecisionState,
  postDecision
} from "./analysisDecisionApi";
import type {
  AnalysisDecisionWriteRequest,
  AnalysisDecisionWriteResponse
} from "./analysisDecisionTypes";

export function decisionStateQueryKey(analysisId: string) {
  return ["portal", "package-import-analysis-decision", analysisId, "state"] as const;
}

export function decisionHistoryQueryKey(analysisId: string) {
  return ["portal", "package-import-analysis-decision", analysisId, "history"] as const;
}

export function useAnalysisDecision(analysisId: string) {
  const queryClient = useQueryClient();

  const stateQuery = useQuery({
    queryKey: decisionStateQueryKey(analysisId),
    queryFn: ({ signal }) => fetchDecisionState(analysisId, signal),
    enabled: analysisId.length > 0
  });

  const historyQuery = useQuery({
    queryKey: decisionHistoryQueryKey(analysisId),
    queryFn: ({ signal }) => fetchDecisionHistory(analysisId, signal),
    enabled: analysisId.length > 0
  });

  const decisionMutation = useMutation({
    mutationFn: (body: AnalysisDecisionWriteRequest) => postDecision(analysisId, body),
    onSuccess: async (_response: AnalysisDecisionWriteResponse) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: decisionStateQueryKey(analysisId) }),
        queryClient.invalidateQueries({ queryKey: decisionHistoryQueryKey(analysisId) })
      ]);
    }
  });

  return {
    stateQuery,
    historyQuery,
    decisionMutation
  };
}
