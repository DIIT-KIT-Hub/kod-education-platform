import { useLingui } from "@lingui/react/macro";
import { House, Lock, RotateCw, TriangleAlert } from "lucide-react";
import { isRouteErrorResponse, useRouteError } from "react-router";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { EmptyState } from "@/shared/ui/EmptyState";
import { PageHeader } from "@/shared/ui/PageHeader";

/**
 * Shown when a page is forbidden (403 from a permission guard) or fails to load or render (e.g. the server cannot be
 * reached).
 * @returns The error view.
 */
export function RouteError() {
    const { t } = useLingui();
    const error = useRouteError();

    if (isRouteErrorResponse(error) && error.status === 403) {
        return (
            <>
                <PageHeader title={t`Доступ обмежено`} />
                <Card>
                    <EmptyState
                        icon={Lock}
                        title={t`У вас немає доступу до цього розділу`}
                        text={t`Якщо доступ потрібен для роботи, зверніться до керівника підрозділу або системного адміністратора.`}
                    >
                        <ButtonLink to="/" icon={House}>
                            {t`На головну`}
                        </ButtonLink>
                    </EmptyState>
                </Card>
            </>
        );
    }

    console.error(error);
    return (
        <EmptyState
            icon={TriangleAlert}
            title={t`Сталася помилка`}
            text={t`Сторінку не вдалося завантажити. Оновіть сторінку або спробуйте пізніше.`}
        >
            <Button icon={RotateCw} onClick={() => window.location.reload()}>
                {t`Оновити`}
            </Button>
        </EmptyState>
    );
}
