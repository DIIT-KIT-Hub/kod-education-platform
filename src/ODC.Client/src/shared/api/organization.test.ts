import { describe, expect, it } from "vitest";
import { apiUrl, inOrganization, isElsewhere, linkTo, organizationBase, organizationCodeOf } from "./organization";

describe("organization addresses", () => {
    it("reads the organization code from an address", () => {
        expect(organizationCodeOf("/o/FUND/documents/1")).toBe("FUND");
        expect(organizationCodeOf("/o/fund")).toBe("fund");
        expect(organizationCodeOf("/documents/1")).toBeNull();
        expect(organizationCodeOf("/office/1")).toBeNull();
        expect(organizationCodeOf("/o/bad_code/x")).toBeNull();
    });

    it("keeps the page's addresses at the root in the default organization", () => {
        expect(organizationBase).toBe("");
        expect(apiUrl("/api/v1/documents/1/file")).toBe("/api/v1/documents/1/file");
        expect(inOrganization(null, "/news")).toBe("/news");
        expect(inOrganization("FUND", "/news")).toBe("/o/FUND/news");
    });

    it("links an item of another organization to that organization's address", () => {
        expect(isElsewhere("FUND", "ORG")).toBe(true);
        expect(isElsewhere("org", "ORG")).toBe(false);
        expect(isElsewhere(null, "ORG")).toBe(false);
        expect(linkTo("FUND", "ORG", "/chats/c-1")).toEqual({ href: "/o/FUND/chats/c-1" });
        expect(linkTo("ORG", "ORG", "/chats/c-1")).toEqual({ to: "/chats/c-1" });
        expect(linkTo(null, "ORG", "/news/a")).toEqual({ to: "/news/a" });
    });
});
