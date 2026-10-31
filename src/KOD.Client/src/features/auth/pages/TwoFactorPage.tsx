import { zodResolver } from "@hookform/resolvers/zod";
import { Trans, useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { ShieldCheck, Smartphone } from "lucide-react";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { Link, useNavigate } from "react-router";
import { z } from "zod";
import { ApiError, describeError } from "@/shared/api/problem";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { Field } from "@/shared/ui/Field";
import { OtpInput } from "@/shared/ui/OtpInput";
import { authApi } from "../api";
import { AuthHeader } from "../components/AuthHeader";
import { completeSignIn, flowUrl, useRememberMe, useReturnUrl } from "../flow";
import styles from "./auth.module.css";

/**
 * The second step of signing in: the code of the authenticator app.
 * @returns The page.
 */
export function TwoFactorPage() {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const returnUrl = useReturnUrl();
    const rememberMe = useRememberMe();
    const [error, setError] = useState<string | null>(null);

    const schema = z.object({ code: z.string().regex(/^\d{6}$/, t`Код містить 6 цифр.`) });
    const { control, handleSubmit, formState } = useForm({ resolver: zodResolver(schema), defaultValues: { code: "" } });

    /**
     * Checks the code; a code is single-use, so the form never sends the same one twice.
     * @param values The form values.
     */
    async function verify(values: z.infer<typeof schema>): Promise<void> {
        setError(null);
        try {
            await authApi.twoFactor(values.code, rememberMe);
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

    const submit = handleSubmit(verify);

    return (
        <>
            <AuthHeader
                icon={Smartphone}
                title={t`Код з автентифікатора`}
                pageTitle={t`Підтвердження входу`}
                text={t`Відкрийте застосунок-автентифікатор і введіть 6-значний код для облікового запису КОД.`}
            />

            {error && <Alert tone="danger">{error}</Alert>}

            <form className={styles.form} noValidate onSubmit={submit}>
                <Field label={t`Код підтвердження`} error={formState.errors.code?.message}>
                    {(ids) => (
                        <Controller
                            control={control}
                            name="code"
                            render={({ field }) => (
                                <OtpInput
                                    {...ids}
                                    ref={field.ref}
                                    name={field.name}
                                    value={field.value}
                                    onChange={field.onChange}
                                    onBlur={field.onBlur}
                                    onComplete={() => {
                                        if (!formState.isSubmitting) {
                                            void submit();
                                        }
                                    }}
                                    disabled={formState.isSubmitting}
                                    autoFocus
                                />
                            )}
                        />
                    )}
                </Field>
                <Button type="submit" size="lg" block icon={ShieldCheck} busy={formState.isSubmitting}>
                    {t`Увійти`}
                </Button>
            </form>

            <p className={styles.note}>
                <Trans>
                    Немає доступу до телефону?{" "}
                    <Link className={layout.link} to={flowUrl("/account/login-recovery", returnUrl, rememberMe)}>
                        Увійти за резервним кодом
                    </Link>
                    .
                </Trans>
            </p>
        </>
    );
}
