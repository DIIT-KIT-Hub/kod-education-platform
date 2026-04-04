"use client";

import React, { useActionState, useEffect, useState } from "react";
import Email from "./Email";
import Password from "./Password";
import Otp from "@/features/auth/components/verification/Otp";
import SubmitButton from "@/shared/components/buttons/submit/SubmitButton";
import { verificationAction } from "../../actions/verification/actions";
import { useRouter } from "@/i18n/routing";
import { useToast } from "@/shared/hooks/toast/useToast";

const initialState = {
  inputs: {
    email: { value: "", error: "" },
    password: { value: "", error: "" },
    otp: { value: "", error: "" },
  },
  status: 0,
  step: 1,
  timestamp: Date.now(),
};

function VerificationForm({ translations }) {
  const router = useRouter();

  const { success, error } = useToast();

  const [state, formAction, isPending] = useActionState(
    verificationAction,
    initialState,
  );

  const getButtonText = () => {
    switch (state.step) {
      case 1:
        return translations.verification.email.startVerification;
      case 2:
        return translations.verification.password.proceedOtp;
      case 3:
        return translations.verification.otp.verify;
      default:
        return "";
    }
  };

  const getButtonLoadingText = () => {
    switch (state.step) {
      case 1:
        return translations.verification.email.checkingUser;
      case 2:
        return translations.verification.password.checkingOtp;
      case 3:
        return translations.verification.otp.verifying;
      default:
        return "";
    }
  };

  useEffect(() => {
    if (state.status === 0) {
      return;
    }

    if (state.status === 200 && state.step === 2) {
      success(translations.verification.email.checkingUserSuccess);
      return;
    }

    if (state.step === 1) {
      switch (state.status) {
        case 400: {
          break;
        }
        case 404: {
          error(translations.verification.errors.userNotFound);
          break;
        }
        case 409: {
          error(translations.verification.errors.userAlreadyVerified);

          router.push("/auth/login");
          break;
        }
        default: {
          error(translations.verification.errors.tokenGenerationError);
          break;
        }
      }
    }
  }, [state.timestamp]);

  return (
    <form action={formAction}>
      {state.step === 1 && (
        <Email
          state={{
            value: state.inputs.email.value,
            error: state.inputs.email.error,
            status: state.status,
            timestamp: state.timestamp,
          }}
          translations={translations}
        />
      )}
      {state.step === 2 && (
        <Password
          state={{
            value: state.inputs.password.value,
            error: state.inputs.password.error,
            status: state.status,
            timestamp: state.timestamp,
          }}
          translations={translations}
        />
      )}
      {/* {step === VERIFICATION_STEP.OTP && (
        <Otp
          translations={translations}
          email={formData.email}
          password={formData.password}
        />
      )} */}
      <SubmitButton
        text={getButtonText()}
        loadingText={getButtonLoadingText()}
        isPending={isPending}
      />
    </form>
  );
}

export default VerificationForm;
