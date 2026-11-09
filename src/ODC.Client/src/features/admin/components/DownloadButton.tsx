import { Download } from "lucide-react";
import { useState } from "react";
import { describeError } from "@/shared/api/problem";
import { downloadFile } from "@/shared/lib/download";
import { Button, type ButtonProps } from "@/shared/ui/Button";
import { toast } from "@/shared/ui/Toast";

/**
 * What the toasts of a download say (translated texts).
 */
export interface DownloadMessages {
    /** Shown when the button is pressed. */
    readonly started: string;
    /** Shown when the file was saved; gets the file name. */
    readonly done: (fileName: string) => string;
    /** Shown when the server failed without a message of its own. */
    readonly failed: string;
}

/**
 * Props of {@link DownloadButton}.
 */
export interface DownloadButtonProps {
    /** The API address of the file. */
    readonly href: string;
    /** The name to save under when the server gives none. */
    readonly fileName: string;
    /** What the toasts say. */
    readonly messages: DownloadMessages;
    /** The look of the button. */
    readonly variant?: ButtonProps["variant"];
    /** The size of the button. */
    readonly size?: ButtonProps["size"];
    /** The label. */
    readonly children: string;
}

/**
 * A button that downloads a file the server prepares (an export, a template): a toast says the download started,
 * the button is busy until the file is saved, and a second toast says it is done (or why it failed).
 * @param props The props.
 * @returns The button.
 */
export function DownloadButton({ href, fileName, messages, variant, size, children }: DownloadButtonProps) {
    const [busy, setBusy] = useState(false);

    /**
     * Downloads the file and reports the start and the end.
     */
    async function run(): Promise<void> {
        setBusy(true);
        toast(messages.started, { tone: "info" });
        try {
            toast(messages.done(await downloadFile(href, fileName)));
        } catch (failure) {
            toast(describeError(failure, messages.failed), { tone: "error" });
        } finally {
            setBusy(false);
        }
    }

    return (
        <Button variant={variant} size={size} icon={Download} busy={busy} onClick={() => void run()}>
            {children}
        </Button>
    );
}
