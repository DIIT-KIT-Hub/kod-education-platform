import "./globals.css";
import localFont from "next/font/local";
import { NextIntlClientProvider } from "next-intl";
import { getMessages } from "next-intl/server";
import { ToastProvider } from "@/stores/toast/ToastContext";

const mabryProBold = localFont({
  src: "../../public/fonts/MabryPro-Bold.woff",
  variable: "--font-mabry-bold",
});

const mabryProRegular = localFont({
  src: "../../public/fonts/MabryPro-Regular.woff",
  variable: "--font-mabry-regular",
});

export default async function RootLayout({ children }) {
  const { locale, messages } = await getMessages();

  return (
    <html lang={locale}>
      <body className={`${mabryProBold.variable} ${mabryProRegular.variable}`}>
        <NextIntlClientProvider messages={messages}>
          <ToastProvider>{children}</ToastProvider>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
