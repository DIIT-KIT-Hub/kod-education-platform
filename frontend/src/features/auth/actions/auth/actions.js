import { getCookie } from "@/shared/services/cookieService";
import { getUserAuthInfoAsync } from "../../services/authService";

export const ROUTES_CONFIG = [
  {
    path: "/users",
    permission: "Users.Module",
    labelKey: "sidebar.users",
  },
  {
    path: "/settings",
    permission: "Settings.Module",
    labelKey: "sidebar.settings",
  },
  {
    path: "/documents",
    permission: "Documents.Module",
    labelKey: "sidebar.documents",
  },
];

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
