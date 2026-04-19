/**
 * Generic HTTP request handler based on native fetch.
 *
 * Handles:
 * - JSON and FormData payloads
 * - Automatic JSON serialization
 * - API base URL prefixing
 * - Error normalization with status + parsed body
 * - Automatic response parsing (JSON or text)
 *
 * @param {string} endpoint - API endpoint (relative path)
 * @param {Object} [options={}] - Request configuration options
 * @param {Object|FormData} [options.body] - Request payload
 * @param {HeadersInit} [options.headers] - Custom request headers
 * @returns {Promise<any>} Parsed response data (JSON or text)
 *
 * @throws {Error & { status?: number, data?: any }} Throws enriched error object when response is not OK
 */
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
    `${process.env.NEXT_PUBLIC_ASP_NET_API_URL}${endpoint}`,
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

/**
 * HTTP API client wrapper.
 *
 * Provides simplified methods for common HTTP operations:
 * GET, POST, PUT, DELETE.
 *
 * Each method internally uses the shared `fetcher` function.
 */
export const api = {
  /**
   * Sends a GET request.
   *
   * @param {string} endpoint - API endpoint
   * @param {Object} [options] - Request options
   * @returns {Promise<any>}
   */
  get: (endpoint, options) => fetcher(endpoint, { ...options, method: "GET" }),

   /**
   * Sends a POST request.
   *
   * @param {string} endpoint - API endpoint
   * @param {Object|FormData} body - Request payload
   * @param {Object} [options] - Request options
   * @returns {Promise<any>}
   */
  post: (endpoint, body, options) =>
    fetcher(endpoint, { ...options, method: "POST", body }),

  /**
   * Sends a PUT request.
   *
   * @param {string} endpoint - API endpoint
   * @param {Object|FormData} body - Request payload
   * @param {Object} [options] - Request options
   * @returns {Promise<any>}
   */
  put: (endpoint, body, options) =>
    fetcher(endpoint, { ...options, method: "PUT", body }),

  /**
   * Sends a DELETE request.
   *
   * @param {string} endpoint - API endpoint
   * @param {Object} [options] - Request options
   * @returns {Promise<any>}
   */
  delete: (endpoint, options) =>
    fetcher(endpoint, { ...options, method: "DELETE" }),
};
