import { useMemo, useState } from "react";
import Alert from "../../components/ui/Alert";
import Dialog from "../../components/ui/Dialog";
import { userMessageForApiError } from "../../lib/api/apiError";
import {
  canConfirm,
  canReject,
  decisionLabel,
  hasNonWhitespaceRationale,
  type AnalysisDecisionHistoryItem,
  type AnalysisDecisionState
} from "./analysisDecisionTypes";
import { useAnalysisDecision } from "./useAnalysisDecision";
import styles from "./AnalysisDecisionPanel.module.css";

type ActionMode = "confirm" | "reject" | null;

function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("pt-PT", {
    dateStyle: "short",
    timeStyle: "medium",
    timeZone: "UTC"
  }).format(date) + " UTC";
}

function DecisionBadge({ decision }: { decision: string }) {
  const normalized = decision.toLowerCase();
  const tone =
    normalized === "confirmed"
      ? styles.badgeConfirmed
      : normalized === "rejected"
        ? styles.badgeRejected
        : styles.badgeUnconfirmed;

  return <span className={`${styles.badge} ${tone}`}>{decisionLabel(decision)}</span>;
}

function CurrentState({ state }: { state: AnalysisDecisionState }) {
  const isUnconfirmed = state.decision.toLowerCase() === "unconfirmed";

  return (
    <section className={styles.section} aria-labelledby="decision-state-title">
      <div className={styles.sectionHeader}>
        <div>
          <p className={styles.eyebrow}>ESTADO ATUAL</p>
          <h2 id="decision-state-title">Decisão da análise</h2>
        </div>
        <DecisionBadge decision={state.decision} />
      </div>

      {isUnconfirmed ? (
        <p className={styles.muted}>
          Ainda não existe uma decisão humana registada para esta análise.
        </p>
      ) : (
        <dl className={styles.metaGrid}>
          <div>
            <dt>Data e hora</dt>
            <dd>{formatDateTime(state.decidedAtUtc)}</dd>
          </div>
          <div>
            <dt>Autor (sujeito)</dt>
            <dd className={styles.mono}>{state.decidedBySubject ?? "—"}</dd>
          </div>
          <div>
            <dt>Emissor</dt>
            <dd className={styles.mono}>{state.decidedByIssuer ?? "—"}</dd>
          </div>
          <div>
            <dt>Sequência</dt>
            <dd>{state.decisionSequence ?? "—"}</dd>
          </div>
          <div className={styles.fullRow}>
            <dt>Justificação</dt>
            <dd>{state.rationale && state.rationale.length > 0 ? state.rationale : "—"}</dd>
          </div>
        </dl>
      )}

      <p className={styles.readiness}>
        Prontidão da análise: <strong>{state.materializationReadiness}</strong>
      </p>
    </section>
  );
}

function HistoryList({
  items,
  effectiveSequence
}: {
  items: AnalysisDecisionHistoryItem[];
  effectiveSequence: number | null;
}) {
  if (items.length === 0) {
    return (
      <div className={styles.empty} role="status">
        <p>Sem histórico de decisões.</p>
        <span>As confirmações e rejeições aparecerão aqui por ordem cronológica inversa.</span>
      </div>
    );
  }

  return (
    <ol className={styles.historyList}>
      {items.map((item) => {
        const isEffective =
          effectiveSequence !== null && item.decisionSequence === effectiveSequence;

        return (
          <li
            key={item.decisionId}
            className={`${styles.historyItem} ${isEffective ? styles.historyEffective : ""}`}
          >
            <div className={styles.historyTop}>
              <DecisionBadge decision={item.decision} />
              <span className={styles.sequence}>#{item.decisionSequence}</span>
              {isEffective && <span className={styles.effectiveTag}>Efetiva</span>}
            </div>
            <dl className={styles.metaGrid}>
              <div>
                <dt>Data e hora</dt>
                <dd>{formatDateTime(item.decidedAtUtc)}</dd>
              </div>
              <div>
                <dt>Autor (sujeito)</dt>
                <dd className={styles.mono}>{item.decidedBySubject}</dd>
              </div>
              <div>
                <dt>Emissor</dt>
                <dd className={styles.mono}>{item.decidedByIssuer}</dd>
              </div>
              <div className={styles.fullRow}>
                <dt>Justificação</dt>
                <dd>{item.rationale && item.rationale.length > 0 ? item.rationale : "—"}</dd>
              </div>
            </dl>
          </li>
        );
      })}
    </ol>
  );
}

export default function AnalysisDecisionPanel({ analysisId }: { analysisId: string }) {
  const { stateQuery, historyQuery, decisionMutation } = useAnalysisDecision(analysisId);
  const [mode, setMode] = useState<ActionMode>(null);
  const [rationale, setRationale] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const state = stateQuery.data;
  const history = historyQuery.data;
  const submitting = decisionMutation.isPending;

  const allowConfirm = state ? canConfirm(state.materializationReadiness) : false;
  const allowReject = state ? canReject(state.materializationReadiness) : false;

  const effectiveSequence = useMemo(() => {
    if (!state || state.decision.toLowerCase() === "unconfirmed") {
      return null;
    }
    return state.decisionSequence;
  }, [state]);

  function openAction(next: ActionMode) {
    setMode(next);
    setRationale("");
    setFormError(null);
    decisionMutation.reset();
  }

  function closeAction() {
    if (submitting) {
      return;
    }
    setMode(null);
    setRationale("");
    setFormError(null);
  }

  async function submitAction() {
    if (!mode) {
      return;
    }

    if (mode === "reject" && !hasNonWhitespaceRationale(rationale)) {
      setFormError("A rejeição exige uma justificação não vazia.");
      return;
    }

    setFormError(null);

    try {
      await decisionMutation.mutateAsync({
        decision: mode === "confirm" ? "confirmed" : "rejected",
        rationale: mode === "confirm"
          ? rationale.length > 0
            ? rationale
            : undefined
          : rationale
      });
      setMode(null);
      setRationale("");
    } catch {
      // Erro mostrado abaixo via decisionMutation.error
    }
  }

  if (stateQuery.isLoading || historyQuery.isLoading) {
    return (
      <div className={styles.panel} aria-busy="true">
        <p>A carregar a decisão da análise…</p>
      </div>
    );
  }

  if (stateQuery.isError) {
    return (
      <div className={styles.panel}>
        <Alert tone="danger" role="alert">
          {userMessageForApiError(stateQuery.error)}
        </Alert>
      </div>
    );
  }

  if (!state) {
    return null;
  }

  return (
    <div className={styles.panel}>
      <CurrentState state={state} />

      <section className={styles.section} aria-labelledby="decision-actions-title">
        <div className={styles.sectionHeader}>
          <div>
            <p className={styles.eyebrow}>AÇÕES</p>
            <h2 id="decision-actions-title">Confirmar ou rejeitar</h2>
          </div>
        </div>

        <div className={styles.actions}>
          <button
            type="button"
            className={styles.primaryButton}
            disabled={!allowConfirm || submitting}
            onClick={() => openAction("confirm")}
          >
            Confirmar análise
          </button>
          <button
            type="button"
            className={styles.dangerButton}
            disabled={!allowReject || submitting}
            onClick={() => openAction("reject")}
          >
            Rejeitar análise
          </button>
        </div>

        {!allowConfirm && allowReject && (
          <p className={styles.hint}>
            Esta análise está bloqueada: apenas a rejeição está disponível.
          </p>
        )}
      </section>

      <section className={styles.section} aria-labelledby="decision-history-title">
        <div className={styles.sectionHeader}>
          <div>
            <p className={styles.eyebrow}>HISTÓRICO</p>
            <h2 id="decision-history-title">Decisões anteriores</h2>
          </div>
        </div>

        {historyQuery.isError ? (
          <Alert tone="danger" role="alert">
            {userMessageForApiError(historyQuery.error)}
          </Alert>
        ) : (
          <HistoryList
            items={history?.items ?? []}
            effectiveSequence={effectiveSequence}
          />
        )}
      </section>

      <Dialog
        open={mode !== null}
        title={mode === "reject" ? "Rejeitar análise" : "Confirmar análise"}
        description={
          mode === "reject"
            ? "A rejeição fica registada no histórico append-only. A justificação é obrigatória."
            : "Confirme explicitamente antes de registar a decisão. A justificação é opcional."
        }
        onClose={closeAction}
        footer={
          <>
            <button
              type="button"
              className={styles.ghostButton}
              onClick={closeAction}
              disabled={submitting}
            >
              Cancelar
            </button>
            <button
              type="button"
              className={mode === "reject" ? styles.dangerButton : styles.primaryButton}
              onClick={() => void submitAction()}
              disabled={
                submitting ||
                (mode === "reject" && !hasNonWhitespaceRationale(rationale))
              }
            >
              {submitting
                ? "A submeter…"
                : mode === "reject"
                  ? "Confirmar rejeição"
                  : "Confirmar decisão"}
            </button>
          </>
        }
      >
        <label className={styles.field} htmlFor="decision-rationale">
          <span>
            Justificação
            {mode === "reject" ? " (obrigatória)" : " (opcional)"}
          </span>
          <textarea
            id="decision-rationale"
            rows={4}
            maxLength={2000}
            value={rationale}
            onChange={(event) => setRationale(event.target.value)}
            disabled={submitting}
          />
        </label>

        {formError && (
          <Alert tone="danger" role="alert">
            {formError}
          </Alert>
        )}
        {decisionMutation.isError && (
          <Alert tone="danger" role="alert">
            {userMessageForApiError(decisionMutation.error)}
          </Alert>
        )}
      </Dialog>
    </div>
  );
}
