import "./globals.css";
import localFont from "next/font/local";
import { Toaster } from "react-hot-toast";
import { getLocale } from "next-intl/server";
import { NextIntlClientProvider } from "next-intl";

const mabryProBold = localFont({
  src: "../../public/fonts/MabryPro-Bold.woff",
  variable: "--font-mabry-bold",
});

const mabryProRegular = localFont({
  src: "../../public/fonts/MabryPro-Regular.woff",
  variable: "--font-mabry-regular",
});

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
