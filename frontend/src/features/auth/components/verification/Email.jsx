import React, { useActionState, useEffect, useTransition } from "react";
import Input from "@/shared/components/input/Input";
import { emailStepAction } from "../../actions/verification/actions";
import SubmitButton from "@/shared/components/buttons/submit/SubmitButton";
import { useToast } from "@/shared/hooks/toast/useToast";
import { useRouter } from "@/i18n/routing";

const initialState = {
  inputs: {
    errors: {},
    values: {},
  },
  status: 0,
  timestamp: 0,
};

function Email({ translations }) {
  const [state, formAction] = useActionState(emailStepAction, initialState);

  const { error } = useToast();

  const router = useRouter();

  useEffect(() => {
    if (state.status === 0) {
      return;
    }

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
  }, [state.timestamp]);

  return (
    <form action={formAction}>
      <p>{translations.verification.email.title}</p>
      <Input
        name="email"
        type="text"
        placeholder={translations.inputs.email.placeholder}
        defaultValue={state?.inputs?.values?.email}
        error={state?.inputs?.errors?.email}
        maxLength={32}
      />
      <SubmitButton
        text={translations.verification.email.startVerification}
        loadingText={translations.verification.email.checkingUser}
      />
    </form>
  );
}

export default Email;
