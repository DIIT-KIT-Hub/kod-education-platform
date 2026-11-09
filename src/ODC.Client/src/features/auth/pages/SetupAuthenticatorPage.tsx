import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, ArrowRight } from "lucide-react";
import { useState } from "react";
import { describeError } from "@/shared/api/problem";
import { Alert } from "@/shared/ui/Alert";
import { ButtonLink } from "@/shared/ui/Button";
import { SkeletonText } from "@/shared/ui/Skeleton";
import { authApi } from "../api";
import { AuthHeader } from "../components/AuthHeader";
import { LinkAuthenticator, RecoveryCodes } from "../components/AuthenticatorEnrollment";
import { useRememberMe, useReturnUrl } from "../flow";

/**
 * The sign-in of an account without an authenticator (an administrator reset it): the authenticator app is linked
 * and the recovery codes are shown once. New accounts link it inside the activation.
 * @returns The page.
 */
export function SetupAuthenticatorPage() {
    const { t } = useLingui();
    const returnUrl = useReturnUrl();
    const rememberMe = useRememberMe();
    const pending = useQuery({
        queryKey: ["auth", "pending"],
        queryFn: ({ signal }) => authApi.pending(signal),
        retry: false,
        staleTime: Infinity,
    });
    const [codes, setCodes] = useState<readonly string[] | null>(null);

    if (codes) {
        return (
            <RecoveryCodes codes={codes}>
                <ButtonLink to={returnUrl} iconEnd={ArrowRight}>
                    {t`Перейти до кабінету`}
                </ButtonLink>
            </RecoveryCodes>
        );
    }

    if (pending.isPending) {
        return <SkeletonText lines={5} />;
    }

    if (pending.isError) {
        return (
            <>
                <AuthHeader title={t`Захистіть вхід`} pageTitle={t`Підключення автентифікатора`} />
                <Alert tone="danger">{describeError(pending.error, t`Не вдалося зв'язатися з сервером. Спробуйте ще раз.`)}</Alert>
                <ButtonLink to="/account/login" variant="secondary" icon={ArrowLeft}>
                    {t`До сторінки входу`}
                </ButtonLink>
            </>
        );
    }

    return <LinkAuthenticator rememberMe={rememberMe} onEnrolled={setCodes} />;
}
