// shared/components/ThemeToggle.jsx
"use client";
import { useTheme } from "next-themes";
import { useEffect, useState } from "react";
import styles from "./ThemeToggle.module.css";

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();
  const [mounted, setMounted] = useState(false);

  useEffect(() => setMounted(true), []);

  if (!mounted) return null;

  return (
    <button
      onClick={() => setTheme(theme === "light" ? "dark" : "light")}
      className={styles.toggle}
      aria-label="Перемкнути тему"
    >
      {theme === "light" ? "🌙" : "☀️"}
    </button>
  );
}
