"use client";

import { useId, useState } from "react";
import { FiEye, FiEyeOff } from "react-icons/fi";

import styles from "./Input.module.css";
import { cn } from "@/shared/utils/cn";

const DEFAULT_MESSAGES = {
  valueMissing: "Заповніть це поле",
  typeMismatch: "Перевірте правильність формату",
  patternMismatch: "Значення не відповідає потрібному формату",
  tooShort: "Замало символів",
  tooLong: "Забагато символів",
  rangeUnderflow: "Значення замале",
  rangeOverflow: "Значення завелике",
  stepMismatch: "Некоректне значення",
  badInput: "Введено некоректне значення",
};

function resolveMessage(el, messages) {
  const validity = el.validity;
  for (const key of Object.keys(messages)) {
    if (validity[key]) return messages[key];
  }
  return el.validationMessage;
}

export default function Input({
  label,
  name,
  type = "text",
  error,
  hint,
  id,
  as = "input",
  /** Іконка зліва: <FiMail /> */
  icon,
  /** Слот справа. Перекриває вбудований перемикач пароля. */
  suffix,
  /** Показувати кнопку «показати пароль» для type="password" */
  revealToggle = true,
  required = false,
  showRequiredMark = true,
  messages,
  validateOn = "blur",
  className = "",
  inputClassName = "",
  onBlur,
  onInput,
  onInvalid,
  ref,
  ...props
}) {
  const uid = useId();
  const inputId = id ?? `${name ?? "field"}-${uid}`;
  const errorId = `${inputId}-error`;
  const hintId = `${inputId}-hint`;

  const [clientError, setClientError] = useState(null);
  const [touched, setTouched] = useState(false);
  const [revealed, setRevealed] = useState(false);

  const allMessages = { ...DEFAULT_MESSAGES, ...messages };

  const serverError = Array.isArray(error) ? error[0] : error;
  const errorText = clientError ?? serverError;
  const hasError = Boolean(errorText);

  const isPassword = type === "password";
  const showToggle = isPassword && revealToggle && !suffix;
  const resolvedType = isPassword && revealed ? "text" : type;

  function check(el) {
    setClientError(el.validity.valid ? null : resolveMessage(el, allMessages));
  }

  function handleInvalid(event) {
    event.preventDefault();
    setTouched(true);
    check(event.currentTarget);
    onInvalid?.(event);
  }

  function handleBlur(event) {
    if (validateOn === "blur") {
      setTouched(true);
      check(event.currentTarget);
    }
    onBlur?.(event);
  }

  function handleInput(event) {
    if (touched) check(event.currentTarget);
    onInput?.(event);
  }

  const describedBy =
    [hasError ? errorId : null, hint ? hintId : null]
      .filter(Boolean)
      .join(" ") || undefined;

  const Field = as === "textarea" ? "textarea" : "input";
  const rightSlot =
    suffix ??
    (showToggle ? (
      <RevealButton
        revealed={revealed}
        onToggle={() => setRevealed((v) => !v)}
      />
    ) : null);

  return (
    <div className={cn(styles.field, className)}>
      {label && (
        <label htmlFor={inputId} className={styles.label}>
          {label}
          {required && showRequiredMark && (
            <span className={styles.required} aria-hidden="true">
              *
            </span>
          )}
        </label>
      )}

      <div className={cn(styles.control, hasError && styles.controlError)}>
        {icon && (
          <span className={styles.icon} aria-hidden="true">
            {icon}
          </span>
        )}

        <Field
          ref={ref}
          id={inputId}
          name={name}
          type={as === "textarea" ? undefined : resolvedType}
          required={required}
          onInvalid={validateOn === "off" ? onInvalid : handleInvalid}
          onBlur={handleBlur}
          onInput={handleInput}
          aria-invalid={hasError || undefined}
          aria-describedby={describedBy}
          className={cn(
            styles.input,
            as === "textarea" && styles.textarea,
            hasError && styles.inputError,
            icon && styles.hasIcon,
            rightSlot && styles.hasSuffix,
            inputClassName,
          )}
          {...props}
        />

        {rightSlot && <div className={styles.suffix}>{rightSlot}</div>}
      </div>

      {hasError ? (
        <p id={errorId} className={styles.error} role="alert">
          {errorText}
        </p>
      ) : hint ? (
        <p id={hintId} className={styles.hint}>
          {hint}
        </p>
      ) : null}
    </div>
  );
}

function RevealButton({ revealed, onToggle }) {
  return (
    <button
      type="button"
      className={styles.toggle}
      onClick={onToggle}
      aria-label={revealed ? "Приховати пароль" : "Показати пароль"}
      aria-pressed={revealed}
    >
      {revealed ? <FiEyeOff /> : <FiEye />}
    </button>
  );
}
