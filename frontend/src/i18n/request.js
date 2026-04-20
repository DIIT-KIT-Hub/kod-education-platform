// Imports
import { getRequestConfig } from "next-intl/server";
import { routing } from "./routing";

/**
 * Next-intl request configuration factory.
 *
 * Resolves and validates the active locale for each request,
 * then dynamically loads localized message bundles based on that locale.
 *
 * Flow:
 * 1. Extract requested locale from the request context
 * 2. Validate locale against supported routing locales
 * 3. Fallback to default locale if invalid or missing
 * 4. Dynamically import translation messages per namespace
 *
 * Message namespaces:
 * - AuthLayout: authentication layout translations
 * - Login: login page translations
 * - Verification: OTP / verification page translations
 * - Inputs: shared input-related translations
 *
 * @param {Object} params
 * @param {Promise<string|undefined>} params.requestLocale - Locale requested by the user/request context
 *
 * @returns {Promise<{
 *   locale: string,
 *   messages: {
 *     AuthLayout: Record<string, string>,
 *     Login: Record<string, string>,
 *     Verification: Record<string, string>,
 *     Inputs: Record<string, string>
 *   }
 * }>} Next-intl configuration object
 */
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
