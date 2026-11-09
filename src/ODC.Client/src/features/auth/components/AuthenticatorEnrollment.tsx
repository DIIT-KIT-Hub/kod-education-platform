import { zodResolver } from "@hookform/resolvers/zod";
import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ShieldCheck } from "lucide-react";
import type { ReactNode } from "react";
import { Controller, useForm } from "react-hook-form";
import { useNavigate } from "react-router";
import { z } from "zod";
import { ApiError, describeError } from "@/shared/api/problem";
import layout from "@/shared/styles/layout.module.css";
import { Button } from "@/shared/ui/Button";
import { CopyButton } from "@/shared/ui/CopyButton";
import { Field } from "@/shared/ui/Field";
import { OtpInput } from "@/shared/ui/OtpInput";
import { Skeleton, SkeletonGroup } from "@/shared/ui/Skeleton";
import { toast } from "@/shared/ui/Toast";
import { authApi } from "../api";
import { completeSignIn } from "../flow";
import styles from "../pages/auth.module.css";
import { AuthHeader } from "./AuthHeader";

/**
 * Props of {@link LinkAuthenticator}.
 */
export interface LinkAuthenticatorProps {
    /** A badge above the heading (e.g. the step). */
    readonly badge?: ReactNode;
    /** Whether the sign-in survives closing the browser. */
    readonly rememberMe: boolean;
    /** Called with the recovery codes once the authenticator is linked and the user is signed in. */
    readonly onEnrolled: (codes: readonly string[]) => void;
}

/**
 * Links the authenticator app of a sign-in waiting for the second factor: QR code or key, then the first code. The
 * user is signed in when the code is right.
 * @param props The props.
 * @returns The step.
 */
export function LinkAuthenticator({ badge, rememberMe, onEnrolled }: LinkAuthenticatorProps) {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const key = useQuery({
        queryKey: ["auth", "authenticator"],
        queryFn: ({ signal }) => authApi.authenticatorKey(signal),
        retry: false,
        staleTime: Infinity,
    });

    const schema = z.object({ code: z.string().regex(/^\d{6}$/, t`Код містить 6 цифр.`) });
    const { control, handleSubmit, formState } = useForm({ resolver: zodResolver(schema), defaultValues: { code: "" } });

    /**
     * Checks the first code: the authenticator is linked and the user signed in.
     * @param values The form values.
     */
    async function enroll(values: z.infer<typeof schema>): Promise<void> {
        try {
            const { codes } = await authApi.enroll(values.code, rememberMe);
            await completeSignIn(queryClient);
            onEnrolled(codes);
        } catch (failure) {
            if (failure instanceof ApiError && failure.code === "locked_out") {
                await navigate("/account/lockout");
                return;
            }

            toast(describeError(failure, t`Не вдалося зв'язатися з сервером. Спробуйте ще раз.`), { tone: "error" });
        }
    }

    const submit = handleSubmit(enroll);

    return (
        <>
            <AuthHeader
                badge={badge}
                title={t`Захистіть вхід`}
                pageTitle={t`Підключення автентифікатора`}
                text={t`Для кожного облікового запису обов'язкова двофакторна автентифікація. Прив'яжіть застосунок-автентифікатор — це займе хвилину.`}
            />

            <ol className={styles.steps}>
                <li>
                    {/* Plain text: static elements inside <Trans> are hoisted by the React Compiler and break the message id. */}
                    <span>{t`Встановіть Google Authenticator, Microsoft Authenticator або інший TOTP-застосунок.`}</span>
                </li>
                <li>
                    <div className={layout.stackSm}>
                        <span>{t`Відскануйте QR-код або введіть ключ вручну:`}</span>
                        {key.isPending && (
                            <SkeletonGroup>
                                <Skeleton shape="block" width="15rem" height="15rem" />
                            </SkeletonGroup>
                        )}
                        {key.data && (
                            <>
                                <div className={styles.qr}>
                                    <img
                                        src={`data:image/svg+xml;charset=utf-8,${encodeURIComponent(key.data.qrCodeSvg)}`}
                                        alt={t`QR-код для автентифікатора`}
                                    />
                                </div>
                                <div className={[layout.row, layout.cluster].join(" ")}>
                                    <code className={styles.key}>{key.data.formattedKey}</code>
                                    <CopyButton value={key.data.sharedKey} variant="ghost" size="sm">
                                        {t`Копіювати`}
                                    </CopyButton>
                                </div>
                            </>
                        )}
                    </div>
                </li>
                <li>
                    <span>{t`Введіть 6-значний код, який показує застосунок.`}</span>
                </li>
            </ol>

            <form className={styles.form} noValidate onSubmit={submit}>
                <Field label={t`Код з застосунку`} error={formState.errors.code?.message}>
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
                                />
                            )}
                        />
                    )}
                </Field>
                <Button type="submit" size="lg" block icon={ShieldCheck} busy={formState.isSubmitting}>
                    {t`Підтвердити та увійти`}
                </Button>
            </form>
        </>
    );
}

/**
 * Props of {@link RecoveryCodes}.
 */
export interface RecoveryCodesProps {
    /** The recovery codes. */
    readonly codes: readonly string[];
    /** A badge above the heading (e.g. the step). */
    readonly badge?: ReactNode;
    /** The action that continues (a link or a button). */
    readonly children: ReactNode;
}

/**
 * The recovery codes, shown once after linking the authenticator.
 * @param props The props.
 * @returns The step.
 */
export function RecoveryCodes({ codes, badge, children }: RecoveryCodesProps) {
    const { t } = useLingui();
    return (
        <>
            <AuthHeader
                badge={badge}
                bigIcon={ShieldCheck}
                title={t`Автентифікатор підключено`}
                text={t`Збережіть резервні коди в надійному місці. Вони знадобляться, якщо Ви втратите доступ до телефону. Кожен код діє один раз.`}
            />
            <ul className={[styles.codes, layout.stagger].join(" ")}>
                {codes.map((code) => (
                    <li key={code}>{code}</li>
                ))}
            </ul>
            <div className={styles.actions}>
                <CopyButton value={codes.join("\n")}>{t`Скопіювати коди`}</CopyButton>
                {children}
            </div>
        </>
    );
}

/**
 * Props of {@link AuthenticatorCodeForm}.
 */
export interface AuthenticatorCodeFormProps {
    /** Whether the sign-in survives closing the browser. */
    readonly rememberMe: boolean;
    /** The label of the button. */
    readonly submitText: string;
    /** Called once the code was right and the user is signed in. */
    readonly onVerified: () => Promise<void>;
}

/**
 * The code of the enrolled authenticator app that completes a sign-in waiting for the second factor. A code is
 * single-use, so the form never sends the same one twice.
 * @param props The props.
 * @returns The form.
 */
export function AuthenticatorCodeForm({ rememberMe, submitText, onVerified }: AuthenticatorCodeFormProps) {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const schema = z.object({ code: z.string().regex(/^\d{6}$/, t`Код містить 6 цифр.`) });
    const { control, handleSubmit, formState } = useForm({ resolver: zodResolver(schema), defaultValues: { code: "" } });

    /**
     * Checks the code.
     * @param values The form values.
     */
    async function verify(values: z.infer<typeof schema>): Promise<void> {
        try {
            await authApi.twoFactor(values.code, rememberMe);
            await completeSignIn(queryClient);
            await onVerified();
        } catch (failure) {
            if (failure instanceof ApiError && failure.code === "locked_out") {
                await navigate("/account/lockout");
                return;
            }

            toast(describeError(failure, t`Не вдалося зв'язатися з сервером. Спробуйте ще раз.`), { tone: "error" });
        }
    }

    const submit = handleSubmit(verify);

    return (
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
                {submitText}
            </Button>
        </form>
    );
}
