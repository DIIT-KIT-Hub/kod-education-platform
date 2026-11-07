import { Trans, useLingui } from "@lingui/react/macro";
import { Smartphone } from "lucide-react";
import { Link, useNavigate } from "react-router";
import layout from "@/shared/styles/layout.module.css";
import { AuthHeader } from "../components/AuthHeader";
import { AuthenticatorCodeForm } from "../components/AuthenticatorEnrollment";
import { flowUrl, useRememberMe, useReturnUrl } from "../flow";
import styles from "./auth.module.css";

/**
 * The second step of signing in: the code of the authenticator app.
 * @returns The page.
 */
export function TwoFactorPage() {
    const { t } = useLingui();
    const navigate = useNavigate();
    const returnUrl = useReturnUrl();
    const rememberMe = useRememberMe();

    return (
        <>
            <AuthHeader
                icon={Smartphone}
                title={t`Код з автентифікатора`}
                pageTitle={t`Підтвердження входу`}
                text={t`Відкрийте застосунок-автентифікатор і введіть 6-значний код для облікового запису КОД.`}
            />

            <AuthenticatorCodeForm
                rememberMe={rememberMe}
                submitText={t`Увійти`}
                onVerified={async () => {
                    await navigate(returnUrl, { replace: true });
                }}
            />

            <p className={styles.note}>
                <Trans>
                    Немає доступу до телефону?{" "}
                    <Link className={layout.link} to={flowUrl("/account/login-recovery", returnUrl, rememberMe)}>
                        Увійти за резервним кодом
                    </Link>
                    .
                </Trans>
            </p>
        </>
    );
}
