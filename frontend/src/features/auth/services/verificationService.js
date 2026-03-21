import { api } from "@/shared/api/fetchInstance";
import Cookies from "js-cookie";

export async function generateVerificationTokenAsync(email) {
  try {
    const verified = await api.get(`/verification/token?email=${email}`);

    return verified;
  } catch (error) {
    throw error;
  }
}

export async function verifyUserAsync(data) {
  try {
    const token = Cookies.get("verification_token");

    const response = await api.post("/verification/verify", data, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });

    return response;
  } catch (error) {
    throw error;
  }
}
