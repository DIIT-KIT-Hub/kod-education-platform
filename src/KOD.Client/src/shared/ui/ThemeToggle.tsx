import { useLingui } from "@lingui/react/macro";
import { Moon, Sun } from "lucide-react";
import { toggleTheme, useTheme } from "@/shared/lib/preferences";
import { Button } from "./Button";

/**
 * Props of {@link ThemeToggle}.
 */
export interface ThemeToggleProps {
    /** Shows the word "Theme" next to the icon. */
    readonly labelled?: boolean;
}

/**
 * Switches between the light and the dark theme (remembered in this browser).
 * @param props The props.
 * @returns The button.
 */
export function ThemeToggle({ labelled }: ThemeToggleProps) {
    const { t } = useLingui();
    const theme = useTheme();
    const icon = theme === "dark" ? Sun : Moon;
    return labelled ? (
        <Button variant="ghost" size="sm" icon={icon} onClick={toggleTheme}>
            {t`Тема`}
        </Button>
    ) : (
        <Button variant="ghost" size="sm" iconOnly icon={icon} onClick={toggleTheme}>
            {t`Змінити тему`}
        </Button>
    );
}
