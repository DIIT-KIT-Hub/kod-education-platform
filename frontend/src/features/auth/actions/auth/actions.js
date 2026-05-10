// SSR
"use server";

// Imports
import { getCookie } from "@/shared/services/cookieService";
import { getUserAuthInfoAsync } from "../../services/authService";

/**
 * Static configuration of application routes with required permissions.
 *
 * Each route defines:
 * - path: URL path of the page
 * - permission: required permission to access the route
 * - labelKey: translation key for sidebar label
 */
export const ROUTES_CONFIG = [
  {
    path: "/",
    permission: "Home.Read",
    labelKey: "sidebar.home",
  },
  {
    path: "/users",
    permission: "Users.Read",
    labelKey: "sidebar.users",
  },
  {
    path: "/settings",
    permission: "Settings.Read",
    labelKey: "sidebar.settings",
  },
  {
    path: "/documents",
    permission: "Documents.Read",
    labelKey: "sidebar.documents",
  },
];

/**
 * Retrieves authenticated user system information and builds
 * permission-based navigation structure.
 *
 * Flow:
 * - Reads access token from cookies
 * - Fetches user auth info from backend
 * - Builds route list based on role/permissions
 *
 * Rules:
 * - Admin users receive all routes
 * - Non-admin users receive filtered routes based on permissions
 *
 * @async
 * @function getAuthSystemInfoAsync
 *
 * @returns {Promise<{
 *   role: string,
 *   permissions: string[],
 *   routes: Array<{path: string, permission: string, labelKey: string}>
 * } | null>}
 * Returns authenticated user system info or null if unauthorized/error.
 */
export async function getAuthSystemInfoAsync() {
  const accessToken = await getCookie("access_token");

  if (!accessToken) {
    return null;
  }

  try {
    const user = await getUserAuthInfoAsync();

    const isAdmin = user.role === "Admin";

    const routes = isAdmin
      ? ROUTES_CONFIG
      : ROUTES_CONFIG.filter((r) => user.permissions.includes(r.permission));

    return {
      role: user.role,
      permissions: user.permissions,
      routes,
    };
  } catch (e) {
    console.error("auth error:", e);
    return null;
  }
}
