import React from "react";
import Input from "@/shared/components/input/Input";

function Email({ state, t }) {
  return (
    <>
      <p>{t.title}</p>
      <Input
        name="email"
        type="text"
        placeholder={t.placeholder}
        defaultValue={state.value}
        error={state.error}
        maxLength={32}
      />
    </>
  );
}

export default Email;
