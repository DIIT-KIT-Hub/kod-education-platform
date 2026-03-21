import React from "react";
import Input from "@/shared/components/input/Input";

function Email({ translations, email, handleChange, error }) {
  return (
    <>
      <p>{translations.title}</p>
      <Input
        name="email"
        type="text"
        placeholder={translations.email}
        value={email}
        onChange={handleChange}
        error={error}
        maxLength={32}
      />
    </>
  );
}

export default Email;
