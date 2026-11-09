import { render, screen } from "@testing-library/react";
import { I18nProvider } from "@lingui/react";
import { KeyRound, ShieldCheck } from "lucide-react";
import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { i18n } from "@/shared/i18n/i18n";
import { AuthHeader } from "./AuthHeader";

/**
 * Renders an element with the translations.
 * @param element The element.
 * @returns The rendered result.
 */
function renderWithI18n(element: ReactNode) {
    return render(<I18nProvider i18n={i18n}>{element}</I18nProvider>);
}

describe("AuthHeader", () => {
    it("shows the heading and uses it as the document title", () => {
        renderWithI18n(<AuthHeader title="Вхід до кабінету" />);

        const heading = screen.getByRole("heading", { level: 1, name: "Вхід до кабінету" });
        expect(heading).toHaveAttribute("tabindex", "-1");
        expect(document.title).toBe("Вхід до кабінету · КОД");
    });

    it("prefers the page title for the document and shows the explanation", () => {
        renderWithI18n(<AuthHeader title="Код з автентифікатора" pageTitle="Підтвердження входу" text="Введіть код." />);

        expect(document.title).toBe("Підтвердження входу · КОД");
        expect(screen.getByText("Введіть код.")).toBeInTheDocument();
    });

    it("renders nothing extra without an icon, a badge or a text", () => {
        const { container } = renderWithI18n(<AuthHeader title="Заголовок" />);

        expect(container.querySelector("svg")).toBeNull();
        expect(container.querySelector("p")).toBeNull();
        // The title is hoisted into the document head: only the heading stays.
        expect(container.querySelector("header")?.children).toHaveLength(1);
    });

    it("shows the icon, the large icon and the badge", () => {
        const { container } = renderWithI18n(<AuthHeader title="Заголовок" icon={KeyRound} bigIcon={ShieldCheck} badge="Крок 2" />);

        expect(container.querySelectorAll("svg")).toHaveLength(2);
        expect(screen.getByText("Крок 2")).toBeInTheDocument();
    });
});
