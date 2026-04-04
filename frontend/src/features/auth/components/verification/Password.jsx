import React, { useState } from "react";
import styles from "./Password.module.css";
import Input from "@/shared/components/input/Input";

function Password({ state, translations }) {
  const [password, setPassword] = useState(state.value);

  const rules = [
    translations.inputs.password.validations.uppercase,
    translations.inputs.password.validations.digit,
    translations.inputs.password.validations.allowedChars,
    translations.inputs.password.validations.length,
  ];

  // useEffect(() => {
  //   if (state.status === 0) {
  //     return;
  //   }

  //   switch (state.status) {
  //     case 200: {
  //       success(translations.verification.password.checkingOtpSuccess);

  //       break;
  //     }
  //     case 400: {
  //       break;
  //     }
  //     case 401: {
  //       error(translations.verification.errors.incorrectVerificationToken);

  //       setTimeout(() => {
  //         window.location.reload();
  //       }, 3000);
  //       break;
  //     }
  //     default: {
  //       error(translations.verification.errors.otpNotSent);
  //       break;
  //     }
  //   }
  // }, [state.timestamp]);

  return (
    <>
      <p>{translations.verification.password.title}</p>
      <Input
        name="password"
        type="password"
        placeholder={translations.password}
        defaultValue={password}
        onChange={(e) => setPassword(e.target.value)}
        maxLength={32}
        error={state.error}
      />
      <div className={styles.rules}>
        <p>{translations.inputs.password.validations.title}</p>
        <ul>
          {rules.map((text, index) => (
            <li key={index}>{text}</li>
          ))}
        </ul>
      </div>
    </>
  );
}

export default Password;
