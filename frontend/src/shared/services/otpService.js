import Cookies from "js-cookie";
import { api } from "../api/fetchInstance";

export async function sendOtpForVerificationAsync(email) {
  try {
    const token = Cookies.get("verification_token");

    const response = await api.get(`/otp/send?email=${email}`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });

    return response;
  } catch (error) {
    throw error;
  }
}
