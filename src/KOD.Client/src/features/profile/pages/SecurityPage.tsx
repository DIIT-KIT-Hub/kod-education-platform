import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { KeyRound, LogOut, RefreshCw, ShieldAlert, ShieldCheck } from "lucide-react";
import { useState } from "react";
import { describeError } from "@/shared/api/problem";
import type { ProfileDto } from "@/shared/api/schema";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Badge } from "@/shared/ui/Badge";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { confirm } from "@/shared/ui/ConfirmHost";
import { CopyButton } from "@/shared/ui/CopyButton";
import { Field } from "@/shared/ui/Field";
import { OtpInput } from "@/shared/ui/OtpInput";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Spinner } from "@/shared/ui/Spinner";
import { toast } from "@/shared/ui/Toast";
import { profileApi, profileKeys, profileQuery } from "../api";
import { ProfileTabs } from "../components/ProfileTabs";
import styles from "./profile.module.css";

/**
 * Security settings: password, two-factor authentication, recovery codes and the sign-ins on other devices.
 * @returns The page.
 */
export function SecurityPage() {
    const { t } = useLingui();
    const profile = useQuery(profileQuery);

    return (
        <>
            <PageHeader title={t`Безпека`} subtitle={t`Пароль, двофакторна автентифікація, резервні коди та входи на інших пристроях.`} />
            <ProfileTabs current="security" />
            {profile.isPending && <Spinner block />}
            {profile.isError && <Alert tone="danger">{describeError(profile.error, t`Не вдалося завантажити профіль.`)}</Alert>}
            {profile.data && (
                <div className={[layout.cols, layout.stagger].join(" ")}>
                    <Card title={t`Пароль`} subtitle={t`Регулярно змінюйте пароль і не використовуйте його в інших сервісах.`}>
                        <div>
                            <ButtonLink to="/account/change-password" icon={KeyRound}>
                                {t`Змінити пароль`}
                            </ButtonLink>
                        </div>
                    </Card>

                    <Card title={t`Двофакторна автентифікація`} subtitle={t`Код із застосунку-автентифікатора при кожному вході.`}>
                        <div className={layout.cluster}>
                            {profile.data.twoFactorEnabled ? (
                                <Badge tone="inverse" icon={ShieldCheck}>
                                    {t`Увімкнено`}
                                </Badge>
                            ) : (
                                <Badge tone="danger" icon={ShieldAlert}>
                                    {t`Не налаштовано`}
                                </Badge>
                            )}
                        </div>
                        <p className={[layout.textSm, layout.muted].join(" ")}>
                            {t`Змінили телефон? Попросіть адміністратора скинути автентифікатор — під час наступного входу ви прив'яжете новий.`}
                        </p>
                    </Card>

                    <RecoveryCodes profile={profile.data} />
                    <OtherDevices />
                </div>
            )}
        </>
    );
}

/**
 * The number of unused recovery codes and a new set after the authenticator code is confirmed (a valid session alone
 * is not enough to replace them).
 * @param props The profile.
 * @returns The card.
 */
function RecoveryCodes({ profile }: { readonly profile: ProfileDto }) {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const [code, setCode] = useState("");
    const [codes, setCodes] = useState<readonly string[] | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    /**
     * Replaces the recovery codes after a confirmation.
     */
    async function regenerate(): Promise<void> {
        if (code.length !== 6) {
            setError(t`Введіть 6 цифр із застосунку-автентифікатора.`);
            return;
        }

        if (!(await confirm({ title: t`Згенерувати нові коди? Старі перестануть діяти.`, confirmText: t`Нові коди` }))) {
            return;
        }

        setBusy(true);
        setError(null);
        try {
            const result = await profileApi.recoveryCodes(code);
            setCodes(result.codes);
            await queryClient.invalidateQueries({ queryKey: profileKeys.all });
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося згенерувати коди. Спробуйте ще раз.`));
        } finally {
            setCode("");
            setBusy(false);
        }
    }

    return (
        <Card title={t`Резервні коди`} subtitle={t`Для входу без телефону. Кожен код діє один раз.`}>
            <p className={styles.counter}>
                <span className={styles.counterValue}>{profile.recoveryCodesLeft}</span>
                <span className={layout.muted}>{t`кодів залишилось`}</span>
            </p>
            {error && <Alert tone="danger">{error}</Alert>}
            {codes && (
                <div className={styles.secret}>
                    <span className={layout.strong}>{t`Нові коди — збережіть їх зараз, вони більше не відображатимуться:`}</span>
                    <ul className={styles.codes}>
                        {codes.map((item) => (
                            <li key={item}>{item}</li>
                        ))}
                    </ul>
                    <div>
                        <CopyButton value={codes.join("\n")} size="sm">
                            {t`Скопіювати`}
                        </CopyButton>
                    </div>
                </div>
            )}
            {profile.twoFactorEnabled && (
                <form
                    className={styles.stepUp}
                    noValidate
                    onSubmit={(event) => {
                        event.preventDefault();
                        void regenerate();
                    }}
                >
                    <Field label={t`Код із застосунку-автентифікатора`}>
                        {(ids) => <OtpInput {...ids} value={code} onChange={setCode} disabled={busy} />}
                    </Field>
                    <Button type="submit" variant="secondary" icon={RefreshCw} busy={busy}>
                        {t`Нові коди`}
                    </Button>
                </form>
            )}
        </Card>
    );
}

/**
 * Ends the sign-ins on every other device; this browser gets new tokens.
 * @returns The card.
 */
function OtherDevices() {
    const { t } = useLingui();
    const [busy, setBusy] = useState(false);

    /**
     * Signs out elsewhere after a confirmation.
     */
    async function signOutEverywhere(): Promise<void> {
        if (!(await confirm({ title: t`Вийти на всіх інших пристроях?`, confirmText: t`Вийти на інших пристроях` }))) {
            return;
        }

        setBusy(true);
        try {
            await profileApi.signOutEverywhere();
            toast(t`Входи на інших пристроях завершено`);
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося виконати дію. Спробуйте ще раз.`), { danger: true });
        } finally {
            setBusy(false);
        }
    }

    return (
        <Card
            title={t`Інші пристрої`}
            subtitle={t`Залишили вхід на чужому комп'ютері або загубили телефон? Завершіть усі входи, крім цього браузера.`}
        >
            <p className={[layout.textSm, layout.muted].join(" ")}>
                {t`Інші пристрої втратять доступ протягом кількох хвилин — щойно закінчиться строк їхнього токена доступу.`}
            </p>
            <div>
                <Button variant="secondary" icon={LogOut} busy={busy} onClick={() => void signOutEverywhere()}>
                    {t`Вийти на інших пристроях`}
                </Button>
            </div>
        </Card>
    );
}
