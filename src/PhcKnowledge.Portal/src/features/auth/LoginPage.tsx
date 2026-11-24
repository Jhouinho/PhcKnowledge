import { useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "./AuthProvider";
import styles from "./LoginPage.module.css";

export default function LoginPage() {
  const { isAuthenticated, isLoading, login, loginError, clearLoginError } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const from =
    (location.state as { from?: string } | null)?.from &&
    (location.state as { from?: string }).from !== "/login"
      ? (location.state as { from: string }).from
      : "/";

  if (!isLoading && isAuthenticated) {
    return <Navigate to={from} replace />;
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    clearLoginError();
    setSubmitting(true);
    try {
      await login(email.trim(), password);
      navigate(from, { replace: true });
    } catch {
      // Erro exposto via loginError no AuthProvider.
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <section className={styles.page} aria-labelledby="login-title">
      <div className={styles.panel}>
        <p className={styles.eyebrow}>PHC KNOWLEDGE</p>
        <h1 id="login-title">Iniciar sessão</h1>
        <p className={styles.lead}>
          Utilize a conta do Portal. A sessão é mantida por cookie na mesma origem.
        </p>

        <form className={styles.form} onSubmit={onSubmit} noValidate>
          <label className={styles.field} htmlFor="login-email">
            <span>Email</span>
            <input
              id="login-email"
              name="email"
              type="email"
              autoComplete="username"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
              disabled={submitting}
            />
          </label>

          <label className={styles.field} htmlFor="login-password">
            <span>Palavra-passe</span>
            <input
              id="login-password"
              name="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
              disabled={submitting}
            />
          </label>

          {loginError && (
            <p className={styles.error} role="alert">
              {loginError}
            </p>
          )}

          <button className={styles.submit} type="submit" disabled={submitting}>
            {submitting ? "A autenticar…" : "Entrar"}
          </button>
        </form>
      </div>
    </section>
  );
}
