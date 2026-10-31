import { zodResolver } from "@hookform/resolvers/zod";
import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Briefcase,
    Camera,
    Check,
    FileText,
    History,
    Network,
    Newspaper,
    Save,
    Shield,
    ShieldCheck,
    Signature,
    Users,
    type LucideIcon,
} from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link } from "react-router";
import { z } from "zod";
import { describeError } from "@/shared/api/problem";
import type { AppModule, ProfileDto } from "@/shared/api/schema";
import { moduleNames } from "@/shared/i18n/display";
import { formatDateTime } from "@/shared/i18n/format";
import { localeNames, locales, type Locale } from "@/shared/i18n/i18n";
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
import { Spinner } from "@/shared/ui/Spinner";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { profileApi, profileKeys, profileQuery } from "../api";
import { ProfileTabs } from "../components/ProfileTabs";
import styles from "./profile.module.css";

/** The icons of the modules. */
const moduleIcons: Record<AppModule, LucideIcon> = {
    Documents: FileText,
    Signing: Signature,
    News: Newspaper,
    Structure: Network,
    Users: Users,
    Administration: Shield,
    Audit: History,
};

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
            {profile.isPending && <Spinner block />}
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
 * The e-mail (read-only) and the phone number.
 * @param props The profile.
 * @returns The card.
 */
function Contacts({ profile }: ProfileCardProps) {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const [error, setError] = useState<string | null>(null);

    const schema = z.object({
        phoneNumber: z
            .string()
            .trim()
            .max(32, t`Некоректний номер телефону.`)
            .refine((value) => value === "" || /^\+?[\d\s().-]{5,}$/u.test(value), t`Некоректний номер телефону.`),
    });

    const { register, handleSubmit, formState, reset } = useForm({
        resolver: zodResolver(schema),
        defaultValues: { phoneNumber: profile.phoneNumber ?? "" },
    });

    /**
     * Saves the phone number.
     * @param values The form values.
     */
    async function save(values: z.infer<typeof schema>): Promise<void> {
        setError(null);
        try {
            await profileApi.phone(values.phoneNumber || null);
            reset(values);
            await queryClient.invalidateQueries({ queryKey: profileKeys.all });
            toast(t`Телефон збережено.`);
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося зберегти телефон. Спробуйте ще раз.`));
        }
    }

    return (
        <Card title={t`Контакти`}>
            {error && <Alert tone="danger">{error}</Alert>}
            <form className={layout.stack} noValidate onSubmit={handleSubmit(save)}>
                <Field label={t`Електронна пошта`}>{(ids) => <TextInput {...ids} value={profile.email} disabled readOnly />}</Field>
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
            <p className={[layout.textSm, layout.muted].join(" ")}>{t`ПІБ та посади змінює відділ кадрів.`}</p>
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
            toast(describeError(failure, t`Не вдалося змінити мову.`), { danger: true });
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
                {t`Мова зберігається у профілі й застосовується на всіх ваших пристроях.`}
            </p>
        </Card>
    );
}
