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
      Global: (await import(`../../messages/global/${locale}.json`)).default,
      Auth: (await import(`../../messages/auth/${locale}.json`))
        .default,
      Validations: (await import(`../../messages/validations/${locale}.json`))
        .default,
      Inputs: (await import(`../../messages/inputs/${locale}.json`))
        .default,
    },
  };
});
