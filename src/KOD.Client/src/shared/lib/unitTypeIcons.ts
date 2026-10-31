import {
    BookOpen,
    Briefcase,
    Building2,
    FileText,
    FlaskConical,
    FolderTree,
    GraduationCap,
    Landmark,
    Layers,
    MapPin,
    Monitor,
    Network,
    Newspaper,
    ScrollText,
    Settings,
    Shield,
    Sparkles,
    Stamp,
    UserCog,
    Users,
    type LucideIcon,
} from "lucide-react";

/**
 * The icons that can mark a unit type, by their Lucide names (the server keeps the list in `UnitType.Icons`).
 */
export const unitTypeIcons: Readonly<Record<string, LucideIcon>> = {
    "building-2": Building2,
    landmark: Landmark,
    network: Network,
    "folder-tree": FolderTree,
    layers: Layers,
    briefcase: Briefcase,
    users: Users,
    "user-cog": UserCog,
    shield: Shield,
    "scroll-text": ScrollText,
    stamp: Stamp,
    "book-open": BookOpen,
    "graduation-cap": GraduationCap,
    "flask-conical": FlaskConical,
    monitor: Monitor,
    settings: Settings,
    newspaper: Newspaper,
    "map-pin": MapPin,
    sparkles: Sparkles,
    "file-text": FileText,
};

/**
 * The icon of a unit type.
 * @param name The icon name stored with the type.
 * @returns The icon, or the folder tree for an unknown name.
 */
export function unitTypeIcon(name: string): LucideIcon {
    return unitTypeIcons[name] ?? FolderTree;
}
