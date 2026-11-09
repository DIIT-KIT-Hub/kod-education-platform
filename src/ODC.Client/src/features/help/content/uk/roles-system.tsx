import { Ui } from "../../components/Manual";
import { AdministratorRoleName, SystemRoleReference } from "../../components/RoleReference";

/**
 * The manual page "roles-system" (Ukrainian).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Під час розгортання КОД створює стандартні ролі, спільні для більшості організацій. Системний адміністратор може змінити
                їхні назви й дозволи та створити власні ролі для своєї сфери — актуальний склад ролей Вашого кабінету видно в{" "}
                <Ui>Адміністрування</Ui> → <Ui>Ролі</Ui> (за наявності доступу).
            </p>
            <SystemRoleReference />
            <p>
                Роль{" "}
                <strong>
                    «<AdministratorRoleName />»
                </strong>{" "}
                завжди має всі дозволи, зокрема нові, що з'являються в наступних версіях.
            </p>
        </>
    );
}
