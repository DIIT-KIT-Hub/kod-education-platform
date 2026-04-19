// Imports
import { api } from "@/shared/api/fetchInstance";
import { getCookie } from "@/shared/services/cookieService";

/**
 * Generates a verification token for a user based on email.
 *
 * This token is typically used to authorize further verification steps
 * such as OTP confirmation or user verification flow.
 *
 * @param {string} email - User email address
 * @returns {Promise<any>} API response containing verification token
 * @throws {Error} Throws error if request fails
 */
export async function generateVerificationTokenAsync(email) {
  try {
    return await api.get(`/verification/token?email=${email}`);
  } catch (error) {
    throw error;
  }
}

/**
 * Verifies a user using email, password, and OTP code.
 *
 * Requires a valid verification token stored in cookies.
 * The token is sent via Authorization header.
 *
 * @param {Object} data - Verification payload
 * @param {string} data.email - User email address
 * @param {string} data.password - User password
 * @param {string} data.otp - One-time password (OTP) code
 *
 * @returns {Promise<any>} API response containing verification result
 * @throws {Error} Throws error if verification fails or request is invalid
 */
export async function verifyUserAsync(data) {
  try {
    const verificationToken = await getCookie("verification_token");

    return await api.post("/verification/verify", data, {
      headers: {
        Authorization: `Bearer ${verificationToken}`,
      },
    });
  } catch (error) {
    throw error;
  }
}
