import { useRouter } from "@/i18n/routing";
import { useForm } from "@/shared/hooks/forms/useForm";
import { useToast } from "@/shared/hooks/toast/useToast";
import { useState } from "react";
import { sendOtpForVerificationAsync } from "@/shared/services/otpService";
import {
  createValidateEmailVerification,
  createValidateOtpVerification,
} from "../utils/verification/validations";

import {
  generateVerificationTokenAsync,
  verifyUserAsync,
} from "../services/verificationService";
import Cookies from "js-cookie";


export function useVerificationFlow(translations) {
  const router = useRouter();
  const [step, setStep] = useState(VERIFICATION_STEP.EMAIL);
  const { promise } = useToast();

  const validateEmailStep = createValidateEmailVerification(
    translations.inputs.email.validations,
  );

  const validateOtpStep = createValidateOtpVerification(
    translations.inputs.otp.validations,
  );

  const { isLoading, formData, errors, handleChange, handleSubmit, reset } =
    useForm(
      {
        email: "",
        password: "",
        otpCode: "",
      },
      (data) => {
        if (step === VERIFICATION_STEP.EMAIL) {
          return validateEmailStep(data);
        }

        if (step === VERIFICATION_STEP.OTP) {
          return validateOtpStep(data);
        }

        return {};
      },
    );

  

  const onPasswordSubmit = async (email) => {
    const checkPasswordFlow = async () => {
      try {
        return await sendOtpForVerificationAsync(email);
      } catch (error) {
        console.error(error);

        if (error.status === 401) {
          setStep(VERIFICATION_STEP.EMAIL);

          reset();

          throw new Error(
            translations.verification.errors.incorrectVerificationToken,
          );
        }

        throw new Error(translations.verification.errors.otpNotSent);
      }
    };

    await promise(checkPasswordFlow(), {
      loading: translations.verification.password.otpPreparing,
      success: translations.verification.password.otpSent,
    });

    setStep(VERIFICATION_STEP.OTP);
  };

  const onOtpSubmit = async (data) => {
    const checkVerificationFlow = async () => {
      try {
        return await verifyUserAsync(data);
      } catch (error) {
        console.error(error);

        if (error.status === 400) {
          throw new Error(translations.verification.errors.otpCodeInvalid);
        }

        if (error.status === 401) {
          setStep(VERIFICATION_STEP.EMAIL);

          reset();

          throw new Error(
            translations.verification.errors.incorrectVerificationToken,
          );
        }

        if (error.status === 410) {
          throw new Error(translations.verification.errors.otpCodeExpired);
        }

        throw new Error(translations.verification.errors.verificationFailed);
      }
    };

    await promise(checkVerificationFlow(), {
      loading: translations.verification.otp.verifying,
      success: translations.verification.otp.verified,
    });

    Cookies.remove("verification_token", { path: "/" });

    router.push("/auth/login");
  };

  const onSubmit = async (data) => {
    switch (step) {
      case VERIFICATION_STEP.EMAIL:
        return await onEmailSubmit(data.email);
      case VERIFICATION_STEP.PASSWORD:
        return await onPasswordSubmit(data.email);
      case VERIFICATION_STEP.OTP:
        return await onOtpSubmit(data);
      default:
        return null;
    }
  };

  const buttonTexts = {
    [VERIFICATION_STEP.EMAIL]:
      translations.verification.button.startVerification,
    [VERIFICATION_STEP.PASSWORD]: translations.verification.button.proceedOtp,
    [VERIFICATION_STEP.OTP]: translations.verification.button.verify,
  };

  return {
    step,
    formData,
    errors,
    isLoading,
    buttonText: buttonTexts[step],
    handleChange,
    handleSubmit: handleSubmit(onSubmit),
  };
}
