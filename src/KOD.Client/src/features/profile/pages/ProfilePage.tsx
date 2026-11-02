import { zodResolver } from "@hookform/resolvers/zod";
import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Briefcase, Camera, Check, Save, ShieldCheck } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link } from "react-router";
import { z } from "zod";
import { describeError } from "@/shared/api/problem";
import type { ProfileDto } from "@/shared/api/schema";
import { moduleNames } from "@/shared/i18n/display";
import { formatDateTime } from "@/shared/i18n/format";
import { localeNames, locales, type Locale } from "@/shared/i18n/i18n";
import { moduleIcons } from "@/shared/lib/moduleIcons";
import { reloadAuthContext } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Avatar } from "@/shared/ui/Avatar";
import { Badge } from "@/shared/ui/Badge";
import { Button } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { EmptyState } from "@/shared/ui/EmptyState";
import { Field } from "@/shared/ui/Field";
import { Icon } from "@/shared/ui/Icon";
import { List, ListItem } from "@/shared/ui/List";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Segmented } from "@/shared/ui/Segmented";
import { SkeletonPage } from "@/shared/ui/Skeleton";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { profileApi, profileKeys, profileQuery } from "../api";
import { ProfileTabs } from "../components/ProfileTabs";
import styles from "./profile.module.css";

/**
 * The profile overview: personal data, positions, modules and permissions, contacts and the interface language.
 * @returns The page.
 */
export function ProfilePage() {
    const { t } = useLingui();
    const profile = useQuery(profileQuery);

    return (
        <>
            <PageHeader title={t`Мій профіль`} subtitle={t`Особисті дані, посади та доступні можливості.`} />
            <ProfileTabs current="overview" />
            {profile.isPending && <SkeletonPage header={false} />}
            {profile.isError && <Alert tone="danger">{describeError(profile.error, t`Не вдалося завантажити профіль.`)}</Alert>}
            {profile.data && (
                <div className={layout.split}>
                    <div className={[layout.stackLg, layout.stagger].join(" ")}>
                        <Identity profile={profile.data} />
                        <Positions profile={profile.data} />
                        <Modules profile={profile.data} />
                    </div>
                    <div className={layout.stackLg}>
                        <Contacts profile={profile.data} />
                        <Language />
                    </div>
                </div>
            )}
        </>
    );
}

/**
 * Props of the profile cards.
 */
interface ProfileCardProps {
    /** The profile. */
    readonly profile: ProfileDto;
}

/**
 * The photo, name, e-mail and sign-in state.
 * @param props The profile.
 * @returns The card.
 */
function Identity({ profile }: ProfileCardProps) {
    const { t } = useLingui();
    return (
        <Card>
            <div className={styles.identity}>
                <Link className={styles.photo} to="/account/photo" title={t`Змінити фото`}>
                    <Avatar name={profile.fullName} photoId={profile.photoId} size="lg" decorative />
                    <span className={styles.photoEdit} aria-hidden="true">
                        <Icon icon={Camera} size="sm" />
                    </span>
                    <span className={layout.visuallyHidden}>{t`Змінити фото`}</span>
                </Link>
                <div className={layout.stackSm}>
                    <h2 className={styles.name}>{profile.fullName}</h2>
                    <span className={layout.muted}>{profile.email}</span>
                    <div className={layout.cluster}>
                        {profile.twoFactorEnabled && (
                            <Badge tone="inverse" icon={ShieldCheck}>
                                {t`2FA увімкнено`}
                            </Badge>
                        )}
                        <Badge tone="dashed">{t`Останній вхід: ${formatDateTime(profile.lastSignInAt)}`}</Badge>
                    </div>
                </div>
            </div>
        </Card>
    );
}

/**
 * The positions of the user, linked to their units.
 * @param props The profile.
 * @returns The card.
 */
function Positions({ profile }: ProfileCardProps) {
    const { t } = useLingui();
    return (
        <Card title={t`Посади`}>
            {profile.positions.length === 0 ? (
                <EmptyState icon={Briefcase} title={t`Посади не призначено`} text={t`Зверніться до відділу кадрів.`} />
            ) : (
                <List>
                    {profile.positions.map((position) => (
                        <ListItem
                            key={`${position.unitId}:${position.roleId}`}
                            to={`/structure/${position.unitId}`}
                            start={
                                <span className={styles.tile}>
                                    <Icon icon={Briefcase} />
                                </span>
                            }
                            title={position.title ?? position.roleName}
                            meta={`${position.unitName} · ${t`роль «${position.roleName}»`}`}
                            end={position.isPrimary && <Badge tone="outline">{t`Основна`}</Badge>}
                        />
                    ))}
                </List>
            )}
        </Card>
    );
}

/**
 * The modules the user can open and every effective permission.
 * @param props The profile.
 * @returns The card.
 */
function Modules({ profile }: ProfileCardProps) {
    const { t } = useLingui();
    return (
        <Card title={t`Доступні модулі`}>
            <div className={styles.modules}>
                {profile.modules.map((module) => (
                    <div key={module} className={layout.row}>
                        <span className={styles.tile}>
                            <Icon icon={moduleIcons[module]} />
                        </span>
                        <span className={layout.strong}>{t(moduleNames[module])}</span>
                    </div>
                ))}
            </div>
            <details className={styles.permissions}>
                <summary>{t`Усі мої дозволи (${profile.permissions.length})`}</summary>
                <List>
                    {profile.permissions.map((permission) => (
                        <ListItem
                            key={permission.code}
                            start={<Icon icon={Check} size="sm" />}
                            title={permission.title ?? permission.code}
                            meta={permission.description}
                        />
                    ))}
                </List>
            </details>
        </Card>
    );
}

/**
 * The name, the e-mail and the phone number. Everyone changes the phone; the name and the e-mail (the login) only users
 * who administer their whole account (a system administrator always), the others ask their administrator or HR.
 * @param props The profile.
 * @returns The card.
 */
function Contacts({ profile }: ProfileCardProps) {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const editable = profile.canEditPersonalData;

    const schema = z.object({
        lastName: z
            .string()
            .trim()
            .min(1, t`Вкажіть прізвище.`)
            .max(64),
        firstName: z
            .string()
            .trim()
            .min(1, t`Вкажіть ім'я.`)
            .max(64),
        middleName: z.string().trim().max(64),
        email: z
            .string()
            .trim()
            .min(1, t`Вкажіть електронну пошту.`)
            .max(256)
            .pipe(z.email(t`Некоректна адреса.`)),
        phoneNumber: z
            .string()
            .trim()
            .max(32, t`Некоректний номер телефону.`)
            .refine((value) => value === "" || /^\+?[\d\s().-]{5,}$/u.test(value), t`Некоректний номер телефону.`),
    });

    const { register, handleSubmit, formState, reset } = useForm({
        resolver: zodResolver(schema),
        defaultValues: {
            lastName: profile.lastName,
            firstName: profile.firstName,
            middleName: profile.middleName ?? "",
            email: profile.email,
            phoneNumber: profile.phoneNumber ?? "",
        },
    });

    /**
     * Saves the changes; the name and the e-mail are sent only when the user may change them.
     * @param values The form values.
     */
    async function save(values: z.infer<typeof schema>): Promise<void> {
        try {
            await profileApi.update(
                editable
                    ? { ...values, phoneNumber: values.phoneNumber || null }
                    : { lastName: null, firstName: null, middleName: null, email: null, phoneNumber: values.phoneNumber || null },
            );
            reset(values);
            await queryClient.invalidateQueries({ queryKey: profileKeys.all });
            if (editable) {
                // The shell shows the name from the auth context, re-issued with the new tokens.
                await reloadAuthContext(queryClient);
            }

            toast(t`Зміни збережено.`);
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося зберегти зміни. Спробуйте ще раз.`), { tone: "error" });
        }
    }

    return (
        <Card title={editable ? t`Особисті дані` : t`Контакти`}>
            <form className={layout.stack} noValidate onSubmit={handleSubmit(save)}>
                {editable && (
                    <>
                        <Field label={t`Прізвище`} required error={formState.errors.lastName?.message}>
                            {(ids) => <TextInput {...ids} {...register("lastName")} autoComplete="family-name" maxLength={64} />}
                        </Field>
                        <Field label={t`Ім'я`} required error={formState.errors.firstName?.message}>
                            {(ids) => <TextInput {...ids} {...register("firstName")} autoComplete="given-name" maxLength={64} />}
                        </Field>
                        <Field label={t`По батькові`}>
                            {(ids) => <TextInput {...ids} {...register("middleName")} autoComplete="additional-name" maxLength={64} />}
                        </Field>
                    </>
                )}
                <Field label={t`Електронна пошта (логін)`} required={editable} error={formState.errors.email?.message}>
                    {(ids) =>
                        editable ? (
                            <TextInput
                                {...ids}
                                {...register("email")}
                                type="email"
                                inputMode="email"
                                autoComplete="email"
                                maxLength={256}
                            />
                        ) : (
                            <TextInput {...ids} value={profile.email} disabled readOnly />
                        )
                    }
                </Field>
                <Field label={t`Телефон`} error={formState.errors.phoneNumber?.message}>
                    {(ids) => (
                        <TextInput {...ids} {...register("phoneNumber")} type="tel" placeholder="+380…" autoComplete="tel" maxLength={32} />
                    )}
                </Field>
                <div>
                    <Button type="submit" variant="secondary" icon={Save} busy={formState.isSubmitting} disabled={!formState.isDirty}>
                        {t`Зберегти`}
                    </Button>
                </div>
            </form>
            <p className={[layout.textSm, layout.muted].join(" ")}>
                {editable
                    ? t`Посади змінюються в адмініструванні користувачів.`
                    : t`ПІБ, електронну пошту та посади змінює адміністратор Вашої організації або відділ кадрів.`}
            </p>
        </Card>
    );
}

/**
 * The interface language: saved in the profile, so it follows the user to every device, and applied at once.
 * @returns The card.
 */
function Language() {
    const { i18n, t } = useLingui();
    const queryClient = useQueryClient();
    const [busy, setBusy] = useState(false);

    /**
     * Changes the language.
     * @param language The language code.
     */
    async function change(language: Locale): Promise<void> {
        setBusy(true);
        try {
            await profileApi.language(language);
            await reloadAuthContext(queryClient);
            // Some data comes translated by the server (permission titles, role and unit type names): load it again.
            await queryClient.invalidateQueries({ predicate: (query) => query.queryKey[0] !== "auth" });
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося змінити мову.`), { tone: "error" });
        } finally {
            setBusy(false);
        }
    }

    return (
        <Card title={t`Мова інтерфейсу`}>
            <fieldset className={styles.fieldset} disabled={busy}>
                <Segmented
                    label={t`Мова інтерфейсу`}
                    value={i18n.locale as Locale}
                    options={locales.map((locale) => ({
                        value: locale,
                        label: <span lang={locale}>{localeNames[locale]}</span>,
                        hint: locale.toUpperCase(),
                    }))}
                    onChange={(language) => void change(language)}
                />
            </fieldset>
            <p className={[layout.textSm, layout.muted].join(" ")}>
                {t`Мова зберігається у профілі й застосовується на всіх Ваших пристроях.`}
            </p>
        </Card>
    );
}
