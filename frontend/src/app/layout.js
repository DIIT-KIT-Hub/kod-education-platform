// Imports
import "./globals.css";
import localFont from "next/font/local";
import { Toaster } from "react-hot-toast";
import { getLocale } from "next-intl/server";
import { NextIntlClientProvider } from "next-intl";

// Fonts
const mabryProBold = localFont({
  src: "../../public/fonts/MabryPro-Bold.woff",
  variable: "--font-mabry-bold",
});

const mabryProRegular = localFont({
  src: "../../public/fonts/MabryPro-Regular.woff",
  variable: "--font-mabry-regular",
});

/**
 * Root application layout for Next.js App Router.
 *
 * Responsibilities:
 * - Loads and applies custom local fonts
 * - Resolves current locale using next-intl
 * - Provides internationalization context via NextIntlClientProvider
 * - Initializes global toast notifications (react-hot-toast)
 * - Wraps all application pages with global providers
 *
 * This layout is server-rendered and executed once per request
 * to ensure correct locale and provider initialization.
 *
 * @param {Object} props - Layout props
 * @param {React.ReactNode} props.children - Nested application pages/components
 *
 * @returns {JSX.Element} Root HTML layout structure
 */
export default async function RootLayout({ children }) {
  const locale = await getLocale();

  return (
    <html lang={locale}>
      <body className={`${mabryProBold.variable} ${mabryProRegular.variable}`}>
        <NextIntlClientProvider locale={locale}>
          <Toaster
            position="top-center"
            reverseOrder={false}
            toastOptions={{
              className: "toaster",
            }}
          />
          {children}
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
