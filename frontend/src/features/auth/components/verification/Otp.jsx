"use client";

import React, { useEffect, useRef, useState, useMemo } from "react";
import styles from "./Otp.module.css";
import Error from "../../../../shared/components/error/Error";

function Otp({ state, t, setOtpExpired }) {
  const [otp, setOtp] = useState(new Array(6).fill(""));
  const inputsRef = useRef([]);

  // ми оновлюємо тільки "now", а не timer
  const [now, setNow] = useState(Date.now());

  // старт таймера
  useEffect(() => {
    const interval = setInterval(() => {
      setNow(Date.now());
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  // залишок часу
  const timer = useMemo(() => {
    if (!state.expiresAt) return 0;

    return Math.floor((new Date(state.expiresAt).getTime() - now) / 1000);
  }, [state.expiresAt, now]);

  const isExpired = timer <= 0;

  // якщо expired → чистимо OTP і фокусимо перший інпут
  useEffect(() => {
    if (!state.expiresAt) return;

    if (isExpired) {
      setOtp(new Array(6).fill(""));
      setOtpExpired(true);

      requestAnimationFrame(() => {
        inputsRef.current?.[0]?.focus();
      });
    }
  }, [isExpired, state.expiresAt, setOtpExpired]);

  const formatTime = () => {
    const safe = Math.max(timer, 0);
    const minutes = Math.floor(safe / 60);
    const seconds = safe % 60;

    return `${minutes}:${seconds < 10 ? `0${seconds}` : seconds}`;
  };

  const handleChange = (element, index) => {
    if (isExpired) return;

    const value = element.value;

    if (!/^\d*$/.test(value)) return;

    const newOtp = [...otp];
    newOtp[index] = value.slice(-1);

    setOtp(newOtp);

    if (value && index < 5) {
      inputsRef.current[index + 1]?.focus();
    }
  };

  const handleKeyDown = (e, index) => {
    if (e.key === "Backspace" && !otp[index] && index > 0) {
      inputsRef.current[index - 1]?.focus();
    }
  };

  return (
    <>
      <p>{t.title}</p>

      <input type="hidden" name="otpCode" value={otp.join("")} />

      <input
        type="hidden"
        name="intent"
        value={isExpired ? "resend" : "verify"}
      />

      <div className={styles.otpInputs}>
        {otp.map((data, index) => (
          <input
            key={index}
            type="text"
            maxLength={1}
            value={data}
            disabled={isExpired}
            ref={(el) => (inputsRef.current[index] = el)}
            onChange={(e) => handleChange(e.target, index)}
            onKeyDown={(e) => handleKeyDown(e, index)}
            className={styles.input}
          />
        ))}
      </div>

      {state.error && <Error error={state.error} />}

      <div className={styles.timerSection}>
        {!isExpired ? (
          <p>
            {t.notReceived} <strong>{formatTime()}</strong>
          </p>
        ) : (
          <p className={styles.resend}>{t.otpCodeExpired}</p>
        )}
      </div>
    </>
  );
}

export default Otp;
