// CSR
"use client";

// Imports
import { useActionState, useEffect } from "react";
import Input from "@/shared/components/input/Input";
import { loginAction } from "../../actions/login/actions";
import SubmitButton from "@/shared/components/buttons/submit/SubmitButton";
import { useRouter } from "next/navigation";
import { useToast } from "@/shared/hooks/toast/useToast";

// Initial form state
const initialState = {
  email: { value: "", error: "" },
  password: { value: "", error: "" },
  status: 0,
  timestamp: Date.now(),
};

/**
 * Login form component for user authentication.
 *
 * Handles:
 * - Email and password input fields
 * - Server action submission via useActionState
 * - Error handling based on HTTP status codes
 * - Navigation to verification flow for unverified users
 * - Toast notifications for login feedback
 *
 * Status handling:
 * - 400: Invalid credentials
 * - 401: User not verified (redirects to verification page)
 * - 404: User not found
 * - 422: Validation error (handled silently by field errors)
 * - default: Generic login failure
 *
 * @param {Object} props - Component props
 * @param {Object} props.t - Translation object containing localized strings
 *
 * @returns {JSX.Element} Rendered login form
 */
function LoginForm({ t }) {
  const [state, formAction, isPending] = useActionState(
    loginAction,
    initialState,
  );

  const router = useRouter();

  const { error } = useToast();

  useEffect(() => {
    if (state.status === 0) {
      return;
    }

    switch (state.status) {
      case 400: {
        error(t.login.errors.invalidCredentials);
        break;
      }
      case 401: {
        error(t.login.errors.notVerified);
        router.push("/auth/verification");
        break;
      }
      case 404: {
        error(t.login.errors.userNotFound);
        break;
      }
      case 422: {
        break;
      }
      default: {
        error(t.login.errors.loginFailed);
        break;
      }
    }
  }, [state.timestamp]);

  return (
    <form action={formAction}>
      <Input
        name="email"
        type="text"
        placeholder={t.inputs.email.placeholder}
        defaultValue={state?.inputs?.values?.email}
        error={state?.inputs?.errors?.email}
        maxLength={32}
      />
      <Input
        name="password"
        type="password"
        placeholder={t.inputs.password.placeholder}
        defaultValue={state?.inputs?.values?.password}
        error={state?.inputs?.errors?.password}
        maxLength={32}
      />
      <SubmitButton
        text={t.login.signIn}
        loadingText={t.login.verifying}
        isPending={isPending}
      />
    </form>
  );
}

// Component export
export default LoginForm;
