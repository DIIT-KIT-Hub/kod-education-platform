import profileImage from "../../images/en/profile.webp";
import { Figure, Ui } from "../../components/Manual";

/**
 * The manual page "profile-overview" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Open <Ui>Profile</Ui> in the menu or click your name at the bottom of the side menu. The profile has two tabs:{" "}
                <Ui>Profile</Ui> and <Ui>Security</Ui>.
            </p>
            <Figure src={profileImage} alt="The profile page" caption="My profile: the photo, contacts, positions and language." />
            <p>
                On the <Ui>Profile</Ui> tab:
            </p>
            <ul>
                <li>
                    <strong>Card</strong> — full name, email, the <Ui>2FA enabled</Ui> badge and the time of your last sign-in.
                </li>
                <li>
                    <strong>Positions</strong> — all your positions: title, unit and role. Your primary position is marked <Ui>Primary</Ui>.
                    Click a position to open the unit's page. If you have no positions — “No positions assigned. Contact the HR department.”
                </li>
                <li>
                    <strong>Available modules</strong> — the modules of the cabinet open to you. Expand <Ui>All my permissions</Ui> to see
                    each permission with its description.
                </li>
                <li>
                    <strong>Contacts</strong> — <Ui>Email (login)</Ui> and <Ui>Phone</Ui>. Enter the number in the +380… format and click{" "}
                    <Ui>Save</Ui> — “Changes saved.” appears.
                </li>
            </ul>
            <p>
                Your name, email and positions are changed by the administrator of your organization or the HR department. If the data is
                wrong or you lack access to a module, contact them or the system administrator.
            </p>
            <p>
                A user administrator who manages every organization of their positions (the system administrator always) sees{" "}
                <Ui>Personal data</Ui> instead of this card and changes their own last name, first name, middle name and email. The new
                email becomes the login at once. Positions are changed in the user administration.
            </p>
            <p>
                <strong>Interface language.</strong> The cabinet is available in Ukrainian and English. In the <Ui>Interface language</Ui>{" "}
                card, choose <Ui>Українська</Ui> or <Ui>English</Ui> — the language changes at once, without reloading the page. The
                language is saved in your profile and applies on all your devices. Before signing in, you can change the language with the
                button in the top-right corner of the sign-in page.
            </p>
        </>
    );
}
