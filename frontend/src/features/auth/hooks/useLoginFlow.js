import { useRouter } from "@/i18n/routing";
import { useForm } from "@/shared/hooks/forms/useForm";
import { loginAsync } from "../services/authService";
import { useToast } from "@/shared/hooks/toast/useToast";
import Cookies from "js-cookie";
import { createValidateLogin } from "../utils/validations";

export function useLoginFlow(translations) {
  const router = useRouter();

  const { promise } = useToast();

  const validateLogin = createValidateLogin({
    ...translations.inputs.email.validations,
    ...translations.inputs.password.validations,
  });

  const { formData, errors, isLoading, handleChange, handleSubmit } = useForm(
    {
      email: "",
      password: "",
    },
    (data) => {
      return validateLogin(data);
    },
  );

  const onSubmit = async (data) => {
    const checkLoginFlow = async () => {
      try {
        return await loginAsync(data);
      } catch (error) {
        console.log(error);

        if (error.status === 401) {
          router.push("/auth/verification");

          throw new Error(translations.login.notVerified);
        }

        if (error.status === 400 || error.status === 404) {
          throw new Error(translations.login.invalidCredentials);
        }

        throw new Error(translations.login.loginFailed);
      }
    };

    const response = await promise(checkLoginFlow(data), {
      loading: translations.login.checkingLogin,
      success: translations.login.checkingLoginSuccess,
    });

    Cookies.set("access_token", response.accessToken, {
      expires: new Date(response.accessTokenExpiresAt),
      path: "/",
      secure: true,
      sameSite: "strict",
    });

    Cookies.set("refresh_token", response.refreshToken, {
      expires: new Date(response.refreshTokenExpiresAt),
      path: "/",
      secure: true,
      sameSite: "strict",
    });

    router.push("/");
  };

  return {
    formData,
    errors,
    isLoading,
    handleChange,
    handleSubmit: handleSubmit(onSubmit),
  };
}
