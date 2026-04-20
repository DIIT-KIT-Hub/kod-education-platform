// CSR
"use client";

// Imports
import { useEffect, useRef, useState, useMemo } from "react";
import styles from "./Otp.module.css";
import Error from "../../../../shared/components/error/Error";

/**
 * OTP verification step component.
 *
 * Handles:
 * - 6-digit OTP input management
 * - Auto-focus between inputs
 * - Countdown timer for OTP expiration
 * - Automatic reset on expiration
 * - Hidden form fields for submission
 *
 * @param {Object} props - Component props
 * @param {Object} props.state - External OTP state
 * @param {string} [props.state.error] - OTP validation error message
 * @param {string|Date} props.state.expiresAt - OTP expiration timestamp
 * @param {Object} props.t - Translation object
 * @param {string} props.t.title - Step title
 * @param {string} props.t.notReceived - Text for resend countdown
 * @param {string} props.t.otpCodeExpired - Text shown when OTP expires
 * @param {(expired: boolean) => void} props.setOtpExpired - Setter for expiration state
 *
 * @returns {JSX.Element} Rendered OTP verification UI
 */
function Otp({ state, t, setOtpExpired }) {
  const [otp, setOtp] = useState(new Array(6).fill(""));
  const [now, setNow] = useState(Date.now());
  const inputsRef = useRef([]);

  useEffect(() => {
    const interval = setInterval(() => {
      setNow(Date.now());
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  const timer = useMemo(() => {
    if (!state.expiresAt) return 0;

    return Math.floor((new Date(state.expiresAt).getTime() - now) / 1000);
  }, [state.expiresAt, now]);

  const isExpired = timer <= 0;

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

// Component export
export default Otp;
