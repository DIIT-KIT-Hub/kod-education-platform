// Imports
import { api } from "@/shared/api/fetchInstance";
import { getCookie } from "@/shared/services/cookieService";

export async function getUserAuthInfoAsync() {
  try {
    const accessToken = await getCookie("access_token");

    return await api.get("/auth/me", {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    });
  } catch (error) {
    throw error;
  }
}

/**
 * Authenticates a user with email and password.
 *
 * Sends login credentials to the backend and returns authentication tokens
 * or user session data depending on API implementation.
 *
 * @param {Object} data - Login credentials
 * @param {string} data.email - User email address
 * @param {string} data.password - User password
 *
 * @returns {Promise<any>} API response containing auth data (e.g. access/refresh tokens)
 * @throws {Error} Throws error if authentication fails
 */
export async function loginAsync(data) {
  try {
    return await api.post("/auth/login", data);
  } catch (error) {
    throw error;
  }
}

/**
 * Refreshes authentication tokens using a refresh token.
 *
 * Used to obtain a new access token when the current one expires.
 *
 * @param {string} refreshToken - Valid refresh token
 *
 * @returns {Promise<any>} API response containing new access/refresh tokens
 * @throws {Error} Throws error if refresh token is invalid or expired
 */
export async function refreshTokenAsync(refreshToken) {
  try {
    return await api.post("/auth/refresh", {
      refreshToken: refreshToken,
    });
  } catch (error) {
    throw error;
  }
}
