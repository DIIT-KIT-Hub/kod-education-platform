import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { Check, X } from "lucide-react";
import { useState } from "react";
import type { UserLookupItem } from "@/shared/api/schema";
import { Avatar } from "@/shared/ui/Avatar";
import { Icon } from "@/shared/ui/Icon";
import { SearchInput } from "@/shared/ui/SearchInput";
import { peopleQuery } from "@/features/chats/api";
import { shortName } from "@/features/chats/display";
import { SkeletonList } from "@/shared/ui/Skeleton";
import styles from "./chats.module.css";

/**
 * Props of {@link PeoplePicker}.
 */
export interface PeoplePickerProps {
    /** The chosen people. */
    readonly selected: readonly UserLookupItem[];
    /** Called with the new choice. */
    readonly onChange: (selected: readonly UserLookupItem[]) => void;
    /** People who cannot be chosen (already in the chat). */
    readonly exclude?: readonly string[];
    /** Chooses one person at a time (the choice replaces the previous one). */
    readonly single?: boolean;
}

/**
 * Finds people by name or e-mail and collects a choice of them (shown as chips above the search).
 * @param props The props.
 * @returns The picker.
 */
export function PeoplePicker({ selected, onChange, exclude = [], single }: PeoplePickerProps) {
    const { t } = useLingui();
    const [term, setTerm] = useState("");
    const { data, isFetching } = useQuery(peopleQuery(term));
    const chosen = new Set(selected.map((p) => p.id));
    const results = (data ?? []).filter((person) => !exclude.includes(person.id));

    /**
     * Adds or removes a person.
     * @param person The person.
     */
    function toggle(person: UserLookupItem): void {
        if (chosen.has(person.id)) {
            onChange(selected.filter((p) => p.id !== person.id));
        } else {
            onChange(single ? [person] : [...selected, person]);
        }
    }

    return (
        <div className={styles.peoplePicker}>
            {selected.length > 0 && (
                <ul className={styles.chips} aria-label={t`Обрані люди`}>
                    {selected.map((person) => (
                        <li key={person.id} className={styles.chip}>
                            <Avatar name={person.fullName} photoId={person.photoId} size="sm" decorative />
                            <span>{shortName(person.fullName)}</span>
                            <button
                                type="button"
                                className={styles.chipRemove}
                                aria-label={t`Прибрати ${person.fullName}`}
                                onClick={() => toggle(person)}
                            >
                                <Icon icon={X} size="sm" />
                            </button>
                        </li>
                    ))}
                </ul>
            )}

            <SearchInput value={term} onSearch={setTerm} placeholder={t`Ім'я, прізвище або e-mail`} autoFocus />

            {term.length < 2 ? (
                <p className={styles.pickerHint}>{t`Введіть щонайменше два символи.`}</p>
            ) : isFetching && !data ? (
                <SkeletonList rows={4} />
            ) : results.length === 0 ? (
                <p className={styles.pickerHint}>{t`Нікого не знайдено.`}</p>
            ) : (
                <ul className={styles.people}>
                    {results.map((person) => (
                        <li key={person.id}>
                            <button
                                type="button"
                                className={styles.person}
                                aria-pressed={chosen.has(person.id)}
                                onClick={() => toggle(person)}
                            >
                                <Avatar name={person.fullName} photoId={person.photoId} size="sm" decorative />
                                <span className={styles.personText}>
                                    <span className={styles.personName}>{person.fullName}</span>
                                    <span className={styles.personMeta}>{person.position ?? person.email}</span>
                                </span>
                                {chosen.has(person.id) && <Icon icon={Check} size="sm" />}
                            </button>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}
