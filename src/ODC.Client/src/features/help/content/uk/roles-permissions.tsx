import { CircleCheck } from "lucide-react";
import { Icon } from "@/shared/ui/Icon";
import { Ref } from "../../components/Manual";
import { PermissionReference } from "../../components/RoleReference";

/**
 * The manual page "roles-permissions" (Ukrainian).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Дозвіл — право на конкретну дію. Нижче — усі дозволи кабінету, згруповані за модулями. Ваші власні дозволи позначено{" "}
                <Icon icon={CircleCheck} />.
            </p>
            <PermissionReference />
            <p>
                Кілька дозволів документів визначають, <strong>чиї</strong> документи Ви бачите — див. «
                <Ref to="documents-visibility">Чиї документи Ви бачите</Ref>».
            </p>
        </>
    );
}
