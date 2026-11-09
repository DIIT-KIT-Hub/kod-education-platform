import { Building2, FileText, FolderTree, GraduationCap, Landmark } from "lucide-react";
import { describe, expect, it } from "vitest";
import { unitTypeIcon, unitTypeIcons } from "./unitTypeIcons";

/** The icon names the server allows (`UnitType.Icons`). */
const serverIcons = [
    "building-2",
    "landmark",
    "network",
    "folder-tree",
    "layers",
    "briefcase",
    "users",
    "user-cog",
    "shield",
    "scroll-text",
    "stamp",
    "book-open",
    "graduation-cap",
    "flask-conical",
    "monitor",
    "settings",
    "newspaper",
    "map-pin",
    "sparkles",
    "file-text",
];

describe("unitTypeIcons", () => {
    it("maps exactly the icon names of the server", () => {
        expect(Object.keys(unitTypeIcons)).toEqual(serverIcons);
    });

    it("maps every name to a distinct component", () => {
        expect(new Set(Object.values(unitTypeIcons)).size).toBe(serverIcons.length);
    });
});

describe("unitTypeIcon", () => {
    it.each([
        ["building-2", Building2],
        ["landmark", Landmark],
        ["graduation-cap", GraduationCap],
        ["file-text", FileText],
        ["folder-tree", FolderTree],
    ])("returns the icon of %s", (name, icon) => {
        expect(unitTypeIcon(name)).toBe(icon);
    });

    it.each(["", "unknown", "Building-2"])("falls back to the folder tree for %j", (name) => {
        expect(unitTypeIcon(name)).toBe(FolderTree);
    });

    it("falls back to the folder tree for names of Object.prototype members", () => {
        expect(unitTypeIcon("toString")).toBe(FolderTree);
        expect(unitTypeIcon("constructor")).toBe(FolderTree);
    });
});
