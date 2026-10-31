import { useLingui } from "@lingui/react/macro";
import { House, SearchX } from "lucide-react";
import { ButtonLink } from "@/shared/ui/Button";
import { DocumentTitle } from "@/shared/ui/DocumentTitle";
import { EmptyState } from "@/shared/ui/EmptyState";

/**
 * Shown for an address the application does not know.
 * @returns The page.
 */
export function NotFoundPage() {
    const { t } = useLingui();
    return (
        <>
            <DocumentTitle>{t`Сторінку не знайдено`}</DocumentTitle>
            <EmptyState icon={SearchX} title={t`Сторінку не знайдено`} text={t`Можливо, посилання застаріло або сторінку перенесено.`}>
                <ButtonLink to="/" icon={House}>
                    {t`На головну`}
                </ButtonLink>
            </EmptyState>
        </>
    );
}
