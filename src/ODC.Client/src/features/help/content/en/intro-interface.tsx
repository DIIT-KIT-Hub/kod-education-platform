import { Bell, ChevronDown, ChevronLeft, Link2, Menu, Moon, Sun } from "lucide-react";
import { Icon } from "@/shared/ui/Icon";
import { Figure, Ref, Ui } from "../../components/Manual";
import homeImage from "../../images/en/home.webp";
import sidebarImage from "../../images/en/sidebar.webp";

/**
 * The manual page "intro-interface" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <h2>Menu</h2>
            <Figure
                src={homeImage}
                alt="The home page of the cabinet with the menu on the left"
                caption="The home page: the menu on the left, documents waiting for you and the news in the middle."
            />
            <Figure
                src={sidebarImage}
                alt="The main menu of the cabinet"
                caption="The main menu: sections fold and unfold; your profile, the theme and sign-out are at the bottom."
            />
            <ul>
                <li>
                    <strong>On a computer</strong> (screens from 1024 px) the menu is always visible on the left. At the top are{" "}
                    <Ui>Home</Ui> and <Ui>Notifications</Ui> <Icon icon={Bell} />, then the sections: <Ui>Document workflow</Ui> (
                    <Ui>Documents</Ui>, <Ui>Signing</Ui>), <Ui>Communication</Ui> (<Ui>Chats</Ui>, <Ui>News</Ui>, <Ui>Requests</Ui>),{" "}
                    <Ui>Organization</Ui> (<Ui>Structure</Ui>, <Ui>Administration</Ui>) and <Ui>Personal</Ui> (<Ui>Profile</Ui>,{" "}
                    <Ui>Help</Ui>). One section is open at a time — the one of the page you are viewing; clicking the name of another
                    section <Icon icon={ChevronDown} /> opens it and folds the previous one. A number next to <Ui>Chats</Ui> shows unread
                    messages (next to the section name while it is folded). At the bottom are your name (a link to your profile), the theme
                    switch and the sign-out button. The round <Icon icon={ChevronLeft} /> button on the edge of the menu next to the logo
                    collapses it to a narrow bar of icons (the item name appears on hover) and expands it again; your choice is remembered
                    in the browser.
                </li>
                <li>
                    <strong>On a phone</strong> there is a quick navigation bar at the bottom of the screen: <Ui>Home</Ui>,{" "}
                    <Ui>Documents</Ui>, <Ui>Chats</Ui>, <Ui>News</Ui>, <Ui>Profile</Ui>. The other items are in the <Icon icon={Menu} />{" "}
                    menu in the top-right corner.
                </li>
                <li>
                    <strong>On a tablet</strong> the menu opens with the <Icon icon={Menu} /> button in the top bar. On phones and tablets
                    the notification bell <Icon icon={Bell} /> is in the top bar.
                </li>
            </ul>
            <p>
                The menu shows only the modules you have access to. If an item is missing, you don't have the corresponding permission (see
                “<Ref to="intro-access">Roles and access</Ref>”).
            </p>
            <h2>Notifications</h2>
            <p>
                The number on the bell <Icon icon={Bell} /> is your unread notifications; it changes at once, without reloading the page.
                See “<Ref to="notifications-kinds">Notifications</Ref>” for details.
            </p>
            <h2>Theme</h2>
            <p>
                The <Icon icon={Moon} /> / <Icon icon={Sun} /> button switches between the light and dark themes. Your choice is remembered
                in the browser. By default the theme follows your system settings.
            </p>
            <h2>Notifications and confirmations</h2>
            <ul>
                <li>
                    Results of actions (for example, “Password changed”) appear as short notifications at the bottom of the screen and
                    disappear on their own.
                </li>
                <li>
                    Important actions (signing out, deleting, generating new recovery codes, etc.) require confirmation in a dialog with the{" "}
                    <Ui>Confirm</Ui> and <Ui>Cancel</Ui> buttons.
                </li>
                <li>Form errors are shown in red under the corresponding field.</li>
            </ul>
            <h2>Tables</h2>
            <p>
                Lists (documents, news, users, the audit log) have search, filters, sorting by column headers and pagination with a choice
                of the number of rows per page (10, 20, 50 or 100; in the news feed — 9, 18, 36 or 72). Search runs as soon as you stop
                typing (or at once with Enter); drop-down filters apply as soon as you pick a value. On a phone, each table row becomes a
                separate card.
            </p>
            <h2>Dates and times</h2>
            <p>All dates are shown in Kyiv time, for example “25 Oct 2026, 14:05”. A dash “—” means there is no value.</p>
            <h2>Interface language</h2>
            <p>
                The cabinet is available in Ukrainian and English. Choose the language in <Ui>Profile</Ui> → <Ui>Interface language</Ui>{" "}
                (see “<Ref to="profile-overview">My profile</Ref>”) or with the language button in the top-right corner of the sign-in page.
            </p>
            <h2>This manual</h2>
            <p>
                The table of contents on the left (on a phone — at the top, the <Ui>Contents</Ui> button) highlights the section you are
                reading. The search box at the top filters topics and highlights the words found. The <Icon icon={Link2} /> button next to a
                topic heading is a direct link you can share with a colleague. <Ui>Print / PDF</Ui> prints the manual or saves it as a PDF.
            </p>
        </>
    );
}
