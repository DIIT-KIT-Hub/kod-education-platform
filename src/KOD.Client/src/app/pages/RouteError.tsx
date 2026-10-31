import { useLingui } from "@lingui/react/macro";
import { RotateCw, TriangleAlert } from "lucide-react";
import { useRouteError } from "react-router";
import { Button } from "@/shared/ui/Button";
import { EmptyState } from "@/shared/ui/EmptyState";

/**
 * Shown when a page fails to load or render (e.g. the server cannot be reached).
 * @returns The error view.
 */
export function RouteError() {
    const { t } = useLingui();
    console.error(useRouteError());
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
