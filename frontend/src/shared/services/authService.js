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