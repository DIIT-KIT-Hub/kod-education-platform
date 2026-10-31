import { msg } from "@lingui/core/macro";
import type { MessageDescriptor } from "@lingui/core";
import { useLingui } from "@lingui/react/macro";
import { ArrowRight, FileSpreadsheet, History, Layers, Network, Newspaper, Shield, Users, type LucideIcon } from "lucide-react";
import { Link } from "react-router";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Icon } from "@/shared/ui/Icon";
import { PageHeader } from "@/shared/ui/PageHeader";
import styles from "./admin.module.css";

/**
 * A section of the administration.
 */
interface AdminSection {
    readonly to: string;
    readonly title: MessageDescriptor;
    readonly description: MessageDescriptor;
    readonly icon: LucideIcon;
    /** Any of them reveals the section. */
    readonly permissions: readonly string[];
}

/** The sections in their order. */
const sections: readonly AdminSection[] = [
    {
        to: "/admin/users",
        title: msg`Користувачі`,
        description: msg`Облікові записи, посади, блокування, скидання пароля та 2FA.`,
        icon: Users,
        permissions: [Permissions.usersView],
    },
    {
        to: "/admin/users/import",
        title: msg`Імпорт і експорт`,
        description: msg`Масове додавання користувачів з XLSX/CSV та вивантаження.`,
        icon: FileSpreadsheet,
        permissions: [Permissions.usersImport, Permissions.usersExport],
    },
    {
        to: "/admin/roles",
        title: msg`Ролі та дозволи`,
        description: msg`Набори дозволів для посад і модулів.`,
        icon: Shield,
        permissions: [Permissions.rolesManage],
    },
    {
        to: "/admin/units",
        title: msg`Структура`,
        description: msg`Організації та їхні підрозділи: департаменти, відділи, філії.`,
        icon: Network,
        permissions: [Permissions.structureManage],
    },
    {
        to: "/admin/unit-types",
        title: msg`Типи підрозділів`,
        description: msg`Назви рівнів структури: організація, департамент, відділ, сектор…`,
        icon: Layers,
        permissions: [Permissions.structureManage],
    },
    {
        to: "/news/manage",
        title: msg`Новини`,
        description: msg`Публікації та оголошення.`,
        icon: Newspaper,
        permissions: [Permissions.newsManage],
    },
    {
        to: "/admin/audit",
        title: msg`Журнал аудиту`,
        description: msg`Входи, зміни прав, підписання документів.`,
        icon: History,
        permissions: [Permissions.auditView],
    },
];

/**
 * The administration hub: only the sections the user has permissions for.
 * @returns The page.
 */
export function AdminPage() {
    const { t } = useLingui();
    const user = useCurrentUser();

    return (
        <>
            <PageHeader title={t`Адміністрування`} subtitle={t`Облікові записи, ролі, організації та їхня структура, журнал подій.`} />
            <div className={[styles.sections, layout.stagger].join(" ")}>
                {sections
                    .filter((section) => section.permissions.some((permission) => hasPermission(user, permission)))
                    .map((section) => (
                        <Link key={section.to} className={styles.section} to={section.to}>
                            <span className={styles.sectionTop}>
                                <span className={styles.tile}>
                                    <Icon icon={section.icon} />
                                </span>
                                <Icon icon={ArrowRight} />
                            </span>
                            <span className={styles.sectionTitle}>{t(section.title)}</span>
                            <span className={[layout.textSm, layout.muted].join(" ")}>{t(section.description)}</span>
                        </Link>
                    ))}
            </div>
        </>
    );
}
