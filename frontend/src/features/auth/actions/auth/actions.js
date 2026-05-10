import { getCookie } from "@/shared/services/cookieService";
import { getUserAuthInfoAsync } from "../../services/authService";

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
