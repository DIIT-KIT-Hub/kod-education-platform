import type { ReactNode } from "react";
import { Link } from "react-router";
import { isElsewhere } from "@/shared/api/organization";
import { useOrganization } from "@/shared/session/session";

/**
 * Props of {@link OrganizationLink}.
 */
export interface OrganizationLinkProps {
    /** The code of the item's organization (lists gather items from all the user's organizations). */
    readonly organizationCode: string | null | undefined;
    /** The application path of the item (`/news/…`). */
    readonly to: string;
    /** The class of the link. */
    readonly className?: string | undefined;
    /** The content. */
    readonly children: ReactNode;
}

/**
 * A link to an item that may live in another organization: a router link inside the page's organization, a plain link
 * to the other organization's address otherwise (the cabinet loads again there).
 * @param props The props.
 * @returns The link.
 */
export function OrganizationLink({ organizationCode, to, className, children }: OrganizationLinkProps) {
    const organization = useOrganization();
    return isElsewhere(organizationCode, organization?.code) ? (
        <a className={className} href={`/o/${organizationCode}${to}`}>
            {children}
        </a>
    ) : (
        <Link className={className} to={to}>
            {children}
        </Link>
    );
}
