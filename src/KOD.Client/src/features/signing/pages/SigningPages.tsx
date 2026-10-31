import { useLingui } from "@lingui/react/macro";
import { BadgeCheck, Signature } from "lucide-react";
import { SigningWizard } from "@/shared/signing/SigningWizard";
import layout from "@/shared/styles/layout.module.css";
import { Card } from "@/shared/ui/Card";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Tab, Tabs } from "@/shared/ui/Tabs";
import { SigningInfo } from "../components/SigningInfo";
import { VerifyPanel } from "../components/VerifyPanel";

/**
 * The tabs of the signing tools.
 * @param props The current tab.
 * @returns The tabs.
 */
function SigningTabs({ current }: { readonly current: "sign" | "verify" }) {
    const { t } = useLingui();
    return (
        <Tabs label={t`Інструменти підпису`}>
            <Tab to="/signing" active={current === "sign"} icon={Signature}>
                {t`Підписати`}
            </Tab>
            <Tab to="/signing/verify" active={current === "verify"} icon={BadgeCheck}>
                {t`Перевірити`}
            </Tab>
        </Tabs>
    );
}

/**
 * Signs any local files with a qualified electronic signature; the files and the key never leave the device.
 * @returns The page.
 */
export function SignFilesPage() {
    const { t } = useLingui();
    return (
        <>
            <PageHeader
                title={t`Підписання файлів`}
                subtitle={t`Накладіть кваліфікований електронний підпис на будь-який файл. Підпис створюється у вашому браузері — файл і ключ нікуди не передаються.`}
            />
            <SigningTabs current="sign" />
            <div className={layout.split}>
                <Card>
                    <SigningWizard target={{ kind: "files" }} active />
                </Card>
                <SigningInfo />
            </div>
        </>
    );
}

/**
 * Verifies a signed file.
 * @returns The page.
 */
export function VerifySignaturePage() {
    const { t } = useLingui();
    return (
        <>
            <PageHeader
                title={t`Перевірка підпису`}
                subtitle={t`Перевірте підписаний файл: хто підписав, ким виданий сертифікат, час підпису та цілісність даних.`}
            />
            <SigningTabs current="verify" />
            <div className={layout.split}>
                <Card>
                    <VerifyPanel />
                </Card>
                <SigningInfo />
            </div>
        </>
    );
}
