import { api } from "@/shared/api/fetchInstance";

import { cookies } from "next/headers";

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
    const cookieStore = await cookies();
    const token = cookieStore.get("verification_token")?.value;

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
