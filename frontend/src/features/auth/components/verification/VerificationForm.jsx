// CSR
"use client";

// Imports
import { useActionState, useEffect, useState } from "react";
import Email from "./Email";
import Password from "./Password";
import Otp from "@/features/auth/components/verification/Otp";
import SubmitButton from "@/shared/components/buttons/submit/SubmitButton";
import { verificationAction } from "../../actions/verification/actions";
import { useRouter } from "@/i18n/routing";
import { useToast } from "@/shared/hooks/toast/useToast";
import { VERIFICATION_STEP } from "../../utils/constants/constants";

// Initial form state
const initialState = {
  inputs: {
    email: { value: "", error: "" },
    password: { value: "", error: "" },
    otpCode: { value: "", error: "" },
  },
  status: 0,
  step: VERIFICATION_STEP.EMAIL,
  timestamp: Date.now(),
  expiresAt: null,
  intent: null,
};

/**
 * Multi-step verification form component.
 *
 * Handles a complete authentication/verification flow consisting of:
 * - Email step
 * - Password step
 * - OTP verification step
 *
 * Features:
 * - Step-based rendering
 * - Server action integration via useActionState
 * - Toast notifications for success/error states
 * - OTP expiration handling and resend flow
 * - Automatic navigation on success
 * - Status-based error handler mapping per step
 *
 * @param {Object} props - Component props
 * @param {Object} props.t - Translation object containing all localized strings
 *
 * @returns {JSX.Element} Rendered multi-step verification form
 */
function VerificationForm({ t }) {
  const [otpExpired, setOtpExpired] = useState(false);
  const router = useRouter();

  const { success, error } = useToast();

  const [state, formAction, isPending] = useActionState(
    verificationAction,
    initialState,
  );

  const handlers = {
    1: {
      400: () => {},
      404: () => error(t.verification.email.errors.userNotFound),
      409: () => {
        error(t.verification.email.errors.userAlreadyVerified);
        router.push("/auth/login");
      },
    },
    2: {
      400: () => {},
      401: () => {
        error(t.verification.errors.incorrectVerificationToken);

        setTimeout(() => {
          window.location.reload();
        }, 1000);
      },
    },
    3: {
      400: () => error(t.verification.otp.errors.otpCodeInvalid),
      401: () => {
        error(t.verification.errors.incorrectVerificationToken);

        setTimeout(() => {
          window.location.reload();
        }, 1000);
      },
      410: () => {
        error(t.verification.otp.errors.otpCodeExpired);

        setOtpExpired(true);
      },
      422: () => {},
    },
    success: {
      2: () => success(t.verification.email.success),
      3: () => success(t.verification.password.success),
      0: () => {
        success(t.verification.otp.success);
        router.push("/auth/login");
      },
    },
    fallback: () => error(t.verification.errors.verificationFailed),
  };

  const getButtonText = () => {
    switch (state.step) {
      case 1:
        return t.verification.email.button;
      case 2:
        return t.verification.password.button;
      case 3:
        return t.verification.otp.buttonVerify;
      default:
        return "";
    }
  };

  const getButtonLoadingText = () => {
    switch (state.step) {
      case VERIFICATION_STEP.EMAIL:
        return t.verification.email.verifying;
      case VERIFICATION_STEP.PASSWORD:
        return t.verification.password.verifying;
      case VERIFICATION_STEP.OTP:
        return t.verification.otp.verifying;
      default:
        return "";
    }
  };

  useEffect(() => {
    if (!state.status) {
      return;
    }

    if (state.intent === "resend" && state.status === 200) {
      success(t.verification.otp.resend);
      return;
    }

    if (state.status === 200) {
      handlers.success[state.step]?.();
      return;
    }

    const stepHandler = handlers[state.step]?.[state.status];

    if (stepHandler) {
      stepHandler();
    } else {
      handlers.fallback();
    }
  }, [state.timestamp]);

  useEffect(() => {
    if (state.expiresAt) {
      setOtpExpired(false);
    }
  }, [state.expiresAt]);

  return (
    <form action={formAction}>
      {state.step === VERIFICATION_STEP.EMAIL && (
        <Email
          state={{
            value: state.inputs.email.value,
            error: state.inputs.email.error,
            status: state.status,
            timestamp: state.timestamp,
          }}
          t={{
            title: t.verification.email.title,
            placeholder: t.inputs.email.placeholder,
          }}
        />
      )}
      {state.step === VERIFICATION_STEP.PASSWORD && (
        <Password
          state={{
            value: state.inputs.password.value,
            error: state.inputs.password.error,
            status: state.status,
            timestamp: state.timestamp,
          }}
          t={{
            title: t.verification.password.title,
            password: t.inputs.password,
          }}
        />
      )}
      {state.step === VERIFICATION_STEP.OTP && (
        <Otp
          state={{
            value: state.inputs.otpCode.value,
            error: state.inputs.otpCode.error,
            status: state.status,
            timestamp: state.timestamp,
            expiresAt: state.expiresAt,
          }}
          t={{
            title: t.verification.otp.title,
            notReceived: t.verification.otp.notReceived,
            otpCodeExpired: t.verification.otp.otpCodeExpired,
            resend: t.verification.otp.resend,
          }}
          setOtpExpired={setOtpExpired}
        />
      )}
      <SubmitButton
        text={otpExpired ? t.verification.otp.buttonResend : getButtonText()}
        loadingText={
          otpExpired ? t.verification.otp.resending : getButtonLoadingText()
        }
        isPending={isPending}
      />
    </form>
  );
}

// Component export
export default VerificationForm;
