import { useLingui } from "@lingui/react/macro";
import { CheckCircle2, CircleDashed, Clock, Lock, Sparkles, type LucideIcon } from "lucide-react";
import type { TicketStatus } from "@/shared/api/schema";
import { ticketStatusNames } from "@/shared/i18n/display";
import { Badge, type BadgeTone } from "@/shared/ui/Badge";

/**
 * The look of each state: new requests stand out, work in progress pulses, closed ones fade.
 */
const looks: Record<TicketStatus, { readonly tone: BadgeTone; readonly icon?: LucideIcon; readonly dot?: boolean }> = {
    New: { tone: "inverse", icon: Sparkles },
    InProgress: { tone: "neutral", dot: true },
    WaitingForAuthor: { tone: "outline", icon: Clock },
    Resolved: { tone: "neutral", icon: CheckCircle2 },
    Closed: { tone: "dashed", icon: Lock },
};

/**
 * The state of a request as a badge.
 * @param props The state.
 * @returns The badge.
 */
export function TicketStatusBadge({ status }: { readonly status: TicketStatus }) {
    const { t } = useLingui();
    const look = looks[status];
    return (
        <Badge tone={look.tone} icon={look.icon ?? (look.dot ? undefined : CircleDashed)} dot={look.dot}>
            {t(ticketStatusNames[status])}
        </Badge>
    );
}
