import { FileText, History, Network, Newspaper, Shield, Signature, Users, type LucideIcon } from "lucide-react";
import type { AppModule } from "@/shared/api/schema";

/**
 * The icons of the modules (groups of permissions).
 */
export const moduleIcons: Record<AppModule, LucideIcon> = {
    Documents: FileText,
    Signing: Signature,
    News: Newspaper,
    Structure: Network,
    Users: Users,
    Administration: Shield,
    Audit: History,
};
