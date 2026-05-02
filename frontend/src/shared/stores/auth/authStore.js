import { create } from "zustand";

export const useAuthStore = create((set, get) => ({
  roles: [],
  permissions: [],
  isLoaded: false,

  setAuth: ({ roles, permissions }) =>
    set({
      roles: roles || [],
      permissions: permissions || [],
      isLoaded: true,
    }),

  clearAuth: () =>
    set({
      roles: [],
      permissions: [],
      isLoaded: false,
    }),

  hasPermission: (p) => get().permissions.includes(p),

  hasRole: (r) => get().roles.includes(r),
}));
