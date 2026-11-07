import { useLingui } from "@lingui/react/macro";
import { Archive, BadgeCheck, CircleX, Hourglass, Pencil, type LucideIcon } from "lucide-react";
import type { DocumentStatus } from "@/shared/api/schema";
import { documentStatusNames } from "@/shared/i18n/display";
import { Badge, type BadgeTone } from "@/shared/ui/Badge";

/**
 * The look of each state.
 */
const looks: Record<DocumentStatus, { readonly tone: BadgeTone; readonly icon: LucideIcon }> = {
    Draft: { tone: "dashed", icon: Pencil },
    InSigning: { tone: "outline", icon: Hourglass },
    Signed: { tone: "inverse", icon: BadgeCheck },
    Rejected: { tone: "danger", icon: CircleX },
    Archived: { tone: "neutral", icon: Archive },
};

/**
 * The life-cycle state of a document as a badge.
 * @param props The state.
 * @returns The badge.
 */
export function DocumentStatusBadge({ status }: { readonly status: DocumentStatus }) {
    const { t } = useLingui();
    return (
        <Badge tone={looks[status].tone} icon={looks[status].icon}>
            {t(documentStatusNames[status])}
        </Badge>
    );
}
