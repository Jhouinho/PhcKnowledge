import { useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import {
  BookOpen,
  ChevronRight,
  CircleDot,
  Command,
  House,
  Menu,
  X
} from "lucide-react";
import styles from "./PortalShell.module.css";

export default function PortalShell() {
  const [mobileNavOpen, setMobileNavOpen] = useState(false);

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
              <strong>Visão geral</strong>
            </div>
          </div>
          <div className={styles.topbarRight}>
            <span className={styles.previewLabel}>
              <CircleDot size={13} />
              Pré-visualização
            </span>
            <div className={styles.avatar} aria-label="Utilizador não autenticado">
              <Command size={16} />
            </div>
          </div>
        </header>

        <main className={styles.content}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
