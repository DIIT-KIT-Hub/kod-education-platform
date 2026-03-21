"use client";

import React from "react";

import { useForm } from "@/shared/hooks/forms/useForm";

import {
  checkUserExistenceByEmailAsync,
  checkUserVerificationByEmailAsync,
  requestOtpByEmailAsync,
  verifyUserAsync,
} from "@/shared/services/authService";
import {
  createValidateEmailVerification,
  createValidateOtpVerification,
} from "../../utils/validations";

import { useToast } from "@/shared/hooks/toast/useToast";
import Email from "./Email";
import Password from "./Password";
import { useFormStep } from "@/shared/hooks/forms/useFormStep";
import Otp from "@/shared/components/otp/Otp";
import { useRouter } from "@/i18n/routing";

const VERIFICATION_STEP = {
  EMAIL: "EMAIL",
  PASSWORD: "PASSWORD",
  OTP: "OTP",
};

function VerificationForm({ translations }) {
  const validateEmailStep = createValidateEmailVerification(
    translations.inputs.email.validations,
  );

  const router = useRouter();

  const validateOtpStep = createValidateOtpVerification(
    translations.inputs.otp.validations,
  );

  const { step, setStep } = useFormStep(VERIFICATION_STEP.EMAIL);

  const { isLoading, formData, errors, handleChange, handleSubmit } = useForm(
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

  const { promise } = useToast();

  const onEmailSubmit = async (email) => {
    const checkEmailFlow = async () => {
      const exists = await checkUserExistenceByEmailAsync(email);
      if (!exists) {
        throw new Error(translations.verification.errors.userNotFound);
      }

      const isVerified = await checkUserVerificationByEmailAsync(email);
      if (isVerified) {
        throw new Error(translations.verification.errors.userAlreadyVerified);
      }

      return true;
    };

    try {
      await promise(checkEmailFlow(), {
        loading: translations.verification.email.checkingUser,
        success: translations.verification.email.checkingUserSuccess,
      });

      setStep(VERIFICATION_STEP.PASSWORD);
    } catch (error) {
      console.error("er:", error);
    }
  };

  const onPasswordSubmit = async (email) => {
    const requestOtp = async () => {
      const isSent = await requestOtpByEmailAsync(email);

      if (!isSent) {
        throw new Error(translations.verification.errors.otpNotSent);
      }

      return true;
    };

    try {
      await promise(requestOtp(), {
        loading: translations.verification.password.otpPreparing,
        success: translations.verification.password.otpSent,
      });

      setStep(VERIFICATION_STEP.OTP);
    } catch (error) {
      console.error("er:", error);
    }
  };

  const onOtpSubmit = async (data) => {
   
    const verifyUserAsync1 = async () => {
      const isVerified = await verifyUserAsync(data);

      if (!isVerified) {
        throw new Error(translations.verification.errors.verificationFailed);
      }

      return true;
    };

    try {
      await promise(verifyUserAsync1(), {
        loading: translations.verification.otp.verifying,
        success: translations.verification.otp.verified,
      });

      router.push("/auth/login");
    } catch (error) {
      console.error("er:", error);
    }
  };

  const onSubmit = async (data) => {
    switch (step) {
      case VERIFICATION_STEP.EMAIL: {
        return await onEmailSubmit(data.email);
      }
      case VERIFICATION_STEP.PASSWORD: {
        return onPasswordSubmit(data.email);
      }
      case VERIFICATION_STEP.OTP: {
        return onOtpSubmit(data);
      }
    }
  };

  const getSubmitButtonText = () => {
    switch (step) {
      case VERIFICATION_STEP.EMAIL: {
        return translations.verification.button.startVerification;
        break;
      }
      case VERIFICATION_STEP.PASSWORD: {
        return translations.verification.button.proceedOtp;
        break;
      }
      case VERIFICATION_STEP.OTP: {
        return translations.verification.button.verify;
        break;
      }
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      {step === VERIFICATION_STEP.EMAIL && (
        <Email
          translations={{
            title: translations.verification.email.title,
            email: translations.inputs.email.placeholder,
          }}
          email={formData.email}
          handleChange={handleChange}
          error={errors.email}
        />
      )}
      {step === VERIFICATION_STEP.PASSWORD && (
        <Password
          translations={translations.verification.password}
          password={formData.password}
          handleChange={handleChange}
        />
      )}
      {step === VERIFICATION_STEP.OTP && (
        <Otp
          translations={translations.verification.otp}
          otpValue={formData.otpCode}
          handleOtpChange={(value) =>
            handleChange({ target: { name: "otpCode", value } })
          }
          error={errors.otpCode}
        />
      )}
      <button type="submit" disabled={isLoading}>
        {getSubmitButtonText()}
      </button>
    </form>
  );
}

export default VerificationForm;
