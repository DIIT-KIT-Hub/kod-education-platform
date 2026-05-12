// Imports
import { create } from "zustand";

/**
 * Zustand store for client-side authentication state.
 *
 * Purpose:
 * - Stores user roles and permissions for UI logic
 * - Provides helper methods for access control checks
 * - Acts as a client-side cache of server-provided auth data
 *
 * IMPORTANT:
 * This store is NOT a security layer.
 * It is only used for UI/UX decisions (show/hide elements).
 * Real authorization must always be enforced on the server.
 *
 * @module useAuthStore
 */
export const useAuthStore = create((set, get) => ({
  /**
   * List of user roles (e.g. Admin, User)
   * @type {string[]}
   */
  roles: [],

  /**
   * List of user permissions (e.g. Users.Read)
   * @type {string[]}
   */
  permissions: [],

  /**
   * Indicates whether auth state has been initialized
   * @type {boolean}
   */
  isLoaded: false,

  /**
   * Sets authentication state in the store.
   *
   * @param {Object} auth
   * @param {string[]} auth.roles - User roles
   * @param {string[]} auth.permissions - User permissions
   */
  setAuth: ({ roles, permissions }) =>
    set({
      roles: roles || [],
      permissions: permissions || [],
      isLoaded: true,
    }),

  /**
   * Clears authentication state (e.g. on logout).
   */
  clearAuth: () =>
    set({
      roles: [],
      permissions: [],
      isLoaded: false,
    }),

  /**
   * Checks if user has a specific permission.
   *
   * @param {string} p - Permission name
   * @returns {boolean} True if permission exists
   */
  hasPermission: (p) => get().permissions.includes(p),

  /**
   * Checks if user has a specific role.
   *
   * @param {string} r - Role name
   * @returns {boolean} True if role exists
   */
  hasRole: (r) => get().roles.includes(r),
}));