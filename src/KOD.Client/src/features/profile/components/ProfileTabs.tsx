import { useLingui } from "@lingui/react/macro";
import { ShieldCheck, User } from "lucide-react";
import { Tab, Tabs } from "@/shared/ui/Tabs";

/**
 * The tabs of the profile section.
 * @param props The current tab.
 * @returns The tabs.
 */
export function ProfileTabs({ current }: { readonly current: "overview" | "security" }) {
    const { t } = useLingui();
    return (
        <Tabs label={t`Розділи профілю`}>
            <Tab to="/profile" active={current === "overview"} icon={User}>
                {t`Профіль`}
            </Tab>
            <Tab to="/profile/security" active={current === "security"} icon={ShieldCheck}>
                {t`Безпека`}
            </Tab>
        </Tabs>
    );
}
