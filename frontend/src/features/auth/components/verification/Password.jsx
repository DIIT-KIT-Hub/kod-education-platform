import React, { useActionState, useEffect, useState } from "react";
import styles from "./Password.module.css";
import Input from "@/shared/components/input/Input";
import {
  validateDigit,
  validateLength,
  validatePasswordAllowedChars,
  validateUppercase,
} from "@/shared/utils/validations";
import SubmitButton from "@/shared/components/buttons/submit/SubmitButton";
import { passwordStepAction } from "../../actions/verification/actions";
import { useToast } from "@/shared/hooks/toast/useToast";

const initialState = {
  inputs: {
    errors: {},
    values: {},
  },
  status: 0,
  timestamp: 0,
};

function Password({ translations, email, onNext }) {
  const [password, setPassword] = useState("");
  const rules = [
    {
      key: "uppercase",
      validate: validateUppercase,
      text: translations.inputs.password.validations.uppercase,
    },
    {
      key: "digit",
      validate: validateDigit,
      text: translations.inputs.password.validations.digit,
    },
    {
      key: "allowedChars",
      validate: (pw) =>
        pw.length > 0 ? validatePasswordAllowedChars(pw) : false,
      text: translations.inputs.password.validations.allowedChars,
    },
    {
      key: "length",
      validate: (pw) => validateLength(pw, 8, 32),
      text: translations.inputs.password.validations.length,
    },
  ];

  const [state, formAction] = useActionState(passwordStepAction, initialState);

  const { success, error } = useToast();
  useEffect(() => {
    if (state.status === 0) {
      return;
    }

    switch (state.status) {
      case 200: {
        success(translations.verification.password.checkingOtpSuccess);
        onNext({
          email: state.inputs.values.email,
          password: state.inputs.values.password,
        });
        break;
      }
      case 400: {
        break;
      }
      case 401: {
        error(translations.verification.errors.incorrectVerificationToken);

        setTimeout(() => {
          window.location.reload();
        }, 3000);
        break;
      }
      default: {
        error(translations.verification.errors.otpNotSent);
        break;
      }
    }
  }, [state.timestamp]);

  const isAllRulesValid = rules.every((rule) => rule.validate(password));

  return (
    <form action={formAction}>
      <p>{translations.verification.password.title}</p>
      <input type="hidden" name="email" value={email} />
      <Input
        name="password"
        type="password"
        placeholder={translations.password}
        defaultValue={password}
        onChange={(e) => setPassword(e.target.value)}
        maxLength={32}
        error={state?.inputs?.errors?.password}
      />
      <div className={styles.rules}>
        <p>{translations.inputs.password.validations.title}</p>
        <ul>
          {rules.map((rule) => {
            const valid = rule.validate(password);
            return (
              <li
                key={rule.key}
                className={valid ? styles.valid : styles.invalid}
              >
                {rule.text}
              </li>
            );
          })}
        </ul>
      </div>
      <SubmitButton
        text={translations.verification.button.proceedOtp}
        loadingText={translations.verification.password.checkingOtp}
        disabled={!isAllRulesValid}
      />
    </form>
  );
}

export default Password;
