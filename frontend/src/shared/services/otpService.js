// Imports
import { api } from "../api/fetchInstance";
import { getCookie } from "./cookieService";

/**
 * Sends an OTP code to the user's email for verification purposes.
 *
 * This function retrieves a `verification_token` from cookies and uses it
 * in the Authorization header to authenticate the request.
 *
 * @param {string} email - The email address to which the OTP will be sent.
 * @returns {Promise<any>} API response containing OTP send status.
 * @throws {Error} Throws an error if the request fails or token is invalid.
 */
export async function sendOtpForVerificationAsync(email) {
  try {
    const verificationToken = await getCookie("verification_token");

    return await api.get(`/otp/send?email=${email}`, {
      headers: {
        Authorization: `Bearer ${verificationToken}`,
      },
    });
  } catch (error) {
    throw error;
  }
}
