"use client";
import { useTheme } from "next-themes";
import { useEffect, useState } from "react";
import Image from "next/image";

export function LogoImage({ variant, locale, alt }) {
  const { resolvedTheme } = useTheme();
  const [mounted, setMounted] = useState(false);
  useEffect(() => setMounted(true), []);

  const width = variant === "mark" ? 32 : 160;
  const height = 32;

  if (!mounted) {
    return <div style={{ width, height }} aria-hidden="true" />;
  }

  const isDark = resolvedTheme === "dark";

  let src;
  if (variant === "mark") {
    src = isDark ? "/mark-dark.svg" : "/mark-light.svg";
  } else {
    const suffix = locale === "en" ? "-en" : "";
    src = isDark ? `/logo-dark${suffix}.svg` : `/logo-light${suffix}.svg`;
  }

  return <Image src={src} alt={alt} width={width} height={height} priority />;
}
