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

function LoginForm({ t }) {
  const [state, formAction] = useActionState(loginAction, initialState);

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
        router.push("/auth/verification");
        error(t.login.errors.notVerified);
        break;
      }
      case 404: {
        error(t.login.errors.notFound);
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
        maxLength={8}
      />
      <SubmitButton
        text={t.login.signIn}
        loadingText={t.login.verifying}
      />
    </form>
  );
}

export default LoginForm;
