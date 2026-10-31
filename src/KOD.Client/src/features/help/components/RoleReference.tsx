import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { CircleCheck } from "lucide-react";
import { Fragment } from "react";
import type { AppModule, PermissionDefinition } from "@/shared/api/schema";
import { moduleNames } from "@/shared/i18n/display";
import { moduleIcons } from "@/shared/lib/moduleIcons";
import { Icon } from "@/shared/ui/Icon";
import { Spinner } from "@/shared/ui/Spinner";
import { helpPermissionsQuery, systemRolesQuery } from "../api";
import { Muted, Table } from "./Manual";
import { useManual } from "./manualContext";

/**
 * Every permission of the cabinet by module, with a mark for those the reader has (built from the server catalog).
 * @returns The tables.
 */
export function PermissionReference() {
    const { t } = useLingui();
    const { has } = useManual();
    const permissions = useQuery(helpPermissionsQuery);
    if (!permissions.data) {
        return <Spinner block />;
    }

    const modules = new Map<AppModule, PermissionDefinition[]>();
    for (const permission of permissions.data) {
        modules.set(permission.module, [...(modules.get(permission.module) ?? []), permission]);
    }

    return [...modules].map(([module, items]) => (
        <Fragment key={module}>
            <h4>
                <Icon icon={moduleIcons[module]} />
                {t(moduleNames[module])}
            </h4>
            <Table>
                <thead>
                    <tr>
                        <th>{t`Дозвіл`}</th>
                        <th>{t`Що дає`}</th>
                        <th>{t`У вас`}</th>
                    </tr>
                </thead>
                <tbody>
                    {items.map((permission) => (
                        <tr key={permission.code}>
                            <td>{permission.title}</td>
                            <td>{permission.description}</td>
                            <td>{has(permission.code) ? <Icon icon={CircleCheck} label={t`Є`} /> : <Muted>—</Muted>}</td>
                        </tr>
                    ))}
                </tbody>
            </Table>
        </Fragment>
    ));
}

/**
 * The standard roles with their purpose and default permissions.
 * @returns The table.
 */
export function SystemRoleReference() {
    const { t } = useLingui();
    const { permissionTitles } = useManual();
    const roles = useQuery(systemRolesQuery);
    if (!roles.data) {
        return <Spinner block />;
    }

    return (
        <Table>
            <thead>
                <tr>
                    <th>{t`Роль`}</th>
                    <th>{t`Призначення`}</th>
                    <th>{t`Стандартні дозволи`}</th>
                </tr>
            </thead>
            <tbody>
                {roles.data.map((role) => (
                    <tr key={role.code}>
                        <td>{role.name}</td>
                        <td>{role.description}</td>
                        <td>{role.permissions.map((code) => permissionTitles.get(code) ?? code).join(", ")}</td>
                    </tr>
                ))}
            </tbody>
        </Table>
    );
}

/**
 * The name of the system administrator role in the interface language.
 * @returns The name.
 */
export function AdministratorRoleName() {
    const roles = useQuery(systemRolesQuery);
    return roles.data?.find((role) => role.code === "system-admin")?.name ?? "";
}
