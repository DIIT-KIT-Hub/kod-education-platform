import { api } from "@/shared/api/fetchInstance";

export async function requestOtpAsync(email) {
  try {
    let result = await api.get(`/verification/otp?email=${email}`);

    return result;
  } catch (error) {
    console.error(error);
  }
}
