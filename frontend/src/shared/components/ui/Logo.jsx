import { getTranslations, getLocale } from "next-intl/server";
import { LogoImage } from "./LogoImage";

export async function Logo({ variant = "full" }) {
  const locale = await getLocale();
  const t = await getTranslations("logo");

  return <LogoImage variant={variant} locale={locale} alt={t("alt")} />;
}
