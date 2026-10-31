import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { Languages } from "lucide-react";
import { useState } from "react";
import { describeError } from "@/shared/api/problem";
import { localeNames, locales } from "@/shared/i18n/i18n";
import { reloadAuthContext } from "@/shared/session/session";
import { Button } from "@/shared/ui/Button";
import { toast } from "@/shared/ui/Toast";
import { authApi } from "../api";

/**
 * Switches the interface language: saved in the profile of a signed-in user and remembered in this browser. The new
 * language applies at once, without a page load.
 * @returns The buttons of the other languages.
 */
export function LanguageSwitch() {
    const { i18n, t } = useLingui();
    const queryClient = useQueryClient();
    const [busy, setBusy] = useState(false);

    /**
     * Changes the language.
     * @param language The language code.
     */
    async function change(language: string): Promise<void> {
        setBusy(true);
        try {
            await authApi.language(language);
            await reloadAuthContext(queryClient);
        } catch (error) {
            toast(describeError(error, t`Не вдалося змінити мову.`), { danger: true });
        } finally {
            setBusy(false);
        }
    }

    return locales
        .filter((locale) => locale !== i18n.locale)
        .map((locale) => (
            <Button
                key={locale}
                variant="ghost"
                size="sm"
                icon={Languages}
                lang={locale}
                title={t`Мова інтерфейсу`}
                busy={busy}
                onClick={() => void change(locale)}
            >
                {localeNames[locale]}
            </Button>
        ));
}
