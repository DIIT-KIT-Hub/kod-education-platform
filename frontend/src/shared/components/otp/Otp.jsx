"use client";
import React, { useState, useEffect, useRef } from "react";
import styles from "./Otp.module.css";

function Otp({ translations }) {
  const [otp, setOtp] = useState(new Array(6).fill(""));
  const [timer, setTimer] = useState(5);
  const [canResend, setCanResend] = useState(false);
  const inputsRef = useRef([]);

  useEffect(() => {
    let interval;

    if (timer > 0) {
      interval = setInterval(() => {
        setTimer((prev) => prev - 1);
      }, 1000);
    } else {
      setCanResend(true);
      clearInterval(interval);
    }

    return () => clearInterval(interval);
  }, [timer]);

  const formatTime = () => {
    const minutes = Math.floor(timer / 60);
    const seconds = timer % 60;

    return `${minutes}:${seconds < 10 ? `0${seconds}` : seconds}`;
  };

  const handleChange = (element, index) => {
    if (isNaN(element.value)) {
      return false;
    }

    const newOtp = [...otp];
    newOtp[index] = element.value.substring(element.value.length - 1);
    setOtp(newOtp);

    if (element.value && index < 5) {
      inputsRef.current[index + 1].focus();
    }
  };

  const handleKeyDown = (e, index) => {
    if (e.key === "Backspace" && !otp[index] && index > 0) {
      inputsRef.current[index - 1].focus();
    }
  };

  const handleResend = () => {
    if (canResend) {
      setTimer(300);
      setCanResend(false);
      setOtp(new Array(6).fill(""));
      console.log("OTP Sent again");
    }
  };

  return (
    <>
      <p>{translations.title}</p>

      <div className={styles.otpInputs}>
        {otp.map((data, index) => (
          <input
            key={index}
            type="text"
            maxLength="1"
            value={data}
            ref={(el) => (inputsRef.current[index] = el)}
            onChange={(e) => handleChange(e.target, index)}
            onKeyDown={(e) => handleKeyDown(e, index)}
            className={styles.input}
          />
        ))}
      </div>

      <div className={styles.timerSection}>
        {timer > 0 ? (
          <p className={styles.notReceived}>
            {translations.notReceived} <strong>{formatTime()}</strong>
          </p>
        ) : (
          <p className={styles.resend} onClick={handleResend}>
            {translations.resend}
          </p>
        )}
      </div>
    </>
  );
}

export default Otp;
