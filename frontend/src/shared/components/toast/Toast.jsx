import React, { useEffect, useState } from "react";
import styles from "./Toast.module.css";
import Image from "next/image";

function Toast({ id, message, type = "default", onClose, duration = 3000 }) {
  const [visible, setVisible] = useState(false);
  const [hide, setHide] = useState(false);

  useEffect(() => {
    // даємо невеликий таймаут, щоб transition спрацював
    const showTimer = setTimeout(() => setVisible(true), 10);

    // через duration починаємо зникати
    const hideTimer = setTimeout(() => setHide(true), duration);

    // через 300ms після зникання видаляємо з DOM
    const removeTimer = setTimeout(() => onClose(id), duration + 300);

    return () => {
      clearTimeout(showTimer);
      clearTimeout(hideTimer);
      clearTimeout(removeTimer);
    };
  }, [id, onClose, duration]);

  const typeClass = {
    success: styles.toastSuccess,
    error: styles.toastError,
    warning: styles.toastWarning,
    default: styles.toastDefault,
  }[type];

  return (
    <div
      className={`${styles.toast} ${typeClass} ${
        visible ? styles.toastVisible : ""
      } ${hide ? styles.toastHide : ""}`}
    >
      {type === "success" && (
        <div className={styles.icon}>
          <Image
            src="/assets/icons/success-icon.webp"
            alt="success_icon_image"
            fill
          />
        </div>
      )}
      {type === "error" && (
        <div className={styles.icon}>
          <Image
            src="/assets/icons/error-icon.webp"
            alt="error_icon_image"
            fill
          />
        </div>
      )}
      {type === "warning" && (
        <div className={styles.icon}>
          <Image
            src="/assets/icons/warning-icon.webp"
            alt="warning_icon_image"
            fill
          />
        </div>
      )}

      {message}
    </div>
  );
}

export default Toast;
