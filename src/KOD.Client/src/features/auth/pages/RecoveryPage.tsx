import { zodResolver } from "@hookform/resolvers/zod";
import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { KeyRound } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router";
import { z } from "zod";
import { ApiError, describeError } from "@/shared/api/problem";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { Field } from "@/shared/ui/Field";
import { TextInput } from "@/shared/ui/TextInput";
import { authApi } from "../api";
import { AuthHeader } from "../components/AuthHeader";
import { completeSignIn, useReturnUrl } from "../flow";
import styles from "./auth.module.css";

/**
 * Signing in with a recovery code instead of the authenticator.
 * @returns The page.
 */
export function RecoveryPage() {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const returnUrl = useReturnUrl();
    const [error, setError] = useState<string | null>(null);

    const schema = z.object({
        code: z
            .string()
            .trim()
            .min(1, t`Введіть резервний код.`)
            .max(32, t`Резервний код задовгий.`),
    });
    const { register, handleSubmit, formState } = useForm({ resolver: zodResolver(schema), defaultValues: { code: "" } });

    /**
     * Checks the recovery code.
     * @param values The form values.
     */
    async function verify(values: z.infer<typeof schema>): Promise<void> {
        setError(null);
        try {
            await authApi.recovery(values.code);
            await completeSignIn(queryClient);
            await navigate(returnUrl, { replace: true });
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
                icon={KeyRound}
                title={t`Вхід за резервним кодом`}
                pageTitle={t`Резервний код`}
                text={t`Використайте один із резервних кодів, отриманих під час підключення автентифікатора. Кожен код діє лише один раз.`}
            />

            {error && <Alert tone="danger">{error}</Alert>}

            <form className={styles.form} noValidate onSubmit={handleSubmit(verify)}>
                <Field label={t`Резервний код`} error={formState.errors.code?.message}>
                    {(control) => <TextInput {...control} {...register("code")} className={layout.mono} autoComplete="off" autoFocus />}
                </Field>
                <Button type="submit" size="lg" block busy={formState.isSubmitting}>
                    {t`Увійти`}
                </Button>
            </form>

            <p className={styles.note}>{t`Після входу згенеруйте нові резервні коди у профілі.`}</p>
        </>
    );
}
