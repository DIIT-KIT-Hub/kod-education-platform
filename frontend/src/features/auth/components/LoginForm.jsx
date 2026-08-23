"use client";

import { useState } from "react";
import { FiMail, FiKey, FiEye, FiEyeOff } from "react-icons/fi";

import Button from "@/shared/components/ui/Button";
import Input from "@/shared/components/ui/Input";
import styles from "./LoginForm.module.css";

function LoginForm() {
  const [showPassword, setShowPassword] = useState(false);

  return (
    <form className={styles.form}>
      <Input
        label="Електронна пошта"
        name="email"
        type="email"
        autoComplete="email"
        placeholder="name@school.edu.ua"
        icon={<FiMail />}
        required
        messages={{
          valueMissing: "Вкажіть електронну пошту",
          typeMismatch: "Схоже, адреса введена з помилкою",
        }}
      />

      <Input
        label="Пароль"
        name="password"
        type={showPassword ? "text" : "password"}
        autoComplete="current-password"
        placeholder="Введіть пароль"
        icon={<FiKey />}
        required
        minLength={8}
        messages={{
          valueMissing: "Введіть пароль",
          tooShort: "Пароль має містити щонайменше 8 символів",
        }}
        suffix={
          <button
            type="button"
            className={styles.toggle}
            onClick={() => setShowPassword((v) => !v)}
            aria-label={showPassword ? "Приховати пароль" : "Показати пароль"}
            aria-pressed={showPassword}
          >
            {showPassword ? <FiEyeOff /> : <FiEye />}
          </button>
        }
      />

      <Button type="submit" size="lg" fullWidth>
        Увійти
      </Button>
    </form>
  );
}

export default LoginForm;
