import { useLingui } from "@lingui/react/macro";
import type { ReactNode } from "react";
import type { AppModule, PermissionDefinition } from "@/shared/api/schema";
import { moduleNames } from "@/shared/i18n/display";
import { moduleIcons } from "@/shared/lib/moduleIcons";
import { Icon } from "@/shared/ui/Icon";
import styles from "../pages/admin.module.css";

/**
 * Props of {@link PermissionModules}.
 */
export interface PermissionModulesProps {
    /** The permission catalog. */
    readonly permissions: readonly PermissionDefinition[];
    /** Renders the control of a permission. */
    readonly children: (permission: PermissionDefinition) => ReactNode;
}

/**
 * The permission catalog grouped by module, in the catalog order.
 * @param props The catalog and the control of a permission.
 * @returns The groups.
 */
export function PermissionModules({ permissions, children }: PermissionModulesProps) {
    const { t } = useLingui();
    const modules = new Map<AppModule, PermissionDefinition[]>();
    for (const permission of permissions) {
        modules.set(permission.module, [...(modules.get(permission.module) ?? []), permission]);
    }

    return (
        <div className={styles.modules}>
            {[...modules].map(([module, items]) => (
                <section key={module} className={styles.module}>
                    <span className={styles.moduleTitle}>
                        <Icon icon={moduleIcons[module]} />
                        {t(moduleNames[module])}
                    </span>
                    <div className={styles.moduleItems}>{items.map((permission) => children(permission))}</div>
                </section>
            ))}
        </div>
    );
}
