import { zodResolver } from "@hookform/resolvers/zod";
import { useLingui } from "@lingui/react/macro";
import { ArrowRight } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router";
import { z } from "zod";
import { ApiError, describeError } from "@/shared/api/problem";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { Checkbox } from "@/shared/ui/Checkbox";
import { Field } from "@/shared/ui/Field";
import { PasswordInput } from "@/shared/ui/PasswordInput";
import { TextInput } from "@/shared/ui/TextInput";
import { authApi } from "../api";
import { AuthHeader } from "../components/AuthHeader";
import { flowUrl, useReturnUrl } from "../flow";
import styles from "./auth.module.css";

/**
 * The first step of signing in: e-mail and password. Then the authenticator code, or the authenticator setup for a
 * new account.
 * @returns The page.
 */
export function LoginPage() {
    const { t } = useLingui();
    const navigate = useNavigate();
    const returnUrl = useReturnUrl();
    const [error, setError] = useState<string | null>(null);

    const schema = z.object({
        email: z
            .string()
            .trim()
            .min(1, t`Вкажіть електронну пошту.`)
            .pipe(z.email(t`Некоректна адреса електронної пошти.`)),
        password: z
            .string()
            .min(1, t`Вкажіть пароль.`)
            .max(128, t`Пароль не може бути довшим за 128 символів.`),
        rememberMe: z.boolean(),
    });

    const { register, handleSubmit, formState } = useForm({
        resolver: zodResolver(schema),
        defaultValues: { email: "", password: "", rememberMe: false },
    });

    /**
     * Checks the password and goes on to the next step.
     * @param values The form values.
     */
    async function signIn(values: z.infer<typeof schema>): Promise<void> {
        setError(null);
        try {
            const { next } = await authApi.login(values.email, values.password, values.rememberMe);
            await navigate(flowUrl(next === "setup" ? "/account/setup-authenticator" : "/account/login-2fa", returnUrl, values.rememberMe));
        } catch (failure) {
            if (failure instanceof ApiError && failure.code === "locked_out") {
                await navigate("/account/lockout");
                return;
            }

            setError(describeError(failure, t`Не вдалося зв'язатися з сервером. Спробуйте ще раз.`));
        }
    }

    return (
        <>
            <AuthHeader
                title={t`Вхід до кабінету`}
                pageTitle={t`Вхід`}
                text={t`Увійдіть за обліковим записом, виданим адміністратором вашої організації.`}
            />

            {error && <Alert tone="danger">{error}</Alert>}

            <form className={styles.form} noValidate onSubmit={handleSubmit(signIn)}>
                <Field label={t`Електронна пошта`} error={formState.errors.email?.message}>
                    {(control) => (
                        <TextInput
                            {...control}
                            {...register("email")}
                            type="email"
                            autoComplete="username"
                            inputMode="email"
                            placeholder="name@company.ua"
                            autoFocus
                        />
                    )}
                </Field>
                <Field label={t`Пароль`} error={formState.errors.password?.message}>
                    {(control) => <PasswordInput {...control} {...register("password")} autoComplete="current-password" />}
                </Field>
                <Checkbox {...register("rememberMe")} hint={t`Не використовуйте на спільних комп'ютерах.`}>
                    {t`Запам'ятати мене на цьому пристрої`}
                </Checkbox>
                <Button type="submit" size="lg" block iconEnd={ArrowRight} busy={formState.isSubmitting}>
                    {t`Продовжити`}
                </Button>
            </form>

            <p className={styles.note}>
                {t`Якщо ви не маєте облікового запису або забули пароль — зверніться до відділу кадрів чи системного адміністратора.`}
            </p>
        </>
    );
}
