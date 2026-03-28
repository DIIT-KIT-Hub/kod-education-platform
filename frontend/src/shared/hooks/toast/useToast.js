import toast from "react-hot-toast";

export const useToast = () => {
  const success = (message) => {
    toast.success(message);
  };

  const error = (message) => {
    toast.error(message);
  };

  const loading = (message) => {
    return toast.loading(message);
  };

  const dismiss = (toastId) => {
    toast.dismiss(toastId);
  };

  const serverError = () => {
    toast.error(tinfo("server_error_occured"));
  };

  const promise = (asyncFunction, messages = {}) => {
    return toast.promise(asyncFunction, {
      loading: messages.loading,
      success: messages.success,
      error: (err) => err.message,
    });
  };

  return { success, error, loading, dismiss, serverError, promise };
};
