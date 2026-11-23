import { useState } from "react";
import { Link, NavLink, Outlet, useLocation } from "react-router-dom";
import {
  BookOpen,
  ChevronRight,
  CircleDot,
  Command,
  House,
  Menu,
  X
} from "lucide-react";
import { useAuth } from "../features/auth/AuthProvider";
import styles from "./PortalShell.module.css";

function breadcrumbLabel(pathname: string): string {
  if (pathname.startsWith("/analyses/")) {
    return "Confirmação da análise";
  }
  if (pathname === "/login") {
    return "Sessão";
  }
  return "Visão geral";
}

export default function PortalShell() {
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const { isAuthenticated, session, logout } = useAuth();
  const location = useLocation();

  const closeMobileNav = () => setMobileNavOpen(false);

  return (
    <div className={styles.portal}>
      {mobileNavOpen && (
        <button
          className={styles.scrim}
          type="button"
          aria-label="Fechar navegação"
          onClick={closeMobileNav}
        />
      )}

      <aside className={`${styles.sidebar} ${mobileNavOpen ? styles.sidebarOpen : ""}`}>
        <div className={styles.brand}>
          <div className={styles.brandMark} aria-hidden="true">
            <BookOpen size={19} strokeWidth={1.8} />
          </div>
          <div className={styles.brandText}>
            <span className={styles.brandName}>PHC Knowledge</span>
            <span className={styles.brandCaption}>TECHNICAL KNOWLEDGE</span>
          </div>
          <button
            className={styles.mobileClose}
            type="button"
            aria-label="Fechar navegação"
            onClick={closeMobileNav}
          >
            <X size={18} />
          </button>
        </div>

        <div className={styles.workspace}>
          <span className={styles.workspaceLabel}>WORKSPACE</span>
          <div className={styles.workspaceChoice}>
            <span className={styles.workspaceGlyph}>P</span>
            <span className={styles.workspaceName}>PHC Knowledge</span>
            <ChevronRight size={15} className={styles.workspaceChevron} />
          </div>
        </div>

        <nav className={styles.navigation} aria-label="Navegação principal">
          <span className={styles.navSection}>GERAL</span>
          <NavLink
            to="/"
            end
            onClick={closeMobileNav}
            className={({ isActive }) =>
              `${styles.navLink} ${isActive ? styles.navLinkActive : ""}`
            }
          >
            <House size={17} strokeWidth={1.8} />
            <span>Visão geral</span>
            <span className={styles.activeMarker} />
          </NavLink>
        </nav>

        <div className={styles.sidebarBottom}>
          <div className={styles.environment}>
            <span className={styles.environmentDot} />
            <span>Ambiente de desenvolvimento</span>
          </div>
          <div className={styles.sidebarFooter}>
            <span>PHC KNOWLEDGE</span>
            <span>0.1.0</span>
          </div>
        </div>
      </aside>

      <div className={styles.mainColumn}>
        <header className={styles.topbar}>
          <div className={styles.topbarLeft}>
            <button
              className={styles.mobileMenu}
              type="button"
              aria-label="Abrir navegação"
              aria-expanded={mobileNavOpen}
              onClick={() => setMobileNavOpen(true)}
            >
              <Menu size={19} />
            </button>
            <div className={styles.breadcrumb}>
              <span>Portal</span>
              <ChevronRight size={14} />
              <strong>{breadcrumbLabel(location.pathname)}</strong>
            </div>
          </div>
          <div className={styles.topbarRight}>
            <span className={styles.previewLabel}>
              <CircleDot size={13} />
              {isAuthenticated ? "Sessão ativa" : "Pré-visualização"}
            </span>
            {isAuthenticated ? (
              <button
                type="button"
                className={styles.avatar}
                aria-label={`Terminar sessão de ${session?.displayName ?? session?.email ?? "utilizador"}`}
                title="Terminar sessão"
                onClick={() => void logout()}
              >
                <Command size={16} />
              </button>
            ) : (
              <Link
                to="/login"
                className={styles.avatar}
                aria-label="Iniciar sessão"
                title="Iniciar sessão"
              >
                <Command size={16} />
              </Link>
            )}
          </div>
        </header>

        <main className={styles.content}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
