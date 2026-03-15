async function fetcher(endpoint, options = {}) {
  const { body, headers = {}, ...rest } = options;

  const finalHeaders = { ...headers };

  const config = {
    ...rest,
    headers: finalHeaders,
  };

  if (body) {
    if (body instanceof FormData) {
      config.body = body;
    } else {
      finalHeaders["Content-Type"] = "application/json";
      config.body = JSON.stringify(body);
    }
  }

  const response = await fetch(
    `${process.env.NEXT_PUBLIC_API_URL}${endpoint}`,
    config,
  );

  if (!response.ok) {
    let errorData;

    try {
      const { title, ...rest } = await response.json();
      errorData = { message: title, ...rest };
    } catch {
      errorData = { message: response.statusText };
    }

    const error = new Error(
      errorData.message ?? "An error occurred while fetching the data.",
    );

    error.status = response.status;
    error.data = errorData;

    throw error;
  }

  const contentType = response.headers.get("content-type");

  if (contentType?.includes("application/json")) {
    return response.json();
  }

  return response.text();
}

export const api = {
  get: (endpoint, options) => fetcher(endpoint, { ...options, method: "GET" }),

  post: (endpoint, body, options) =>
    fetcher(endpoint, { ...options, method: "POST", body }),

  put: (endpoint, body, options) =>
    fetcher(endpoint, { ...options, method: "PUT", body }),

  delete: (endpoint, options) =>
    fetcher(endpoint, { ...options, method: "DELETE" }),
};
