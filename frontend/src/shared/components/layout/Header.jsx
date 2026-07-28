import { getTranslations } from "next-intl/server";

import { Link } from "@/i18n/navigation";
import styles from "./Header.module.css";
import { Logo } from "../ui/Logo";
import { ThemeToggle } from "../ui/ThemeToggle";

export async function Header() {
  const t = await getTranslations("nav");

  return (
    <header className={styles.header}>
      <Logo variant="full" />
      <nav>
        <Link href="/documents">{t("documents")}</Link>
        <Link href="/students">{t("students")}</Link>
      </nav>
      <ThemeToggle />
    </header>
  );
}
