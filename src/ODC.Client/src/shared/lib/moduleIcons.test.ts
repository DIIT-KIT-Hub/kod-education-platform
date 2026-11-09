import { FileText, History, LifeBuoy, MessagesSquare, Network, Newspaper, Shield, Signature, Users } from "lucide-react";
import { describe, expect, it } from "vitest";
import { moduleIcons } from "./moduleIcons";

describe("moduleIcons", () => {
    it("gives every module its icon", () => {
        expect(moduleIcons).toEqual({
            Documents: FileText,
            Signing: Signature,
            News: Newspaper,
            Structure: Network,
            Users: Users,
            Administration: Shield,
            Audit: History,
            Chats: MessagesSquare,
            Tickets: LifeBuoy,
        });
    });
});
