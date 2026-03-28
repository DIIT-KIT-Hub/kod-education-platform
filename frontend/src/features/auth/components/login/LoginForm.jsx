"use client";

import React, { useActionState, useEffect } from "react";
import Input from "@/shared/components/input/Input";
import { loginAction } from "../../actions/login/actions";
import SubmitButton from "@/shared/components/buttons/submit/SubmitButton";
import { useRouter } from "next/navigation";
import { useToast } from "@/shared/hooks/toast/useToast";

const initialState = {
  success: false,
  inputs: {
    errors: {},
    values: {},
  },
  status: 0,
  timestamp: 0,
};

function LoginForm({ translations }) {
  const [state, formAction] = useActionState(loginAction, initialState);

  const router = useRouter();

  const { error } = useToast();

  useEffect(() => {
    if (state.status === 0) {
      return;
    }

    switch (state.status) {
      case 400: {
        error(translations.login.invalidCredentials);
        break;
      }
      case 401: {
        router.push("/auth/verification");
        error(translations.login.notVerified);
        break;
      }
      case 404: {
        error(translations.login.notFound);
        break;
      }
      case 422: {
        break;
      }
      default: {
        error(translations.login.loginFailed);
        break;
      }
    }
  }, [state.timestamp]);

  return (
    <form action={formAction}>
      <Input
        name="email"
        type="text"
        placeholder={translations.inputs.email.placeholder}
        defaultValue={state?.inputs?.values?.email}
        error={state?.inputs?.errors?.email}
        maxLength={32}
      />
      <Input
        name="password"
        type="password"
        placeholder={translations.inputs.password.placeholder}
        defaultValue={state?.inputs?.values?.password}
        error={state?.inputs?.errors?.password}
        maxLength={8}
      />
      <SubmitButton
        text={translations.login.signIn}
        loadingText={translations.login.checkingLogin}
      />
    </form>
  );
}

export default LoginForm;
