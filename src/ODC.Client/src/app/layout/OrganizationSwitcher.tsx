import { useLingui } from "@lingui/react/macro";
import { ArrowRight, Building2 } from "lucide-react";
import { openInOrganization } from "@/shared/api/organization";
import type { OrganizationAccess } from "@/shared/api/schema";
import { hasPermission, Permissions, useAuthContext, useCurrentUser } from "@/shared/session/session";
import { Badge } from "@/shared/ui/Badge";
import { ButtonLink } from "@/shared/ui/Button";
import { Icon } from "@/shared/ui/Icon";
import { Select } from "@/shared/ui/Select";
import styles from "./OrganizationSwitcher.module.css";

/**
 * The bar of the organization the cabinet works in, at the top of every page: its full name, short name and code, why
 * the user is there (works there, a guest, the platform's administrator), the switch to another organization (the
 * cabinet loads again at that organization's address) and, for a platform administrator, the registry of all of them.
 * @returns The bar, or nothing without an organization.
 */
export function OrganizationSwitcher() {
    const { t } = useLingui();
    const { organization, organizations } = useAuthContext();
    const platform = hasPermission(useCurrentUser(), Permissions.administrationAll);
    if (!organization) {
        return null;
    }

    const accessNames: Record<OrganizationAccess, string> = {
        Member: t`Ви тут працюєте`,
        Guest: t`Гість`,
        Platform: t`Адміністратор платформи`,
        System: "",
        Anonymous: "",
    };
    const label = (code: string, name: string, shortName: string | null, access: OrganizationAccess) =>
        [shortName ? `${shortName} — ${name}` : name, access === "Member" ? "" : accessNames[access], code].filter(Boolean).join(" · ");

    return (
        <section className={styles.bar} aria-label={t`Поточна організація`}>
            <span className={styles.tile} aria-hidden="true">
                <Icon icon={Building2} />
            </span>
            <div className={styles.text}>
                <span className={styles.eyebrow}>{t`Організація`}</span>
                <strong className={styles.name} title={organization.name}>
                    {organization.name}
                </strong>
                <span className={styles.meta}>
                    {organization.shortName && <span>{organization.shortName}</span>}
                    <span className={styles.code}>{organization.code}</span>
                    {accessNames[organization.access] && (
                        <Badge tone={organization.access === "Member" ? "outline" : "inverse"}>{accessNames[organization.access]}</Badge>
                    )}
                </span>
            </div>
            <div className={styles.actions}>
                {organizations.length > 1 && (
                    <div className={styles.switch}>
                        <Select
                            compact
                            aria-label={t`Перейти до іншої організації`}
                            value={organization.code}
                            options={organizations.map((o) => ({ value: o.code, label: label(o.code, o.name, o.shortName, o.access) }))}
                            onChange={(code) => {
                                if (code !== organization.code) {
                                    openInOrganization(code, "/");
                                }
                            }}
                        />
                    </div>
                )}
                {platform && (
                    <ButtonLink to="/admin/organizations" variant="secondary" size="sm" iconEnd={ArrowRight}>
                        {t`Усі організації`}
                    </ButtonLink>
                )}
            </div>
        </section>
    );
}
