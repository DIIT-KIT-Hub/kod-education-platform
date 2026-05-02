"use client";

import { useEffect } from "react";
import { useAuthStore } from "./authStore";

export default function AuthProvider({ initialAuth, children }) {
  const setAuth = useAuthStore((s) => s.setAuth);

  useEffect(() => {
    if (initialAuth) {
      setAuth(initialAuth);
    }
  }, [initialAuth]);

  return children;
}
