import { Inter } from "next/font/google";
import { NextIntlClientProvider } from "next-intl";

import { routing } from "@/i18n/routing";
import { ThemeProvider } from "@/shared/providers/ThemeProvider";
import "../globals.css";
import { locale as getRootLocale } from "next/root-params";
import { getLocale } from "next-intl/server";

const inter = Inter({
  variable: "--font-inter",
  subsets: ["latin", "cyrillic"],
});

export function generateStaticParams() {
  return routing.locales.map((locale) => ({ locale }));
}

export async function generateMetadata({}) {
  const { locale } = await getRootLocale();

  const title =
    locale === "en" ? "Educational Data Cabinet" : "Кабінет освітніх даних";
  const description =
    locale === "en"
      ? "Unified document management system for higher education"
      : "Єдина система документообігу закладу вищої освіти";

  return { title, description };
}

export default async function LocaleLayout({ children }) {
  const locale = await getLocale();

  return (
    <html lang={locale} className={inter.variable} suppressHydrationWarning>
      <body>
        <NextIntlClientProvider>
          <ThemeProvider>{children}</ThemeProvider>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
