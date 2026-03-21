import { api } from "@/shared/api/fetchInstance";

export async function checkUserExistenceByEmailAsync(email) {
  try {
    let response = await api.get(
      `/users/checkUserExistenceByEmailAsync?email=${email}`,
    );

    return response;
  } catch (error) {
    throw error;
  }
}

export async function checkUserVerificationByEmailAsync(email) {
  try {
    let response = await api.get(
      `/users/checkUserVerificationByEmailAsync?email=${email}`,
    );

    return response;
  } catch (error) {
    throw error;
  }
}

export async function requestOtpByEmailAsync(email) {
  try {
    const response = await api.get(`/verification/otp?email=${email}`);

    return response;
  } catch (error) {
    throw error;
  }
}

export async function verifyUserAsync(data) {
  console.log(data)
  try {
    const response = await api.post("/verification/confirm", data);

    return response;
  } catch (error) {
    throw error;
  }
}
