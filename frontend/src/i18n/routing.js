// Imports
import { defineRouting } from 'next-intl/routing';
import { createNavigation } from 'next-intl/navigation';

/**
 * Internationalization routing configuration for next-intl.
 *
 * Defines supported locales and default locale for the application.
 *
 * Supported locales:
 * - English (en)
 * - Ukrainian (uk)
 *
 * Default locale:
 * - uk
 */
export const routing = defineRouting({
  locales: ['en', 'uk'],
  defaultLocale: 'uk'
});

/**
 * Next-intl navigation helpers bound to routing configuration.
 *
 * Provides locale-aware replacements for Next.js navigation utilities:
 * - Link: localized link component
 * - redirect: locale-aware redirect function
 * - usePathname: hook for current localized pathname
 * - useRouter: locale-aware router
 */
export const { Link, redirect, usePathname, useRouter } = createNavigation(routing);