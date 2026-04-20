// Imports
import createMiddleware from "next-intl/middleware";
import { routing } from "./i18n/routing";
import { NextResponse } from "next/server";
import { refreshTokenAsync } from "./features/auth/services/authService";

/**
 * Internationalization (next-intl) middleware instance.
 * Handles locale-based routing according to the app routing config.
 */
const intlMiddleware = createMiddleware(routing);

/**
 * Next.js Edge Middleware
 *
 * Handles:
 * - Authentication check using access/refresh tokens from cookies
 * - Automatic token refresh when access token is missing but refresh token exists
 * - Redirect to login page if user is unauthenticated
 * - Delegation to next-intl middleware for i18n routing
 *
 * Flow:
 * 1. Extract access token from cookies
 * 2. If no access token and user is not on auth pages:
 *    - Try to refresh tokens using refresh token
 *    - If refresh succeeds → set new cookies and continue
 *    - If refresh fails → redirect to login
 * 3. If access token exists → continue to intl middleware
 *
 * @param {import("next/server").NextRequest} request - Incoming Next.js request object
 * @returns {Promise<import("next/server").NextResponse>} Modified response or redirect
 */
export default async function middleware(request) {
  // Auth check
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

/**
 * Middleware matcher configuration.
 *
 * Specifies which routes the middleware applies to.
 * Excludes:
 * - API routes
 * - Next.js internal files (_next)
 * - Static files (files with extensions)
 */
export const config = {
  matcher: ["/((?!api|_next|.*\\..*).*)"],
};
