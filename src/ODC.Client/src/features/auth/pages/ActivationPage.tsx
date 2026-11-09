import { zodResolver } from "@hookform/resolvers/zod";
import { Trans, useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowRight, Camera, KeyRound, Mail, MailCheck, PencilLine, RotateCw, Smartphone } from "lucide-react";
import { useEffect, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { Link, useNavigate } from "react-router";
import { z } from "zod";
import type { ActivationStatusResponse, ActivationVerifiedResponse } from "@/shared/api/schema";
import { ApiError, describeError } from "@/shared/api/problem";
import { authContextQuery } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Button } from "@/shared/ui/Button";
import { Field } from "@/shared/ui/Field";
import { OtpInput } from "@/shared/ui/OtpInput";
import { PasswordInput } from "@/shared/ui/PasswordInput";
import { SkeletonText } from "@/shared/ui/Skeleton";
import { StepProgress } from "@/shared/ui/StepProgress";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { activationApi, authApi } from "../api";
import { AuthenticatorCodeForm, LinkAuthenticator, RecoveryCodes } from "../components/AuthenticatorEnrollment";
import { AuthHeader } from "../components/AuthHeader";
import { PhotoForm } from "../components/PhotoForm";
import styles from "./auth.module.css";

/**
 * Where the activation stands in this browser.
 */
type Stage =
    | { readonly kind: "email" }
    | { readonly kind: "code"; readonly email: string; readonly resendAt: number; readonly expired: boolean }
    | { readonly kind: "password"; readonly account: ActivationVerifiedResponse | null }
    | { readonly kind: "authenticator"; readonly enroll: boolean; readonly account: ActivationVerifiedResponse | null }
    | { readonly kind: "recovery"; readonly codes: readonly string[]; readonly account: ActivationVerifiedResponse | null }
    | { readonly kind: "photo" };

/**
 * The position of every stage in the stepper (the recovery codes belong to the authenticator step).
 */
const stepIndex: Readonly<Record<Stage["kind"], number>> = { email: 0, code: 1, password: 2, authenticator: 3, recovery: 3, photo: 4 };

/**
 * The moment an answer of the server arrived (called from event handlers, never during rendering).
 * @returns Milliseconds since the epoch.
 */
function answeredAt(): number {
    return Date.now();
}

/**
 * What this browser can continue after a reload: the activation the server remembers, or the sign-in it started (the
 * password is set and the authenticator step is open).
 */
interface Resumed {
    /** The activation, while the code or the password step is open. */
    readonly status?: ActivationStatusResponse;
    /** The sign-in waiting for the second factor, after the password step. */
    readonly pending?: { readonly twoFactorEnabled: boolean };
}

/**
 * Asks the server where this browser stopped.
 * @param signal Cancels the requests when the page no longer needs the answer.
 * @returns The activation or the pending sign-in; nothing for a fresh start.
 */
async function resume(signal?: AbortSignal): Promise<Resumed> {
    try {
        return { status: await activationApi.status(signal) };
    } catch (failure) {
        if (!(failure instanceof ApiError && failure.status === 404)) {
            throw failure;
        }
    }

    try {
        return { pending: await authApi.pending(signal) };
    } catch {
        return {};
    }
}

/**
 * Turns what the server remembers for this browser into the stage to show (after a reload).
 * @param resumed The activation or the pending sign-in, or undefined for a fresh start.
 * @param receivedAt When the answer arrived (the pause before another code counts from it).
 * @returns The stage.
 */
function stageOf(resumed: Resumed | undefined, receivedAt: number): Stage {
    if (resumed?.pending) {
        return { kind: "authenticator", enroll: !resumed.pending.twoFactorEnabled, account: null };
    }

    const status = resumed?.status;
    if (!status) {
        return { kind: "email" };
    }

    if (status.stage === "Password") {
        return { kind: "password", account: null };
    }

    return {
        kind: "code",
        email: status.email,
        resendAt: receivedAt + status.resendAfterSeconds * 1000,
        expired: status.stage === "Expired",
    };
}

/**
 * Activation of an account created by an administrator, as one multi-step form: the e-mail, the one-time code sent to
 * it, an own password, the authenticator app and the profile photo. Nobody but the owner ever knows the password.
 * A reload continues where the user stopped (the server keeps the activation of this browser in an HttpOnly cookie;
 * after the authenticator the user is signed in and the photo page takes over).
 * @returns The page.
 */
export function ActivationPage() {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const status = useQuery({
        queryKey: ["auth", "activation"],
        queryFn: ({ signal }) => resume(signal),
        retry: false,
        staleTime: Infinity,
        gcTime: 0,
    });
    const [chosen, setChosen] = useState<Stage | null>(null);

    if (!chosen && status.isPending) {
        return <SkeletonText lines={6} />;
    }

    const stage = chosen ?? stageOf(status.data, status.dataUpdatedAt);
    const steps = [t`Пошта`, t`Код`, t`Пароль`, t`Автентифікатор`, t`Фото`];

    /**
     * Continues after the authenticator: the photo, unless the account already has one.
     */
    async function afterSignIn(): Promise<void> {
        const context = queryClient.getQueryData(authContextQuery.queryKey);
        if (context?.user?.photoId) {
            await finish();
            return;
        }

        setChosen({ kind: "photo" });
    }

    /**
     * Ends the activation in the cabinet.
     */
    async function finish(): Promise<void> {
        toast(t`Обліковий запис активовано. Ласкаво просимо до КОД!`);
        await navigate("/", { replace: true });
    }

    /**
     * Starts over with another e-mail.
     */
    async function restart(): Promise<void> {
        try {
            await activationApi.cancel();
        } catch {
            // Nothing to forget: the cookie has expired already.
        }

        setChosen({ kind: "email" });
    }

    return (
        <div className={layout.stack}>
            <StepProgress label={t`Кроки активації`} steps={steps} current={stepIndex[stage.kind]} />

            {stage.kind === "email" && (
                <EmailStep
                    onSent={(sent, at) =>
                        setChosen({ kind: "code", email: sent.email, resendAt: at + sent.resendAfterSeconds * 1000, expired: false })
                    }
                />
            )}

            {stage.kind === "code" && (
                <CodeStep
                    key={`${stage.email}-${stage.resendAt}`}
                    email={stage.email}
                    resendAt={stage.resendAt}
                    expired={stage.expired}
                    onResent={(sent, at) =>
                        setChosen({ kind: "code", email: sent.email, resendAt: at + sent.resendAfterSeconds * 1000, expired: false })
                    }
                    onVerified={(account) => setChosen({ kind: "password", account })}
                    onRestart={restart}
                />
            )}

            {stage.kind === "password" && (
                <PasswordStep
                    account={stage.account}
                    onSaved={(next) => setChosen({ kind: "authenticator", enroll: next === "setup", account: stage.account })}
                    onExpired={restart}
                />
            )}

            {stage.kind === "authenticator" &&
                (stage.enroll ? (
                    <LinkAuthenticator
                        rememberMe={false}
                        onEnrolled={(codes) => setChosen({ kind: "recovery", codes, account: stage.account })}
                    />
                ) : (
                    <>
                        <AuthHeader
                            icon={Smartphone}
                            title={t`Код з автентифікатора`}
                            pageTitle={t`Активація облікового запису`}
                            text={t`Пароль збережено. До облікового запису вже підключено автентифікатор — введіть 6-значний код із застосунку.`}
                        />
                        <AuthenticatorCodeForm rememberMe={false} submitText={t`Підтвердити`} onVerified={afterSignIn} />
                        <p className={styles.note}>
                            <Trans>
                                Немає доступу до телефону?{" "}
                                <Link className={layout.link} to="/account/login-recovery">
                                    Увійти за резервним кодом
                                </Link>
                                .
                            </Trans>
                        </p>
                    </>
                ))}

            {stage.kind === "recovery" && (
                <RecoveryCodes codes={stage.codes}>
                    <Button iconEnd={ArrowRight} onClick={() => void afterSignIn()}>
                        {t`Далі`}
                    </Button>
                </RecoveryCodes>
            )}

            {stage.kind === "photo" && (
                <>
                    <AuthHeader
                        icon={Camera}
                        title={t`Додайте фото профілю`}
                        pageTitle={t`Активація облікового запису`}
                        text={t`Останній крок. Колеги бачать Ваше фото в документах, новинах і структурі. Без фото кабінетом користуватися не можна.`}
                    />
                    <PhotoForm onSaved={finish} />
                </>
            )}
        </div>
    );
}

/**
 * Props of {@link EmailStep}.
 */
interface EmailStepProps {
    /** Called with the state once the code was requested, and the moment of the answer. */
    readonly onSent: (status: ActivationStatusResponse, at: number) => void;
}

/**
 * Step 1: the e-mail of the account.
 * @param props The props.
 * @returns The step.
 */
function EmailStep({ onSent }: EmailStepProps) {
    const { t } = useLingui();
    const schema = z.object({
        email: z
            .string()
            .trim()
            .min(1, t`Вкажіть електронну пошту.`)
            .pipe(z.email(t`Некоректна адреса електронної пошти.`)),
    });
    const { register, handleSubmit, formState } = useForm({ resolver: zodResolver(schema), defaultValues: { email: "" } });

    /**
     * Requests the code.
     * @param values The form values.
     */
    async function send(values: z.infer<typeof schema>): Promise<void> {
        try {
            const status = await activationApi.start(values.email);
            onSent(status, answeredAt());
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося зв'язатися з сервером. Спробуйте ще раз.`), { tone: "error" });
        }
    }

    return (
        <>
            <AuthHeader
                icon={Mail}
                title={t`Активація облікового запису`}
                text={t`Адміністратор уже створив Ваш обліковий запис. Вкажіть робочу електронну пошту — надішлемо на неї одноразовий код, щоб підтвердити, що це Ви.`}
            />
            <form className={styles.form} noValidate onSubmit={handleSubmit(send)}>
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
                <Button type="submit" size="lg" block iconEnd={ArrowRight} busy={formState.isSubmitting}>
                    {t`Надіслати код`}
                </Button>
            </form>
            <p className={styles.note}>
                <Trans>
                    Уже активували обліковий запис?{" "}
                    <Link className={layout.link} to="/account/login">
                        Увійдіть з паролем
                    </Link>
                    .
                </Trans>
            </p>
        </>
    );
}

/**
 * Counts down to a moment, once a second.
 * @param moment The moment (milliseconds since the epoch).
 * @returns The whole seconds left (0 when reached).
 */
function useSecondsUntil(moment: number): number {
    const [now, setNow] = useState(() => Date.now());
    useEffect(() => {
        const timer = window.setInterval(() => setNow(Date.now()), 1000);
        return () => window.clearInterval(timer);
    }, []);

    return Math.max(0, Math.ceil((moment - now) / 1000));
}

/**
 * Props of {@link CodeStep}.
 */
interface CodeStepProps {
    /** The e-mail the code went to. */
    readonly email: string;
    /** When another code can be requested (milliseconds since the epoch). */
    readonly resendAt: number;
    /** Whether the code can no longer be entered (a new one is needed). */
    readonly expired: boolean;
    /** Called with the state once a new code was requested, and the moment of the answer. */
    readonly onResent: (status: ActivationStatusResponse, at: number) => void;
    /** Called once the code was right. */
    readonly onVerified: (account: ActivationVerifiedResponse) => void;
    /** Starts over with another e-mail. */
    readonly onRestart: () => Promise<void>;
}

/**
 * Step 2: the one-time code from the e-mail.
 * @param props The props.
 * @returns The step.
 */
function CodeStep({ email, resendAt, expired: expiredAtStart, onResent, onVerified, onRestart }: CodeStepProps) {
    const { t } = useLingui();
    const [expired, setExpired] = useState(expiredAtStart);
    const [resending, setResending] = useState(false);
    const wait = useSecondsUntil(resendAt);
    const schema = z.object({ code: z.string().regex(/^\d{6}$/, t`Код містить 6 цифр.`) });
    const { control, handleSubmit, formState, reset } = useForm({ resolver: zodResolver(schema), defaultValues: { code: "" } });

    /**
     * Checks the code.
     * @param values The form values.
     */
    async function verify(values: z.infer<typeof schema>): Promise<void> {
        try {
            onVerified(await activationApi.verify(values.code));
        } catch (failure) {
            reset({ code: "" });
            if (failure instanceof ApiError && failure.code === "no_activation") {
                toast(failure.detail, { tone: "warning" });
                await onRestart();
                return;
            }

            if (failure instanceof ApiError && failure.code === "activation_expired") {
                setExpired(true);
            }

            toast(describeError(failure, t`Не вдалося зв'язатися з сервером. Спробуйте ще раз.`), { tone: "error" });
        }
    }

    /**
     * Requests a new code for the same e-mail.
     */
    async function resend(): Promise<void> {
        setResending(true);
        try {
            const status = await activationApi.start(email);
            toast(t`Новий код надіслано.`, { tone: "info" });
            onResent(status, answeredAt());
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося зв'язатися з сервером. Спробуйте ще раз.`), { tone: "error" });
        } finally {
            setResending(false);
        }
    }

    const submit = handleSubmit(verify);
    const minutes = Math.floor(wait / 60);
    const seconds = String(wait % 60).padStart(2, "0");

    return (
        <>
            <AuthHeader
                icon={MailCheck}
                title={t`Введіть код із листа`}
                pageTitle={t`Активація облікового запису`}
                text={
                    <Trans>
                        Якщо обліковий запис <strong>{email}</strong> очікує активації, на цю адресу надіслано лист із 6-значним кодом. Код
                        діє кілька хвилин і підходить лише один раз.
                    </Trans>
                }
            />

            {expired ? (
                <p className={styles.note}>{t`Цей код більше не діє. Надішліть новий — попередні коди перестануть діяти.`}</p>
            ) : (
                <form className={styles.form} noValidate onSubmit={submit}>
                    <Field label={t`Код із листа`} error={formState.errors.code?.message}>
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
                    <Button type="submit" size="lg" block iconEnd={ArrowRight} busy={formState.isSubmitting}>
                        {t`Підтвердити`}
                    </Button>
                </form>
            )}

            <div className={styles.actions}>
                <Button
                    variant={expired ? "primary" : "secondary"}
                    icon={RotateCw}
                    busy={resending}
                    disabled={wait > 0}
                    onClick={() => void resend()}
                >
                    {wait > 0 ? t`Надіслати ще раз через ${minutes}:${seconds}` : t`Надіслати код ще раз`}
                </Button>
                <Button variant="ghost" icon={PencilLine} onClick={() => void onRestart()}>
                    {t`Змінити пошту`}
                </Button>
            </div>

            <p className={styles.note}>
                {t`Лист не прийшов? Перевірте папку «Спам» і правильність адреси. Якщо обліковий запис ще не створено, зверніться до адміністратора.`}
            </p>
        </>
    );
}

/**
 * Props of {@link PasswordStep}.
 */
interface PasswordStepProps {
    /** The account whose code was entered (null after a reload). */
    readonly account: ActivationVerifiedResponse | null;
    /** Called with the next sign-in step once the password was saved. */
    readonly onSaved: (next: string) => void;
    /** Called when the activation can no longer continue. */
    readonly onExpired: () => Promise<void>;
}

/**
 * Step 3: the user's own password.
 * @param props The props.
 * @returns The step.
 */
function PasswordStep({ account, onSaved, onExpired }: PasswordStepProps) {
    const { t } = useLingui();
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
        try {
            const { next } = await activationApi.password(values.newPassword);
            onSaved(next);
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося зв'язатися з сервером. Спробуйте ще раз.`), { tone: "error" });
            if (failure instanceof ApiError && (failure.code === "activation_expired" || failure.code === "no_activation")) {
                await onExpired();
            }
        }
    }

    const name = account?.firstName;
    return (
        <>
            <AuthHeader
                icon={KeyRound}
                title={name ? t`Вітаємо, ${name}!` : t`Придумайте пароль`}
                pageTitle={t`Активація облікового запису`}
                text={t`Пошту підтверджено. Придумайте пароль — його знатимете лише Ви: адміністратори його не бачать і не надсилають.`}
            />
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
