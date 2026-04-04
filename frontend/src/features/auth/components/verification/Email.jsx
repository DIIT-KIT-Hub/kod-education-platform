import React from "react";
import Input from "@/shared/components/input/Input";

function Email({ state, translations }) {
  return (
    <>
      <p>{translations.verification.email.title}</p>
      <Input
        name="email"
        type="text"
        placeholder={translations.inputs.email.placeholder}
        defaultValue={state.value}
        error={state.error}
        maxLength={32}
      />
    </>
  );
}

export default Email;
