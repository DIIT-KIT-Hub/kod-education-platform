import { getRequestConfig } from "next-intl/server";
import { routing } from "./routing";

export default getRequestConfig(async ({ requestLocale }) => {
  let locale = await requestLocale;

  if (!locale || !routing.locales.includes(locale)) {
    locale = routing.defaultLocale;
  }

  return {
    locale,
    messages: {
      AuthLayout: (await import(`../../messages/auth/layout/${locale}.json`))
        .default,
      Login: (await import(`../../messages/auth/login/${locale}.json`)).default,
      Verification: (
        await import(`../../messages/auth/verification/${locale}.json`)
      ).default,
      Inputs: (await import(`../../messages/inputs/${locale}.json`)).default,
    },
  };
});
