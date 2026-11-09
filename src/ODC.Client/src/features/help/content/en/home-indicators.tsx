import { Table } from "../../components/Manual";

/**
 * The manual page "home-indicators" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>Four cards with counters; each one is a link to the filtered document registry:</p>
            <Table>
                <thead>
                    <tr>
                        <th>Card</th>
                        <th>What it counts</th>
                        <th>Where it leads</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Awaiting my signature</td>
                        <td>Documents where it is currently your turn. If there are more than zero, the card is highlighted in black.</td>
                        <td>The “Awaiting my signature” tab</td>
                    </tr>
                    <tr>
                        <td>My drafts</td>
                        <td>Your documents with the “Draft” status.</td>
                        <td>“Created by me” + the “Draft” status</td>
                    </tr>
                    <tr>
                        <td>My documents in signing</td>
                        <td>Your documents that others are currently signing.</td>
                        <td>“Created by me” + “In signing”</td>
                    </tr>
                    <tr>
                        <td>Signed by me this month</td>
                        <td>Documents you have signed since the beginning of the current month.</td>
                        <td>“Signed by me”</td>
                    </tr>
                </tbody>
            </Table>
        </>
    );
}
