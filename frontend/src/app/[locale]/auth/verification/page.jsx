import VerificationForm from "@/features/auth/components/verification/VerificationForm";
import { getTranslations } from "next-intl/server";
import React from "react";

async function page() {
  const tVerification = await getTranslations("Verification");
  const tInputs = await getTranslations("Inputs");

  return (
    <>
      <h1>{tVerification("data.title")}</h1>
      <VerificationForm
        translations={{
          verification: tVerification.raw("data"),
          inputs: tInputs.raw("data"),
        }}
      />
    </>
  );
}

export default page;
