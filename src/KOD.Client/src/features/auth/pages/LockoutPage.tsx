import { useLingui } from "@lingui/react/macro";
import { ArrowLeft, Timer } from "lucide-react";
import { ButtonLink } from "@/shared/ui/Button";
import { AuthHeader } from "../components/AuthHeader";

/**
 * Shown after too many failed sign-in attempts.
 * @returns The page.
 */
export function LockoutPage() {
    const { t } = useLingui();
    return (
        <>
            <AuthHeader
                icon={Timer}
                title={t`Забагато спроб`}
                pageTitle={t`Вхід тимчасово заблоковано`}
                text={t`Вхід до облікового запису тимчасово заблоковано на 15 хвилин через кілька невдалих спроб. Спробуйте пізніше або зверніться до адміністратора.`}
            />
            <ButtonLink to="/account/login" variant="secondary" icon={ArrowLeft}>
                {t`До сторінки входу`}
            </ButtonLink>
        </>
    );
}
