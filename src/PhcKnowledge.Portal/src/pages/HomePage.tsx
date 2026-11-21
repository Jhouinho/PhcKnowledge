import {
  ArrowUpRight,
  Boxes,
  Braces,
  Layers3,
  ShieldCheck
} from "lucide-react";
import styles from "./HomePage.module.css";

const foundations = [
  {
    icon: Layers3,
    title: "Estrutura modular",
    description: "Uma base React organizada por aplicação, componentes e funcionalidades."
  },
  {
    icon: ShieldCheck,
    title: "Segurança no Host",
    description: "A autenticação e a autorização permanecem sob responsabilidade do ASP.NET Core."
  },
  {
    icon: Braces,
    title: "Contratos reais",
    description: "A interface será ligada às APIs existentes, sem inventar dados ou capacidades."
  }
];

export default function HomePage() {
  return (
    <div className={styles.page}>
      <div className={styles.pageIntro}>
        <div className={styles.introCopy}>
          <div className={styles.eyebrow}>
            <span className={styles.eyebrowLine} />
            PLATAFORMA DE CONHECIMENTO TÉCNICO
          </div>
          <h1>O conhecimento PHC, <span>com contexto.</span></h1>
          <p className={styles.lead}>
            Um espaço para organizar, relacionar e reutilizar conhecimento técnico
            construído a partir da experiência real com PHC.
          </p>
        </div>
        <div className={styles.introOrb} aria-hidden="true">
          <div className={styles.orbRing} />
          <div className={styles.orbCore}>
            <Boxes size={34} strokeWidth={1.2} />
          </div>
          <span className={styles.orbNode} />
          <span className={styles.orbNodeSecondary} />
        </div>
      </div>

      <div className={styles.phaseStrip}>
        <div className={styles.phaseStatus}>
          <span className={styles.statusDot} />
          <span>INCREMENTO A</span>
        </div>
        <div className={styles.phaseDivider} />
        <p>Fundação do frontend</p>
        <span className={styles.phaseNote}>Estrutura e linguagem visual</span>
      </div>

      <section className={styles.foundationSection} aria-labelledby="foundation-title">
        <div className={styles.sectionHeading}>
          <div>
            <span className={styles.sectionEyebrow}>PRINCÍPIOS DA BASE</span>
            <h2 id="foundation-title">Construído para evoluir.</h2>
          </div>
          <span className={styles.sectionIndex}>01 / 03</span>
        </div>

        <div className={styles.foundationGrid}>
          {foundations.map((item, index) => {
            const Icon = item.icon;
            return (
              <article className={styles.foundationCard} key={item.title}>
                <div className={styles.cardTopline}>
                  <span className={styles.cardIndex}>0{index + 1}</span>
                  <Icon size={19} strokeWidth={1.7} />
                </div>
                <h3>{item.title}</h3>
                <p>{item.description}</p>
                <div className={styles.cardRule} />
              </article>
            );
          })}
        </div>
      </section>

      <section className={styles.nextSection} aria-labelledby="next-title">
        <div className={styles.nextIcon}>
          <ArrowUpRight size={19} />
        </div>
        <div className={styles.nextCopy}>
          <span className={styles.sectionEyebrow}>PRÓXIMA ETAPA</span>
          <h2 id="next-title">Integração com o Host</h2>
          <p>
            No Incremento B, o ASP.NET Core passará a servir a aplicação e as rotas
            do Portal, mantendo a API e a interface na mesma origem.
          </p>
        </div>
        <span className={styles.nextTag}>A SEGUIR</span>
      </section>

      <footer className={styles.pageFooter}>
        <span>PHC KNOWLEDGE</span>
        <span>Conhecimento técnico. Contexto preservado.</span>
      </footer>
    </div>
  );
}
