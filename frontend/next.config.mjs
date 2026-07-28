import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin("./src/i18n/request.js");

/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  reactCompiler: true,
  cacheComponents: true,
  experimental: {
    rootParams: true,
  },
};

export default withNextIntl(nextConfig);
