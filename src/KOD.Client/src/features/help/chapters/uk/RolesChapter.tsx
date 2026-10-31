import { CircleCheck } from "lucide-react";
import { Icon } from "@/shared/ui/Icon";
import { Chapter, Ref, Topic, Ui } from "../../components/Manual";
import { AdministratorRoleName, PermissionReference, SystemRoleReference } from "../../components/RoleReference";

/**
 * Roles and permissions: the reference of every permission and the standard roles (Ukrainian).
 * @returns The chapter.
 */
export function RolesChapter() {
    return (
        <Chapter id="roles">
            <Topic id="roles-permissions">
                <p>
                    Дозвіл — право на конкретну дію. Нижче — усі дозволи кабінету, згруповані за модулями. Ваші власні дозволи позначено{" "}
                    <Icon icon={CircleCheck} />.
                </p>
                <PermissionReference />
                <p>
                    Кілька дозволів документів визначають, <strong>чиї</strong> документи ви бачите — див. «
                    <Ref to="documents-visibility">Чиї документи ви бачите</Ref>».
                </p>
            </Topic>
            <Topic id="roles-system">
                <p>
                    Під час розгортання КОД створює стандартні ролі, спільні для більшості організацій. Системний адміністратор може змінити
                    їхні назви й дозволи та створити власні ролі для своєї сфери — актуальний склад ролей вашого кабінету видно в{" "}
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
            </Topic>
        </Chapter>
    );
}
