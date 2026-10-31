import { msg } from "@lingui/core/macro";
import type { MessageDescriptor } from "@lingui/core";
import type { DocumentKind, DocumentStatus, NewsCategory, SignaturePackaging, SignerStatus, SigningMode } from "@/shared/api/schema";

/**
 * The names of document kinds (the context separates "Довідка" = Certificate from "Довідка" = Help).
 */
export const documentKindNames: Record<DocumentKind, MessageDescriptor> = {
    Order: msg({ message: "Наказ", context: "document-kind" }),
    Directive: msg({ message: "Розпорядження", context: "document-kind" }),
    Memo: msg({ message: "Службова записка", context: "document-kind" }),
    Application: msg({ message: "Заява", context: "document-kind" }),
    Report: msg({ message: "Звіт", context: "document-kind" }),
    Minutes: msg({ message: "Протокол", context: "document-kind" }),
    Contract: msg({ message: "Договір", context: "document-kind" }),
    Certificate: msg({ message: "Довідка", context: "document-kind" }),
    Regulation: msg({ message: "Положення / інструкція", context: "document-kind" }),
    Letter: msg({ message: "Лист", context: "document-kind" }),
    Act: msg({ message: "Акт", context: "document-kind" }),
    Other: msg({ message: "Інший документ", context: "document-kind" }),
};

/**
 * The names of document states.
 */
export const documentStatusNames: Record<DocumentStatus, MessageDescriptor> = {
    Draft: msg`Чернетка`,
    InSigning: msg`На підписанні`,
    Signed: msg`Підписано`,
    Rejected: msg`Відхилено`,
    Archived: msg`В архіві`,
};

/**
 * The names of news categories.
 */
export const newsCategoryNames: Record<NewsCategory, MessageDescriptor> = {
    News: msg`Новини`,
    Announcement: msg`Оголошення`,
    Event: msg`Подія`,
    Training: msg`Навчання та розвиток`,
    Projects: msg`Проєкти та досягнення`,
};

/**
 * The states of a signer on a route.
 */
export const signerStatusNames: Record<SignerStatus, MessageDescriptor> = {
    Pending: msg`Очікується підпис`,
    Signed: msg`Підписав(-ла)`,
    Rejected: msg`Відмовив(-ла)`,
};

/**
 * The orders of signing.
 */
export const signingModeNames: Record<SigningMode, MessageDescriptor> = {
    Sequential: msg`Послідовно`,
    Parallel: msg`Паралельно`,
};

/**
 * The packagings of a signature.
 */
export const packagingNames: Record<SignaturePackaging, MessageDescriptor> = {
    Detached: msg`окремо від даних (зовнішній)`,
    Enveloped: msg`з даними (внутрішній)`,
};

/**
 * The names of signature formats (not translated).
 */
export const formatNames = { CAdES: "CAdES", XAdES: "XAdES", PAdES: "PAdES", AsicS: "ASiC-S", AsicE: "ASiC-E" } as const;
