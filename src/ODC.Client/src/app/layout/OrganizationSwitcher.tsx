import { useLingui } from "@lingui/react/macro";
import { Building2 } from "lucide-react";
import { openInOrganization } from "@/shared/api/organization";
import { useAuthContext } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Icon } from "@/shared/ui/Icon";
import { Select } from "@/shared/ui/Select";
import styles from "./OrganizationSwitcher.module.css";

/**
 * Props of {@link OrganizationSwitcher}.
 */
export interface OrganizationSwitcherProps {
    /** Whether the sidebar is an icon rail (only the icon shows, the name is its tooltip). */
    readonly rail?: boolean;
}

/**
 * The organization the cabinet works in now, and the switch to another one the user works in, takes part in as a guest
 * or (a platform administrator) may open. Switching loads the cabinet again at the other organization's address.
 * @param props The props.
 * @returns The block, or nothing without an organization.
 */
export function OrganizationSwitcher({ rail = false }: OrganizationSwitcherProps) {
    const { t } = useLingui();
    const { organization, organizations } = useAuthContext();
    if (!organization) {
        return null;
    }

    const name = organization.shortName ?? organization.name;
    if (rail) {
        return (
            <div className={styles.rail} title={organization.name}>
                <Icon icon={Building2} />
            </div>
        );
    }

    const access: Record<string, string> = { Member: "", Guest: t`гість`, Platform: t`адміністратор платформи` };
    return (
        <div className={styles.block}>
            <span className={[layout.textXs, layout.muted].join(" ")}>{t`Організація`}</span>
            {organizations.length > 1 ? (
                <Select
                    compact
                    aria-label={t`Перейти до іншої організації`}
                    value={organization.code}
                    options={organizations.map((o) => ({
                        value: o.code,
                        label: access[o.access] ? `${o.shortName ?? o.name} · ${access[o.access]}` : (o.shortName ?? o.name),
                    }))}
                    onChange={(code) => {
                        if (code !== organization.code) {
                            openInOrganization(code, "/");
                        }
                    }}
                />
            ) : (
                <span className={[layout.strong, layout.truncate].join(" ")} title={organization.name}>
                    {name}
                </span>
            )}
        </div>
    );
}
