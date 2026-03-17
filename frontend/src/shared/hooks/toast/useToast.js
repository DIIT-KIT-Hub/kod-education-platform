import { useTranslations } from "use-intl";
import toast from "react-hot-toast";

export const useToast = () => {
  const tinfo = useTranslations("Info");

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

  return { success, error, warning, serverError };
};
