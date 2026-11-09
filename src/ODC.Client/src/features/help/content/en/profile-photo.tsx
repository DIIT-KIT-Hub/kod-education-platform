import { Steps, Ui } from "../../components/Manual";

/**
 * The manual page "profile-photo" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
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
                    In the round frame, drag the photo with the mouse or a finger (or the arrow keys) and zoom with the slider or the mouse
                    wheel so the face is inside the circle.
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
                The photo is made smaller automatically, and the hidden data of the picture, including the place it was taken, is removed.
                Only signed-in users of the cabinet can see photos.
            </p>
        </>
    );
}
