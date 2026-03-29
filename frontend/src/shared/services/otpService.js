import { api } from "../api/fetchInstance";
import { cookies } from "next/headers";

export async function sendOtpForVerificationAsync(email) {
  try {
    const cookieStore = await cookies();

    const token = cookieStore.get("verification_token")?.value;

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
