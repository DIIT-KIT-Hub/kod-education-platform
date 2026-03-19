"use client";

import React from "react";
import styles from "./Additional.module.css";
import { Link } from "@/i18n/routing";
import { usePathname } from "next/navigation";

function Additional({ translations }) {
  const pathname = usePathname();
  const isVerificationPage = pathname.includes("/verification");

  return (
    <div className={styles.additional}>
      {!isVerificationPage && (
        <>
          <Link href="/auth/forgotpassword">{translations.forgotPassword}</Link>
          <span>|</span>
        </>
      )}
      <Link href={isVerificationPage ? "/auth/login" : "/auth/verification"}>
        {isVerificationPage ? translations.verified : translations.notVerified}
      </Link>
    </div>
  );
}

export default Additional;
