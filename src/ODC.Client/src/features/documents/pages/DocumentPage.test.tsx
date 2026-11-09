import { act, fireEvent, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { SigningWizardProps } from "@/shared/signing/SigningWizard";
import { makeUserRef, type Schemas } from "@/test/factories";
import { navigatedTo, renderPage } from "@/test/render";
import { mockApi, problem } from "@/test/server";
import { DocumentPage } from "./DocumentPage";

/** The props the signing wizard stand-in received, newest last. */
const wizardProps: SigningWizardProps[] = [];

/** How many times the signing wizard stand-in was mounted. */
const wizardMounts = { count: 0 };

// The wizard has its own tests (shared/signing); here it is a stand-in that shows what the page passes to it.
vi.mock("@/shared/signing/SigningWizard", async () => {
    const { useEffect } = await import("react");
    return {
        /**
         * Stands in for the signing wizard.
         * @param props The props of the real wizard.
         * @returns Buttons that report a busy state and a finished signature.
         */
        SigningWizard(props: SigningWizardProps) {
            wizardProps.push(props);
            useEffect(() => {
                wizardMounts.count++;
            }, []);
            return (
                <div data-testid="wizard" data-active={String(props.active)}>
                    <button type="button" onClick={() => props.onBusyChange?.(true)}>
                        wizard busy
                    </button>
                    <button type="button" onClick={() => props.onBusyChange?.(false)}>
                        wizard idle
                    </button>
                    <button type="button" onClick={() => props.onSigned?.()}>
                        wizard signed
                    </button>
                </div>
            );
        },
    };
});

afterEach(() => {
    wizardProps.length = 0;
    wizardMounts.count = 0;
    vi.useRealTimers();
});

/** No action allowed. */
const noActions: Schemas["DocumentActions"] = {
    canEdit: false,
    canSubmit: false,
    canWithdraw: false,
    canSign: false,
    canReject: false,
    canArchive: false,
    canDelete: false,
};

/**
 * A signature of the document.
 * @param overrides Changed fields.
 * @returns The signature.
 */
function makeSignature(overrides: Partial<Schemas["DocumentSignatureItem"]> = {}): Schemas["DocumentSignatureItem"] {
    return {
        id: "sig-1",
        userId: "user-2",
        userName: "Коваленко Марія Андріївна",
        format: "CAdES",
        packaging: "Detached",
        level: "B-LT",
        keyMedia: "File",
        certificate: {
            subjectFullName: "КОВАЛЕНКО МАРІЯ АНДРІЇВНА",
            subjectOrganization: null,
            subjectTaxId: "1234567890",
            issuerName: "КНЕДП ДПС",
            serialNumber: "58E2D9E7F900DD6E",
            validFrom: null,
            validTo: null,
        },
        signedAt: "2026-11-03T10:00:00Z",
        fileName: "order.pdf.p7s",
        size: 4096,
        ...overrides,
    };
}

/**
 * A signer of the route.
 * @param overrides Changed fields.
 * @returns The signer.
 */
function makeSigner(overrides: Partial<Schemas["DocumentSignerItem"]> = {}): Schemas["DocumentSignerItem"] {
    return {
        userId: "user-2",
        fullName: "Коваленко Марія Андріївна",
        position: "Начальник відділу",
        order: 1,
        status: "Pending",
        decidedAt: null,
        comment: null,
        photoId: null,
        ...overrides,
    };
}

/**
 * A document as the card shows it.
 * @param overrides Changed fields.
 * @returns The document.
 */
function makeDocument(overrides: Partial<Schemas["DocumentDetails"]> = {}): Schemas["DocumentDetails"] {
    return {
        id: "doc-1",
        registrationNumber: "2026/0001",
        title: "Про затвердження графіка відпусток",
        description: null,
        kind: "Order",
        status: "Draft",
        signingMode: "Sequential",
        author: makeUserRef({ id: "user-1", fullName: "Шевченко Тарас Григорович" }),
        unit: { id: "unit-1", name: "Відділ кадрів" },
        file: { fileName: "order.pdf", contentType: "application/pdf", size: 2048, sha256: "ab".repeat(32), isPdf: true },
        createdAt: "2026-11-01T09:30:00Z",
        submittedAt: null,
        completedAt: null,
        signers: [],
        signatures: [],
        actions: noActions,
        combinedSignatureId: null,
        ...overrides,
    };
}

/**
 * Mocks the document card.
 * @param document The document (or a function answering every load).
 * @returns The recorded calls.
 */
function mockDocument(document: Schemas["DocumentDetails"]) {
    return mockApi("get", "/api/v1/documents/{id}", document);
}

/**
 * Renders the card of document `doc-1`.
 * @returns The rendered page.
 */
function renderDocument() {
    return renderPage(<DocumentPage />, { path: "/documents/:id", url: "/documents/doc-1" });
}

/**
 * Waits for the card to load.
 * @returns The heading.
 */
async function loaded() {
    return screen.findByRole("heading", { level: 1, name: "Про затвердження графіка відпусток" });
}

/**
 * The open confirmation or form dialog with the given title.
 * @param title The title.
 * @returns Queries inside the dialog.
 */
async function dialog(title: string) {
    const heading = await screen.findByRole("heading", { name: title });
    return within(heading.closest("dialog") as HTMLElement);
}

describe("DocumentPage", () => {
    it("shows a skeleton while the document loads", async () => {
        mockApi("get", "/api/v1/documents/{id}", () => new Promise(() => undefined));
        renderDocument();

        expect(await screen.findByRole("status", { name: "Завантаження" })).toBeInTheDocument();
    });

    it("says the document was not found and links back to the registry", async () => {
        mockApi("get", "/api/v1/documents/{id}", () => problem(404, "not_found", "Документ не знайдено або видалено."));
        const { user } = renderDocument();

        expect(await screen.findByText("Документ не знайдено")).toBeInTheDocument();
        expect(screen.getByText("Документ не існує або у Вас немає до нього доступу.")).toBeInTheDocument();
        // The answer is the page's own state, not a failure: no toast.
        expect(screen.queryByRole("alert")).not.toBeInTheDocument();
        await user.click(screen.getByRole("link", { name: "До реєстру" }));
        expect(await navigatedTo()).toBe("/documents");
    });

    it("loads the document by the id of the address", async () => {
        const calls = mockDocument(makeDocument());
        renderDocument();

        await loaded();
        expect(calls[0]?.path).toBe("/api/v1/documents/doc-1");
    });

    it("shows the header with the number, the state and the kind", async () => {
        mockDocument(makeDocument({ status: "Signed", kind: "Contract" }));
        renderDocument();

        await loaded();
        const breadcrumbs = within(screen.getByRole("navigation", { name: "Навігаційний ланцюжок" }));
        expect(breadcrumbs.getByRole("link", { name: "Документи" })).toHaveAttribute("href", "/documents");
        expect(breadcrumbs.getByText("№ 2026/0001")).toHaveAttribute("aria-current", "page");
        // The page header holds the breadcrumbs; other banners (dialog headers) are not part of it.
        const header = within(screen.getByRole("navigation", { name: "Навігаційний ланцюжок" }).closest("header") ?? document.body);
        expect(header.getByText("Підписано")).toBeInTheDocument();
        expect(header.getByText("Договір")).toBeInTheDocument();
        expect(header.getAllByText("№ 2026/0001").length).toBeGreaterThan(0);
    });

    it("shows the file with its size, type, SHA-256 and a download link", async () => {
        mockDocument(makeDocument());
        renderDocument();

        await loaded();
        expect(screen.getByText("order.pdf")).toBeInTheDocument();
        expect(screen.getByText("2 КБ · application/pdf")).toBeInTheDocument();
        expect(screen.getByText("ab".repeat(32))).toBeInTheDocument();
        const download = screen.getByRole("link", { name: "Завантажити" });
        expect(download).toHaveAttribute("href", "/api/v1/documents/doc-1/file");
        expect(download).toHaveAttribute("download", "");
    });

    it("lists the details of the document", async () => {
        mockDocument(
            makeDocument({
                kind: "Memo",
                signingMode: "Parallel",
                submittedAt: "2026-11-02T08:00:00Z",
                description: "Графік на 2027 рік.",
            }),
        );
        renderDocument();

        await loaded();
        expect(screen.getByText("Реєстраційний номер").nextElementSibling).toHaveTextContent("2026/0001");
        expect(screen.getByText("Вид").nextElementSibling).toHaveTextContent("Службова записка");
        expect(screen.getByRole("link", { name: "Відділ кадрів" })).toHaveAttribute("href", "/structure/unit-1");
        expect(screen.getByText("Автор").nextElementSibling).toHaveTextContent("Шевченко Тарас Григорович");
        expect(screen.getByText("Порядок підписання").nextElementSibling).toHaveTextContent("Паралельно");
        expect(screen.getByText("Створено").nextElementSibling).toHaveTextContent("01.11.2026, 11:30");
        expect(screen.getByText("Надіслано на підпис").nextElementSibling).toHaveTextContent("02.11.2026, 10:00");
        expect(screen.getByText("Завершено").nextElementSibling).toHaveTextContent("—");
        expect(screen.getByText("Графік на 2027 рік.")).toBeInTheDocument();
    });

    it("says there are no signatures and no signers yet", async () => {
        mockDocument(makeDocument());
        renderDocument();

        await loaded();
        expect(screen.getByText("Підписів ще немає")).toBeInTheDocument();
        expect(screen.getByText("Підписантів не додано")).toBeInTheDocument();
        expect(screen.queryByText(/Це оригінал без підписів/)).not.toBeInTheDocument();
        expect(screen.queryByText("Документ з усіма підписами")).not.toBeInTheDocument();
    });

    it("lists the signatures with their certificates and signature files", async () => {
        mockDocument(
            makeDocument({
                status: "InSigning",
                signatures: [
                    makeSignature(),
                    makeSignature({
                        id: "sig-2",
                        format: "PAdES",
                        packaging: "Enveloped",
                        level: null,
                        certificate: { ...makeSignature().certificate, subjectFullName: "ІВАНЕНКО ПЕТРО", subjectTaxId: null },
                    }),
                ],
            }),
        );
        renderDocument();

        await loaded();
        expect(screen.getByText("КОВАЛЕНКО МАРІЯ АНДРІЇВНА")).toBeInTheDocument();
        expect(screen.getByText("CAdES B-LT · окремо від даних (зовнішній) · 03.11.2026, 12:00")).toBeInTheDocument();
        expect(screen.getAllByText(/КНЕДП ДПС · сертифікат №/)[0]).toHaveTextContent(
            "КНЕДП ДПС · сертифікат № 58E2D9E7F900DD6E · 1234567890",
        );
        expect(screen.getByText("PAdES · з даними (внутрішній) · 03.11.2026, 12:00")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "Файл підпису" })).toHaveAttribute("href", "/api/v1/documents/doc-1/signatures/sig-1/file");
        expect(screen.getByRole("link", { name: "Підписаний PDF" })).toHaveAttribute(
            "href",
            "/api/v1/documents/doc-1/signatures/sig-2/file",
        );
        expect(screen.getByText("Це оригінал без підписів. Підписані файли завантажуйте в розділі «Підписи».")).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "«Перевірка підпису»" })).toHaveAttribute("href", "/signing/verify");
    });

    it("offers the document with all signatures of a cumulative chain", async () => {
        mockDocument(
            makeDocument({
                status: "Signed",
                combinedSignatureId: "sig-2",
                signatures: [makeSignature(), makeSignature({ id: "sig-2" })],
            }),
        );
        renderDocument();

        await loaded();
        expect(screen.getByText("Документ з усіма підписами")).toBeInTheDocument();
        const downloads = screen.getAllByRole("link", { name: "Завантажити" });
        expect(downloads.map((link) => link.getAttribute("href"))).toEqual([
            "/api/v1/documents/doc-1/file",
            "/api/v1/documents/doc-1/signatures/sig-2/file",
        ]);
        expect(screen.getAllByRole("link", { name: "Версія після цього підпису" })).toHaveLength(2);
        expect(screen.queryByText(/Це оригінал без підписів/)).not.toBeInTheDocument();
    });

    it("shows the signing route with every signer's state", async () => {
        mockDocument(
            makeDocument({
                status: "Rejected",
                signers: [
                    makeSigner({ userId: "u1", fullName: "Перший Підписант", status: "Signed", decidedAt: "2026-11-03T10:00:00Z" }),
                    makeSigner({
                        userId: "u2",
                        order: 2,
                        fullName: "Другий Підписант",
                        position: null,
                        status: "Rejected",
                        decidedAt: "2026-11-04T10:00:00Z",
                        comment: "Невірна дата в пункті 2.",
                    }),
                    makeSigner({ userId: "u3", order: 3, fullName: "Третій Підписант" }),
                ],
            }),
        );
        renderDocument();

        await loaded();
        expect(screen.getByText("Перший Підписант")).toBeInTheDocument();
        expect(screen.getByText("Підписав(-ла) · 03.11.2026, 12:00")).toBeInTheDocument();
        expect(screen.getByText("Відмовив(-ла) · 04.11.2026, 12:00")).toBeInTheDocument();
        expect(screen.getByText("Невірна дата в пункті 2.")).toBeInTheDocument();
        expect(screen.getByText("Третій Підписант")).toBeInTheDocument();
        expect(screen.getAllByText("Очікується підпис").length).toBeGreaterThan(0);
        expect(screen.getAllByText("Начальник відділу")).toHaveLength(2);
    });

    it("shows no action the server does not allow", async () => {
        mockDocument(makeDocument());
        renderDocument();

        await loaded();
        for (const name of ["Підписати", "Відмовити", "На підписання", "Відкликати", "В архів", "Видалити"]) {
            expect(screen.queryByRole("button", { name })).not.toBeInTheDocument();
        }

        expect(screen.queryByRole("link", { name: "Редагувати" })).not.toBeInTheDocument();
        expect(screen.queryByText("Документ очікує Вашого підпису")).not.toBeInTheDocument();
    });

    it("shows every action the server allows", async () => {
        mockDocument(
            makeDocument({
                actions: {
                    canEdit: true,
                    canSubmit: true,
                    canWithdraw: true,
                    canSign: true,
                    canReject: true,
                    canArchive: true,
                    canDelete: true,
                },
            }),
        );
        renderDocument();

        await loaded();
        for (const name of ["Підписати", "Відмовити", "На підписання", "Відкликати", "В архів", "Видалити"]) {
            expect(screen.getByRole("button", { name })).toBeInTheDocument();
        }

        expect(screen.getByRole("link", { name: "Редагувати" })).toHaveAttribute("href", "/documents/doc-1/edit");
    });

    it("explains how to fix a rejected document to its editor", async () => {
        mockDocument(makeDocument({ status: "Rejected", actions: { ...noActions, canEdit: true } }));
        renderDocument();

        await loaded();
        expect(screen.getByText("Документ відхилено")).toBeInTheDocument();
    });

    it("does not explain the fix to someone who cannot edit the rejected document", async () => {
        mockDocument(makeDocument({ status: "Rejected" }));
        renderDocument();

        await loaded();
        expect(screen.queryByText("Документ відхилено")).not.toBeInTheDocument();
    });

    describe("status changes", () => {
        it.each([
            ["На підписання", "Надіслати документ на підписання?", "InSigning", "Документ надіслано на підписання", "canSubmit"],
            ["Відкликати", "Відкликати документ у чернетки?", "Draft", "Документ повернуто в чернетки", "canWithdraw"],
            ["В архів", "Перемістити документ в архів?", "Archived", "Документ переміщено в архів", "canArchive"],
        ] as const)("«%s» asks «%s» and sets the state %s", async (button, question, status, done, action) => {
            const loads = mockDocument(makeDocument({ actions: { ...noActions, [action]: true } }));
            const calls = mockApi("put", "/api/v1/documents/{id}/status", 204);
            const { user } = renderDocument();

            await loaded();
            await user.click(screen.getByRole("button", { name: button }));
            await user.click((await dialog(question)).getByRole("button", { name: "Підтвердити" }));

            expect(await screen.findByText(done)).toBeInTheDocument();
            expect(calls).toHaveLength(1);
            expect(calls[0]?.path).toBe("/api/v1/documents/doc-1/status");
            expect(calls[0]?.body).toEqual({ status });
            expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
            // The card reloads after the change.
            await waitFor(() => expect(loads).toHaveLength(2));
        });

        it("does nothing when the question is cancelled", async () => {
            mockDocument(makeDocument({ actions: { ...noActions, canSubmit: true } }));
            const calls = mockApi("put", "/api/v1/documents/{id}/status", 204);
            const { user } = renderDocument();

            await loaded();
            await user.click(screen.getByRole("button", { name: "На підписання" }));
            await user.click((await dialog("Надіслати документ на підписання?")).getByRole("button", { name: "Скасувати" }));

            await waitFor(() =>
                expect(screen.queryByRole("heading", { name: "Надіслати документ на підписання?" })).not.toBeInTheDocument(),
            );
            expect(calls).toHaveLength(0);
        });

        it("reports the server's refusal", async () => {
            mockDocument(makeDocument({ actions: { ...noActions, canSubmit: true } }));
            mockApi("put", "/api/v1/documents/{id}/status", () => problem(409, "no_signers", "Додайте хоча б одного підписанта."));
            const { user } = renderDocument();

            await loaded();
            await user.click(screen.getByRole("button", { name: "На підписання" }));
            await user.click((await dialog("Надіслати документ на підписання?")).getByRole("button", { name: "Підтвердити" }));

            expect(await screen.findByRole("alert")).toHaveTextContent("Додайте хоча б одного підписанта.");
        });

        it("reports a failure without details with a general message", async () => {
            mockDocument(makeDocument({ actions: { ...noActions, canArchive: true } }));
            mockApi("put", "/api/v1/documents/{id}/status", 500);
            const { user } = renderDocument();

            await loaded();
            await user.click(screen.getByRole("button", { name: "В архів" }));
            await user.click((await dialog("Перемістити документ в архів?")).getByRole("button", { name: "Підтвердити" }));

            expect(await screen.findByRole("alert")).toBeInTheDocument();
        });
    });

    describe("deleting", () => {
        it("deletes after a confirmation and returns to my documents", async () => {
            mockDocument(makeDocument({ actions: { ...noActions, canDelete: true } }));
            const calls = mockApi("delete", "/api/v1/documents/{id}", 204);
            const { user, router } = renderDocument();

            await loaded();
            await user.click(screen.getByRole("button", { name: "Видалити" }));
            const confirmation = await dialog("Видалити документ остаточно?");
            await user.click(confirmation.getByRole("button", { name: "Видалити" }));

            expect(await screen.findByText("Документ видалено")).toBeInTheDocument();
            expect(await navigatedTo()).toBe("/documents");
            expect(router.state.location.state).toMatchObject({ view: "scope=Authored" });
            expect(calls).toHaveLength(1);
            expect(calls[0]?.path).toBe("/api/v1/documents/doc-1");
            expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
        });

        it("keeps the document when the deletion is cancelled", async () => {
            mockDocument(makeDocument({ actions: { ...noActions, canDelete: true } }));
            const calls = mockApi("delete", "/api/v1/documents/{id}", 204);
            const { user } = renderDocument();

            await loaded();
            await user.click(screen.getByRole("button", { name: "Видалити" }));
            await user.click((await dialog("Видалити документ остаточно?")).getByRole("button", { name: "Скасувати" }));

            await waitFor(() => expect(screen.queryByRole("heading", { name: "Видалити документ остаточно?" })).not.toBeInTheDocument());
            expect(calls).toHaveLength(0);
            expect(screen.queryByTestId("location")).not.toBeInTheDocument();
        });

        it("reports a failed deletion and stays on the card", async () => {
            mockDocument(makeDocument({ actions: { ...noActions, canDelete: true } }));
            mockApi("delete", "/api/v1/documents/{id}", () => problem(409, "has_signatures", "Підписаний документ не можна видалити."));
            const { user } = renderDocument();

            await loaded();
            await user.click(screen.getByRole("button", { name: "Видалити" }));
            await user.click((await dialog("Видалити документ остаточно?")).getByRole("button", { name: "Видалити" }));

            expect(await screen.findByRole("alert")).toHaveTextContent("Підписаний документ не можна видалити.");
            expect(screen.queryByTestId("location")).not.toBeInTheDocument();
        });
    });

    describe("rejecting", () => {
        /**
         * Renders a document the user may refuse to sign and opens the refusal dialog.
         * @returns The rendered page and the dialog.
         */
        async function openRejection() {
            const loads = mockDocument(makeDocument({ status: "InSigning", actions: { ...noActions, canReject: true } }));
            const page = renderDocument();
            await loaded();
            await page.user.click(screen.getByRole("button", { name: "Відмовити" }));
            return { ...page, loads, form: await dialog("Відмова в підписанні") };
        }

        it("asks for a reason", async () => {
            const calls = mockApi("post", "/api/v1/documents/{id}/rejections", 204);
            const { user, form } = await openRejection();

            expect(form.getByText("Автор побачить причину й зможе виправити документ.")).toBeInTheDocument();
            await user.click(form.getByRole("button", { name: "Відмовити" }));

            expect(await form.findByText("Вкажіть причину відмови.")).toBeInTheDocument();
            expect(calls).toHaveLength(0);
        });

        it("does not take a reason of spaces only", async () => {
            const calls = mockApi("post", "/api/v1/documents/{id}/rejections", 204);
            const { user, form } = await openRejection();

            await user.type(form.getByLabelText(/Причина/), "    ");
            await user.click(form.getByRole("button", { name: "Відмовити" }));

            expect(await form.findByText("Вкажіть причину відмови.")).toBeInTheDocument();
            expect(calls).toHaveLength(0);
        });

        it("limits the reason to 1000 characters", async () => {
            const calls = mockApi("post", "/api/v1/documents/{id}/rejections", 204);
            const { user, form } = await openRejection();

            const reason = form.getByLabelText(/Причина/);
            expect(reason).toHaveAttribute("maxlength", "1000");
            fireEvent.change(reason, { target: { value: "а".repeat(1001) } });
            await user.click(form.getByRole("button", { name: "Відмовити" }));

            expect(await form.findByText("Причина не може бути довшою за 1000 символів.")).toBeInTheDocument();
            expect(calls).toHaveLength(0);
        });

        it("sends the trimmed reason, closes the dialog and reloads the card", async () => {
            const calls = mockApi("post", "/api/v1/documents/{id}/rejections", 204);
            const { user, form, loads } = await openRejection();

            await user.type(form.getByLabelText(/Причина/), "  Невірна дата в пункті 2.  ");
            await user.click(form.getByRole("button", { name: "Відмовити" }));

            expect(await screen.findByText("Відмову зафіксовано")).toBeInTheDocument();
            expect(calls[0]?.path).toBe("/api/v1/documents/doc-1/rejections");
            expect(calls[0]?.body).toEqual({ reason: "Невірна дата в пункті 2." });
            expect(calls[0]?.headers.get("X-CSRF-TOKEN")).toBe("csrf-test-token");
            await waitFor(() => expect(screen.queryByRole("heading", { name: "Відмова в підписанні" })).not.toBeInTheDocument());
            await waitFor(() => expect(loads).toHaveLength(2));
        });

        it("reports a failed refusal and keeps the dialog open", async () => {
            mockApi("post", "/api/v1/documents/{id}/rejections", () => problem(409, "not_your_turn", "Зараз не Ваша черга."));
            const { user, form } = await openRejection();

            await user.type(form.getByLabelText(/Причина/), "Причина");
            await user.click(form.getByRole("button", { name: "Відмовити" }));

            expect(await screen.findByRole("alert")).toHaveTextContent("Зараз не Ваша черга.");
            expect(screen.getByRole("heading", { name: "Відмова в підписанні" })).toBeInTheDocument();
            expect(form.getByLabelText(/Причина/)).toHaveValue("Причина");
        });

        it("closes the dialog with «Скасувати»", async () => {
            const { user, form } = await openRejection();

            await user.click(form.getByRole("button", { name: "Скасувати" }));

            await waitFor(() => expect(screen.queryByRole("heading", { name: "Відмова в підписанні" })).not.toBeInTheDocument());
        });
    });

    describe("signing", () => {
        /**
         * Renders a document awaiting the user's signature.
         * @returns The rendered page and the recorded loads.
         */
        async function renderSignable() {
            const loads = mockDocument(makeDocument({ status: "InSigning", actions: { ...noActions, canSign: true } }));
            const page = renderDocument();
            await loaded();
            return { ...page, loads };
        }

        it("invites the user to sign", async () => {
            await renderSignable();

            expect(screen.getByText("Документ очікує Вашого підпису")).toBeInTheDocument();
            expect(screen.queryByRole("heading", { name: "Підписання документа" })).not.toBeInTheDocument();
        });

        it("keeps the wizard inactive until the dialog opens, then passes the document with its registered SHA-256", async () => {
            const { user } = await renderSignable();

            expect(wizardProps.at(-1)?.active).toBe(false);
            await user.click(screen.getByRole("button", { name: "Підписати" }));

            const signing = await dialog("Підписання документа");
            expect(signing.getByText("№ 2026/0001 · order.pdf")).toBeInTheDocument();
            expect(signing.getByTestId("wizard")).toHaveAttribute("data-active", "true");
            expect(wizardProps.at(-1)?.target).toEqual({
                kind: "document",
                documentId: "doc-1",
                title: "Про затвердження графіка відпусток",
                fileName: "order.pdf",
                sha256: "ab".repeat(32),
            });
        });

        it("cannot be closed while the signature is being made", async () => {
            const { user } = await renderSignable();

            await user.click(screen.getByRole("button", { name: "Підписати" }));
            const signing = await dialog("Підписання документа");
            await user.click(signing.getByRole("button", { name: "wizard busy" }));
            expect(signing.getByRole("button", { name: "Закрити" })).toBeDisabled();

            await user.click(signing.getByRole("button", { name: "wizard idle" }));
            expect(signing.getByRole("button", { name: "Закрити" })).toBeEnabled();
            await user.click(signing.getByRole("button", { name: "Закрити" }));
            await waitFor(() => expect(screen.queryByRole("heading", { name: "Підписання документа" })).not.toBeInTheDocument());
            expect(wizardProps.at(-1)?.active).toBe(false);
        });

        it("reloads the card after signing and closes the dialog with a fresh wizard a moment later", async () => {
            const { user, loads } = await renderSignable();

            await user.click(screen.getByRole("button", { name: "Підписати" }));
            const signing = await dialog("Підписання документа");
            expect(wizardMounts.count).toBe(1);
            vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
            fireEvent.click(signing.getByRole("button", { name: "wizard signed" }));

            expect(screen.getByText("Документ підписано")).toBeInTheDocument();
            expect(screen.getByRole("heading", { name: "Підписання документа" })).toBeInTheDocument();
            await act(async () => {
                await vi.advanceTimersByTimeAsync(1400);
            });
            vi.useRealTimers();

            await waitFor(() => expect(screen.queryByRole("heading", { name: "Підписання документа" })).not.toBeInTheDocument());
            await waitFor(() => expect(loads.length).toBeGreaterThanOrEqual(2));
            expect(wizardMounts.count).toBe(2);
        });
    });
});
