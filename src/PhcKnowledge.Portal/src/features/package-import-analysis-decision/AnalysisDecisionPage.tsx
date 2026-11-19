import { useParams } from "react-router-dom";
import AnalysisDecisionPanel from "./AnalysisDecisionPanel";
import styles from "./AnalysisDecisionPage.module.css";

const GUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export default function AnalysisDecisionPage() {
  const { analysisId = "" } = useParams();

  if (!GUID_PATTERN.test(analysisId)) {
    return (
      <section className={styles.page} aria-labelledby="invalid-analysis-title">
        <p className={styles.eyebrow}>ANÁLISE</p>
        <h1 id="invalid-analysis-title">Identificador inválido</h1>
        <p className={styles.lead}>
          O endereço deve incluir um <code>analysisId</code> GUID válido.
        </p>
      </section>
    );
  }

  return (
    <section className={styles.page} aria-labelledby="analysis-decision-title">
      <div className={styles.intro}>
        <p className={styles.eyebrow}>PACKAGE IMPORT ANALYSIS</p>
        <h1 id="analysis-decision-title">Confirmação da análise</h1>
        <p className={styles.lead}>
          Consulte o estado atual, o histórico e registe a confirmação ou rejeição
          humana desta análise.
        </p>
        <p className={styles.meta}>
          AnalysisId: <code>{analysisId}</code>
        </p>
      </div>

      <AnalysisDecisionPanel analysisId={analysisId} />
    </section>
  );
}
