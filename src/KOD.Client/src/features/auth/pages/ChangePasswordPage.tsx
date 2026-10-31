import { zodResolver } from "@hookform/resolvers/zod";
import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, KeyRound } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router";
import { z } from "zod";
import { describeError } from "@/shared/api/problem";
import { reloadAuthContext, useCurrentUser } from "@/shared/session/session";
import { Alert } from "@/shared/ui/Alert";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { Field } from "@/shared/ui/Field";
import { PasswordInput } from "@/shared/ui/PasswordInput";
import { toast } from "@/shared/ui/Toast";
import { authApi } from "../api";
import { AuthHeader } from "../components/AuthHeader";
import { useReturnUrl } from "../flow";
import styles from "./auth.module.css";

/**
 * Changes the password of the signed-in user; forced after an administrator set a temporary one. The other sign-ins of
 * the user end, this browser gets new tokens.
 * @returns The page.
 */
export function ChangePasswordPage() {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const returnUrl = useReturnUrl();
    const forced = useCurrentUser().mustChangePassword;
    const [error, setError] = useState<string | null>(null);

    const schema = z
        .object({
            currentPassword: z.string().min(1, t`Вкажіть поточний пароль.`),
            newPassword: z
                .string()
                .min(1, t`Вкажіть новий пароль.`)
                .min(12, t`Пароль має містити від 12 до 128 символів.`)
                .max(128, t`Пароль має містити від 12 до 128 символів.`),
            confirmPassword: z.string(),
        })
        .refine((values) => values.newPassword === values.confirmPassword, {
            message: t`Паролі не збігаються.`,
            path: ["confirmPassword"],
        });

    const { register, handleSubmit, formState } = useForm({
        resolver: zodResolver(schema),
        defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" },
    });

    /**
     * Saves the new password.
     * @param values The form values.
     */
    async function change(values: z.infer<typeof schema>): Promise<void> {
        setError(null);
        try {
            await authApi.changePassword(values.currentPassword, values.newPassword);
            await reloadAuthContext(queryClient);
            toast(t`Пароль змінено.`);
            if (forced) {
                await navigate(returnUrl, { replace: true });
            } else {
                window.location.assign("/profile/security");
            }
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося зв'язатися з сервером. Спробуйте ще раз.`));
        }
    }

    return (
        <>
            <AuthHeader
                icon={KeyRound}
                title={forced ? t`Встановіть власний пароль` : t`Зміна пароля`}
                pageTitle={t`Зміна пароля`}
                text={
                    forced
                        ? t`Ви увійшли з тимчасовим паролем. Задайте власний пароль, щоб продовжити роботу.`
                        : t`Після зміни пароля всі інші сесії буде завершено.`
                }
            />

            {error && <Alert tone="danger">{error}</Alert>}

            <form className={styles.form} noValidate onSubmit={handleSubmit(change)}>
                <Field label={t`Поточний пароль`} error={formState.errors.currentPassword?.message}>
                    {(control) => <PasswordInput {...control} {...register("currentPassword")} autoComplete="current-password" autoFocus />}
                </Field>
                <Field
                    label={t`Новий пароль`}
                    hint={t`Щонайменше 12 символів: великі й малі латинські літери та цифри.`}
                    error={formState.errors.newPassword?.message}
                >
                    {(control) => <PasswordInput {...control} {...register("newPassword")} autoComplete="new-password" />}
                </Field>
                <Field label={t`Повторіть новий пароль`} error={formState.errors.confirmPassword?.message}>
                    {(control) => <PasswordInput {...control} {...register("confirmPassword")} autoComplete="new-password" />}
                </Field>
                <Button type="submit" size="lg" block busy={formState.isSubmitting}>
                    {t`Зберегти пароль`}
                </Button>
            </form>

            {!forced && (
                <ButtonLink href="/profile/security" variant="ghost" icon={ArrowLeft}>
                    {t`Повернутися`}
                </ButtonLink>
            )}
        </>
    );
}
