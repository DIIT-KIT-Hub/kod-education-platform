import { Chapter, Ref, Steps, Topic, Ui } from "../../components/Manual";

/**
 * Profile (English).
 * @returns The chapter.
 */
export function ProfileChapter() {
    return (
        <Chapter id="profile">
            <Topic id="profile-overview">
                <p>
                    Open <Ui>Profile</Ui> in the menu or click your name at the bottom of the side menu. The profile has two tabs:{" "}
                    <Ui>Profile</Ui> and <Ui>Security</Ui>.
                </p>
                <p>
                    On the <Ui>Profile</Ui> tab:
                </p>
                <ul>
                    <li>
                        <strong>Card</strong> — full name, email, the <Ui>2FA enabled</Ui> badge and the time of your last sign-in.
                    </li>
                    <li>
                        <strong>Positions</strong> — all your positions: title, unit and role. Your primary position is marked{" "}
                        <Ui>Primary</Ui>. Click a position to open the unit's page. If you have no positions — “No positions assigned.
                        Contact the HR department.”
                    </li>
                    <li>
                        <strong>Available modules</strong> — the modules of the cabinet open to you. Expand <Ui>All my permissions</Ui> to
                        see each permission with its description.
                    </li>
                    <li>
                        <strong>Contacts</strong> — your email (cannot be changed, it is your login) and <Ui>Phone</Ui>. Enter the number in
                        the +380… format and click <Ui>Save</Ui> — “Phone number saved.” appears.
                    </li>
                </ul>
                <p>
                    Your name and positions are managed by the HR department. If the data is wrong or you lack access to a module, contact
                    them or the system administrator.
                </p>
                <p>
                    <strong>Interface language.</strong> The cabinet is available in Ukrainian and English. In the{" "}
                    <Ui>Interface language</Ui> card, choose <Ui>Українська</Ui> or <Ui>English</Ui> — the language changes at once, without
                    reloading the page. The language is saved in your profile and applies on all your devices. Before signing in, you can
                    change the language with the button in the top-right corner of the sign-in page.
                </p>
            </Topic>
            <Topic id="profile-photo">
                <p>
                    A profile photo is required: colleagues see it next to your name in documents and signing routes, news and comments, the
                    structure, user lists and the audit log. Until you have a photo, the cabinet opens the <Ui>Add a profile photo</Ui> page
                    after you sign in — the other sections become available as soon as you save one.
                </p>
                <Steps>
                    <li>
                        Click <Ui>Choose or drop a photo</Ui> and pick a file: JPEG, PNG, WebP, AVIF or GIF up to 15 MB.
                    </li>
                    <li>
                        In the round frame, drag the photo with the mouse or a finger (or the arrow keys) and zoom with the slider or the
                        mouse wheel so the face is inside the circle.
                    </li>
                    <li>
                        Click <Ui>Save photo</Ui> — “Profile photo saved” appears.
                    </li>
                </Steps>
                <p>
                    To change the photo later, click the photo on the <Ui>Profile</Ui> tab (the camera icon). The new photo replaces the
                    previous one everywhere, and the old one is deleted.
                </p>
                <p>
                    The photo is compressed on the server to AVIF (a few kilobytes), and the image metadata, including location, is removed.
                    Only signed-in users of the cabinet can see photos.
                </p>
            </Topic>
            <Topic id="profile-security">
                <p>
                    The <Ui>Security</Ui> tab has four cards. The first three are described below, the <Ui>Other devices</Ui> card — in “
                    <Ref to="profile-sessions">Signing out on other devices</Ref>”.
                </p>
                <h4>Password</h4>
                <p>
                    The <Ui>Change password</Ui> button opens the password change form (see “
                    <Ref to="account-change-password">Changing your password</Ref>”).
                </p>
                <h4>Two-factor authentication</h4>
                <p>
                    Shows the status: <Ui>Enabled</Ui> or <Ui>Not set up</Ui>. Changed your phone? Ask the administrator to reset your
                    authenticator — you will set up the new one the next time you sign in.
                </p>
                <h4>Recovery codes</h4>
                <p>Shows how many codes are left. If you are running low on codes or suspect someone has seen them, generate a new set:</p>
                <Steps>
                    <li>
                        Open the authenticator app and enter the current 6-digit code in the field next to the <Ui>New codes</Ui> button.
                        This is an extra confirmation: without your phone the codes cannot be changed, even by someone who has access to
                        your signed-in browser.
                    </li>
                    <li>
                        Click <Ui>New codes</Ui> and confirm “Generate new codes? The old ones will stop working.”
                    </li>
                    <li>
                        10 new codes appear. Click <Ui>Copy</Ui> and save them — they will not be shown again. The old codes no longer work.
                    </li>
                </Steps>
                <p>
                    A wrong code shows the message “The code is incorrect or has already been used. Enter the next code from the app.” Wrong
                    codes count towards the failed attempts limit (5 attempts — blocked for 15 minutes).
                </p>
            </Topic>
            <Topic id="profile-sessions">
                <p>
                    The <Ui>Other devices</Ui> card on the <Ui>Security</Ui> tab ends all your sign-ins except this browser. You will need
                    it if you left yourself signed in on someone else's computer or lost your phone.
                </p>
                <Steps>
                    <li>
                        Open <Ui>Profile</Ui> → the <Ui>Security</Ui> tab.
                    </li>
                    <li>
                        In the <Ui>Other devices</Ui> card, click <Ui>Sign out on other devices</Ui> and confirm “Sign out on all other
                        devices?”.
                    </li>
                    <li>The message “Signed out on other devices” appears.</li>
                </Steps>
                <p>
                    You stay signed in in this browser. Other devices lose access <strong>within a few minutes</strong> — as soon as their
                    access token expires (at most 5 minutes). After that, a new sign-in is needed on those devices.
                </p>
                <p>
                    If you suspect someone else knows your password, change it (see “
                    <Ref to="account-change-password">Changing your password</Ref>”) — this also ends the sign-ins on all other devices.
                </p>
            </Topic>
        </Chapter>
    );
}
