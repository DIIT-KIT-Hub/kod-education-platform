import { zodResolver } from "@hookform/resolvers/zod";
import { Trans, useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, ArrowRight, ShieldCheck } from "lucide-react";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { useNavigate } from "react-router";
import { z } from "zod";
import { ApiError, describeError } from "@/shared/api/problem";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Badge } from "@/shared/ui/Badge";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { CopyButton } from "@/shared/ui/CopyButton";
import { Field } from "@/shared/ui/Field";
import { OtpInput } from "@/shared/ui/OtpInput";
import { PasswordInput } from "@/shared/ui/PasswordInput";
import { Spinner } from "@/shared/ui/Spinner";
import { authApi } from "../api";
import { AuthHeader } from "../components/AuthHeader";
import { completeSignIn, useRememberMe, useReturnUrl } from "../flow";
import styles from "./auth.module.css";

/**
 * The first sign-in of an account (or after an administrator reset its authenticator): a temporary password is
 * replaced first, then the authenticator app is linked and the recovery codes are shown once.
 * @returns The page.
 */
export function SetupAuthenticatorPage() {
    const { t } = useLingui();
    const pending = useQuery({ queryKey: ["auth", "pending"], queryFn: authApi.pending, retry: false, staleTime: Infinity });
    const [wasTemporaryPassword, setWasTemporaryPassword] = useState(false);
    const [codes, setCodes] = useState<readonly string[] | null>(null);

    if (codes) {
        return <RecoveryCodes codes={codes} />;
    }

    if (pending.isPending) {
        return <Spinner block />;
    }

    if (pending.isError) {
        return <NoPendingSignIn error={describeError(pending.error, t`Не вдалося зв'язатися з сервером. Спробуйте ще раз.`)} />;
    }

    if (pending.data.mustChangePassword) {
        return (
            <FirstPassword
                onDone={async () => {
                    setWasTemporaryPassword(true);
                    await pending.refetch();
                }}
            />
        );
    }

    return <LinkAuthenticator step={wasTemporaryPassword} onEnrolled={setCodes} />;
}

/**
 * Shown when the time between the password and this step ran out.
 * @param props The message.
 * @returns The block.
 */
function NoPendingSignIn({ error }: { readonly error: string }) {
    const { t } = useLingui();
    return (
        <>
            <AuthHeader title={t`Захистіть вхід`} pageTitle={t`Підключення автентифікатора`} />
            <Alert tone="danger">{error}</Alert>
            <ButtonLink to="/account/login" variant="secondary" icon={ArrowLeft}>
                {t`До сторінки входу`}
            </ButtonLink>
        </>
    );
}

/**
 * Step 1 of 2 for a temporary password: the user's own password.
 * @param props Called after the password was saved.
 * @returns The step.
 */
function FirstPassword({ onDone }: { readonly onDone: () => Promise<void> }) {
    const { t } = useLingui();
    const [error, setError] = useState<string | null>(null);

    const schema = z
        .object({
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
        defaultValues: { newPassword: "", confirmPassword: "" },
    });

    /**
     * Saves the password.
     * @param values The form values.
     */
    async function save(values: z.infer<typeof schema>): Promise<void> {
        setError(null);
        try {
            await authApi.firstPassword(values.newPassword);
            await onDone();
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося зв'язатися з сервером. Спробуйте ще раз.`));
        }
    }

    return (
        <>
            <AuthHeader
                badge={<Badge tone="outline">{t`Крок 1 з 2`}</Badge>}
                title={t`Створіть власний пароль`}
                pageTitle={t`Підключення автентифікатора`}
                text={t`Тимчасовий пароль від адміністратора діє лише для першого входу. Придумайте власний — після цього підключимо автентифікатор.`}
            />

            {error && <Alert tone="danger">{error}</Alert>}

            <form className={styles.form} noValidate onSubmit={handleSubmit(save)}>
                <Field
                    label={t`Новий пароль`}
                    hint={t`Від 12 символів: великі й малі літери та цифри. Паролі з відомих витоків не приймаються.`}
                    error={formState.errors.newPassword?.message}
                >
                    {(control) => <PasswordInput {...control} {...register("newPassword")} autoComplete="new-password" autoFocus />}
                </Field>
                <Field label={t`Повторіть пароль`} error={formState.errors.confirmPassword?.message}>
                    {(control) => <PasswordInput {...control} {...register("confirmPassword")} autoComplete="new-password" />}
                </Field>
                <Button type="submit" size="lg" block iconEnd={ArrowRight} busy={formState.isSubmitting}>
                    {t`Зберегти пароль`}
                </Button>
            </form>
        </>
    );
}

/**
 * Props of {@link LinkAuthenticator}.
 */
interface LinkAuthenticatorProps {
    /** Shows "Step 2 of 2" (the password was replaced just before). */
    readonly step: boolean;
    /** Called with the recovery codes once the authenticator is linked and the user is signed in. */
    readonly onEnrolled: (codes: readonly string[]) => void;
}

/**
 * Links the authenticator app: QR code or key, then the first code.
 * @param props The props.
 * @returns The step.
 */
function LinkAuthenticator({ step, onEnrolled }: LinkAuthenticatorProps) {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const rememberMe = useRememberMe();
    const key = useQuery({ queryKey: ["auth", "authenticator"], queryFn: authApi.authenticatorKey, retry: false, staleTime: Infinity });
    const [error, setError] = useState<string | null>(null);

    const schema = z.object({ code: z.string().regex(/^\d{6}$/, t`Код містить 6 цифр.`) });
    const { control, handleSubmit, formState } = useForm({ resolver: zodResolver(schema), defaultValues: { code: "" } });

    /**
     * Checks the first code: the authenticator is linked and the user signed in.
     * @param values The form values.
     */
    async function enroll(values: z.infer<typeof schema>): Promise<void> {
        setError(null);
        try {
            const { codes } = await authApi.enroll(values.code, rememberMe);
            await completeSignIn(queryClient);
            onEnrolled(codes);
        } catch (failure) {
            if (failure instanceof ApiError && failure.code === "locked_out") {
                await navigate("/account/lockout");
                return;
            }

            setError(describeError(failure, t`Не вдалося зв'язатися з сервером. Спробуйте ще раз.`));
        }
    }

    const submit = handleSubmit(enroll);

    return (
        <>
            <AuthHeader
                badge={step ? <Badge tone="outline">{t`Крок 2 з 2`}</Badge> : undefined}
                title={t`Захистіть вхід`}
                pageTitle={t`Підключення автентифікатора`}
                text={t`Для кожного облікового запису обов'язкова двофакторна автентифікація. Прив'яжіть застосунок-автентифікатор — це займе хвилину.`}
            />

            <ol className={styles.steps}>
                <li>
                    <span>
                        <Trans>
                            Встановіть <strong>Google Authenticator</strong>, <strong>Microsoft Authenticator</strong> або інший
                            TOTP-застосунок.
                        </Trans>
                    </span>
                </li>
                <li>
                    <div className={layout.stackSm}>
                        <span>{t`Відскануйте QR-код або введіть ключ вручну:`}</span>
                        {key.isPending && <Spinner />}
                        {key.isError && (
                            <Alert tone="danger">{describeError(key.error, t`Не вдалося зв'язатися з сервером. Спробуйте ще раз.`)}</Alert>
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

            {error && <Alert tone="danger">{error}</Alert>}

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
 * The recovery codes, shown once after linking the authenticator.
 * @param props The codes.
 * @returns The step.
 */
function RecoveryCodes({ codes }: { readonly codes: readonly string[] }) {
    const { t } = useLingui();
    const returnUrl = useReturnUrl();
    return (
        <>
            <AuthHeader
                bigIcon={ShieldCheck}
                title={t`Автентифікатор підключено`}
                text={t`Збережіть резервні коди в надійному місці. Вони знадобляться, якщо ви втратите доступ до телефону. Кожен код діє один раз.`}
            />
            <ul className={[styles.codes, layout.stagger].join(" ")}>
                {codes.map((code) => (
                    <li key={code}>{code}</li>
                ))}
            </ul>
            <div className={styles.actions}>
                <CopyButton value={codes.join("\n")}>{t`Скопіювати коди`}</CopyButton>
                <ButtonLink to={returnUrl} iconEnd={ArrowRight}>
                    {t`Перейти до кабінету`}
                </ButtonLink>
            </div>
        </>
    );
}
