// Imports
import { cookies } from "next/headers";

/**
 * Retrieves a cookie value by name from the Next.js server-side cookies store.
 *
 * @param {string} name - The name of the cookie to retrieve.
 * @returns {Promise<string|undefined>} The cookie value if found, otherwise undefined.
 */
export async function getCookie(name) {
  const cookieStore = await cookies();

  return cookieStore.get(name)?.value;
}

/**
 * Sets a cookie in the Next.js server-side cookies store.
 *
 * @param {string} name - The name of the cookie.
 * @param {string} value - The value to store in the cookie.
 * @param {import("next/headers").CookieSerializeOptions} options - Cookie configuration options (e.g. expires, path, secure).
 * @returns {Promise<void>}
 */
export async function setCookie(name, value, options) {
  const cookieStore = await cookies();

  cookieStore.set(name, value, options);
}

/**
 * Removes a cookie from the Next.js server-side cookies store.
 *
 * @param {string} name - The name of the cookie to delete.
 * @returns {Promise<void>}
 */
export async function removeCookie(name) {
  const cookieStore = await cookies();

  cookieStore.delete(name);
}
