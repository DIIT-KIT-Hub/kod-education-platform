import createMiddleware from "next-intl/middleware";
import { routing } from "./i18n/routing";
import { NextResponse } from "next/server";
import { refreshTokenAsync } from "./features/auth/services/authService";

const intlMiddleware = createMiddleware(routing);

export default async function middleware(request) {
  const { pathname } = request.nextUrl;

  const accessToken = request.cookies.get("access_token")?.value;

  const isAuthPage =
    pathname.includes("/auth/login") || pathname.includes("/auth/verification");

  if (!accessToken && !isAuthPage) {
    const refreshToken = request.cookies.get("refresh_token")?.value;

    if (refreshToken) {
      try {
        const response = await refreshTokenAsync(refreshToken);

        const res = NextResponse.next();

        res.cookies.set("access_token", response.accessToken, {
          expires: new Date(response.accessTokenExpiresAt),
          path: "/",
          secure: true,
          sameSite: "strict",
        });

        res.cookies.set("refresh_token", response.refreshToken, {
          expires: new Date(response.refreshTokenExpiresAt),
          path: "/",
          secure: true,
          sameSite: "strict",
        });

        return res;
      } catch (error) {
        console.error(error);
      }
    }

    const loginUrl = new URL("/auth/login", request.url);

    return NextResponse.redirect(loginUrl);
  }

  return intlMiddleware(request);
}

export const config = {
  matcher: ["/((?!api|_next|.*\\..*).*)"],
};
