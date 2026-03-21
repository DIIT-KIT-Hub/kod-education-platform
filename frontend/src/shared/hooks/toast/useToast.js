import toast from "react-hot-toast";

export const useToast = () => {
  const success = (message) => {
    toast.success(tinfo(message));
  };

  const error = (message) => {
    toast.error(tinfo(message));
  };

  const warning = (message) => {
    toast(tinfo(message), {
      icon: "⚠️",
    });
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

  return { success, error, warning, serverError, promise };
};
