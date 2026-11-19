import { Route, Routes } from "react-router-dom";
import PortalShell from "./components/PortalShell";
import LoginPage from "./features/auth/LoginPage";
import RequireAuth from "./features/auth/RequireAuth";
import AnalysisDecisionPage from "./features/package-import-analysis-decision/AnalysisDecisionPage";
import HomePage from "./pages/HomePage";

function NotFoundPage() {
  return (
    <section className="not-found" aria-labelledby="not-found-title">
      <span className="not-found-label">Erro 404</span>
      <h1 id="not-found-title">Esta página ainda não existe.</h1>
      <p>O endereço não corresponde a uma área disponível no Portal.</p>
    </section>
  );
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<PortalShell />}>
        <Route index element={<HomePage />} />
        <Route
          path="analyses/:analysisId"
          element={
            <RequireAuth>
              <AnalysisDecisionPage />
            </RequireAuth>
          }
        />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}
