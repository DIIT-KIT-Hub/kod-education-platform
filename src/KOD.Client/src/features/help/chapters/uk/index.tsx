import { GettingStartedChapter } from "./GettingStartedChapter";
import { AccountChapter } from "./AccountChapter";
import { HomeChapter } from "./HomeChapter";
import { DocumentsChapter } from "./DocumentsChapter";
import { SigningChapter } from "./SigningChapter";
import { NewsChapter } from "./NewsChapter";
import { ChatsChapter } from "./ChatsChapter";
import { RequestsChapter } from "./RequestsChapter";
import { NotificationsChapter } from "./NotificationsChapter";
import { StructureChapter } from "./StructureChapter";
import { ProfileChapter } from "./ProfileChapter";
import { AdministrationChapter } from "./AdministrationChapter";
import { RolesChapter } from "./RolesChapter";
import { FaqChapter } from "./FaqChapter";

/**
 * The whole manual in Ukrainian, chapters in the order of the table of contents.
 * @returns The chapters.
 */
export function Manual() {
    return (
        <>
            <GettingStartedChapter />
            <AccountChapter />
            <HomeChapter />
            <DocumentsChapter />
            <SigningChapter />
            <NewsChapter />
            <ChatsChapter />
            <RequestsChapter />
            <NotificationsChapter />
            <StructureChapter />
            <ProfileChapter />
            <AdministrationChapter />
            <RolesChapter />
            <FaqChapter />
        </>
    );
}
