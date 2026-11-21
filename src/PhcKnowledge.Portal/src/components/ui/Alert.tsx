import type { ReactNode } from "react";
import styles from "./Alert.module.css";

type AlertTone = "info" | "danger" | "success";

export default function Alert({
  tone = "info",
  children,
  role = "status"
}: {
  tone?: AlertTone;
  children: ReactNode;
  role?: "status" | "alert";
}) {
  return (
    <div className={`${styles.alert} ${styles[tone]}`} role={role}>
      {children}
    </div>
  );
}
