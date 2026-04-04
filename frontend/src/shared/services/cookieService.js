import { cookies } from "next/headers";

export async function getCookie(name) {
  const cookieStore = await cookies();

  return cookieStore.get(name)?.value;
}

export async function setCookie(name, value, options) {
  const cookieStore = await cookies();

  cookieStore.set(name, value, options);
}
