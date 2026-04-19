// Imports
import toast from "react-hot-toast";

/**
 * Custom toast hook built on top of react-hot-toast.
 *
 * Provides a simplified API for displaying success, error, loading,
 * and promise-based notifications.
 *
 * @returns {{
 *  success: (message: string) => void,
 *  error: (message: string) => void,
 *  loading: (message: string) => string,
 *  dismiss: (toastId?: string) => void,
 *  promise: (asyncFunction: Promise<any>, messages?: {
 *    loading?: string,
 *    success?: string
 *  }) => Promise<any>
 * }}
 */
export const useToast = () => {

  /**
   * Shows a success toast notification.
   *
   * @param {string} message - Message to display.
   */
  const success = (message) => {
    toast.success(message);
  };

  /**
   * Shows an error toast notification.
   *
   * @param {string} message - Error message to display.
   */
  const error = (message) => {
    toast.error(message);
  };

   /**
   * Shows a loading toast notification.
   *
   * @param {string} message - Loading message to display.
   * @returns {string} Toast ID for later dismissal or update.
   */
  const loading = (message) => {
    return toast.loading(message);
  };

   /**
   * Dismisses a specific toast or all toasts if no ID is provided.
   *
   * @param {string} [toastId] - Optional toast ID to dismiss.
   */
  const dismiss = (toastId) => {
    toast.dismiss(toastId);
  };

   /**
   * Handles promise-based toast notifications.
   *
   * Automatically displays loading, success, and error states
   * based on the provided async function.
   *
   * @param {Promise<any>} asyncFunction - Promise or async function to track.
   * @param {{loading?: string, success?: string}} [messages] - Toast messages.
   * @returns {Promise<any>} Result of the async function.
   */
  const promise = (asyncFunction, messages = {}) => {
    return toast.promise(asyncFunction, {
      loading: messages.loading,
      success: messages.success,
      error: (err) => err.message,
    });
  };

  return { success, error, loading, dismiss, promise };
};
