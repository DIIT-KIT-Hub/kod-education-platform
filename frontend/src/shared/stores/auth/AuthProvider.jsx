// CSR
"use client";

// Imports
import { useEffect } from "react";
import { useAuthStore } from "./authStore";

/**
 * AuthProvider hydrates the client-side authentication store
 * with initial server-provided authentication data.
 *
 * Responsibilities:
 * - Receives initial auth state from SSR (layout/server component)
 * - Populates Zustand auth store on client mount
 * - Keeps auth state available for UI permission checks
 *
 * NOTE:
 * This component does NOT perform any authentication itself.
 * It only hydrates already-validated server-side data.
 *
 * @component
 *
 * @param {Object} props - Component props
 * @param {Object|null} props.initialAuth - Initial authentication state from server
 * @param {string} props.initialAuth.role - User role (e.g. Admin, User)
 * @param {string[]} props.initialAuth.permissions - List of user permissions
 * @param {React.ReactNode} props.children - Application content
 *
 * @returns {JSX.Element} Wrapped application with hydrated auth state
 */
function AuthProvider({ initialAuth, children }) {
  const setAuth = useAuthStore((s) => s.setAuth);

  useEffect(() => {
    if (initialAuth) {
      setAuth(initialAuth);
    }
  }, [initialAuth]);

  return children;
}

// Auth provider export
export default AuthProvider;
