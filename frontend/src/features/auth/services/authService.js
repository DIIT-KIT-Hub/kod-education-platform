import { api } from "@/shared/api/fetchInstance";

export async function loginAsync(data) {
  try {
    const response = await api.post("/auth/login", data);

    return response;
  } catch (error) {
    throw error;
  }
}

export async function refreshTokenAsync(refreshToken) {
  try {
    const response = await api.post("/auth/refresh", {
      refreshToken: refreshToken,
    });

    return response;
  } catch (error) {
    throw error;
  }
}
