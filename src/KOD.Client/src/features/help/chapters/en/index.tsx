import { GettingStartedChapter } from "./GettingStartedChapter";
import { AccountChapter } from "./AccountChapter";
import { HomeChapter } from "./HomeChapter";
import { DocumentsChapter } from "./DocumentsChapter";
import { SigningChapter } from "./SigningChapter";
import { NewsChapter } from "./NewsChapter";
import { StructureChapter } from "./StructureChapter";
import { ProfileChapter } from "./ProfileChapter";
import { AdministrationChapter } from "./AdministrationChapter";
import { RolesChapter } from "./RolesChapter";
import { FaqChapter } from "./FaqChapter";

/**
 * The whole manual in English, chapters in the order of the table of contents.
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
            <StructureChapter />
            <ProfileChapter />
            <AdministrationChapter />
            <RolesChapter />
            <FaqChapter />
        </>
    );
}
