import organizationBarImage from "../../images/en/organization-bar.webp";
import { Figure, Note, Ref, Steps, Table, Ui } from "../../components/Manual";

/**
 * The manual page "intro-organizations" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Every organization of the cabinet has <strong>its own database and its own file storage</strong>: its documents, chats,
                news, structure and notifications are kept apart from other organizations. You have one account — you sign in once and work
                in every organization where you hold a position or where you were invited.
            </p>
            <h2>The current organization and the switcher</h2>
            <p>
                At the top of every page the <Ui>Organization</Ui> bar shows which organization you are working in: its full name, short
                name, code and how you work there. The structure, the document register, the organization's news and the administration are
                about this one.
            </p>
            <Figure
                src={organizationBarImage}
                alt="The bar of the current organization"
                caption="The current organization, the switcher and (for a platform administrator) the way to all organizations."
            />
            <Steps>
                <li>Open the list on the right of the bar (it is there when several organizations are open to you).</li>
                <li>Choose another organization.</li>
                <li>The cabinet opens again in it: the menu, the pages and the permissions are those of that organization.</li>
            </Steps>
            <p>
                The address of another organization's page starts with its code, for example <Ui>/o/FUND/documents</Ui>. You can send such a
                link to a colleague: it opens the page in that very organization. An address without a code opens your main organization.
            </p>
            <h2>How you work in an organization</h2>
            <Table>
                <thead>
                    <tr>
                        <th>In the switcher</th>
                        <th>What it means</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Your organization</td>
                        <td>You hold a position there: the permissions of your positions in that organization apply.</td>
                    </tr>
                    <tr>
                        <td>Guest</td>
                        <td>
                            You were added to the signing route of a document or to a chat of this organization. You see only these
                            documents and chats and the news of the whole cabinet, nothing else of the organization.
                        </td>
                    </tr>
                    <tr>
                        <td>Platform administrator</td>
                        <td>
                            A system administrator may work in any organization. Every action there is recorded both in the organization's
                            log and in the platform's log.
                        </td>
                    </tr>
                </tbody>
            </Table>
            <h2>What is gathered from all organizations</h2>
            <ul>
                <li>
                    <strong>The home page</strong>: documents waiting for your signature in every organization.
                </li>
                <li>
                    <strong>Notifications</strong>: the bell and its counter gather the notifications of all your organizations.
                </li>
                <li>
                    <strong>Chats</strong>: the list shows all your chats, including those with people of other organizations.
                </li>
                <li>
                    <strong>News of the whole cabinet</strong>: visible in every organization.
                </li>
            </ul>
            <p>
                An item of another organization carries its short name. It opens in the context of its organization: the cabinet goes there
                by itself.
            </p>
            <Note title="Colleagues from other organizations">
                You can choose a person of another organization as a signer or a chat member — the search shows their organization instead
                of the position. They work with the document or the chat as a guest (see “<Ref to="documents-route">Signing route</Ref>”, “
                <Ref to="chats-start">Chat list and a new chat</Ref>”).
            </Note>
        </>
    );
}
