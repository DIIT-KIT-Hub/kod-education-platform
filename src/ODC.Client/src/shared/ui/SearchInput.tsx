import { Search } from "lucide-react";
import { useEffect, useState } from "react";
import { Icon } from "./Icon";
import styles from "./SearchInput.module.css";

/**
 * Props of {@link SearchInput}.
 */
export interface SearchInputProps {
    /** The current search (from the address). */
    readonly value: string;
    /** Called with the trimmed text a moment after the user stops typing, and at once on Enter. */
    readonly onSearch: (value: string) => void;
    /** The placeholder, also the accessible name. */
    readonly placeholder: string;
    /** The input id (a label may point to it). */
    readonly id?: string;
    /** Focuses the field when it appears. */
    readonly autoFocus?: boolean;
}

/**
 * A rounded search field with a magnifier that reports the text after a short pause.
 * @param props The props.
 * @returns The field.
 */
export function SearchInput({ value, onSearch, placeholder, id, autoFocus }: SearchInputProps) {
    const [text, setText] = useState(value);
    const [shown, setShown] = useState(value);

    // The address changed elsewhere (a tab, the back button): show its search.
    if (value !== shown) {
        setShown(value);
        setText(value);
    }

    useEffect(() => {
        if (text.trim() === value) {
            return;
        }

        const timer = setTimeout(() => onSearch(text.trim()), 350);
        return () => clearTimeout(timer);
    }, [text, value, onSearch]);

    return (
        <div className={styles.search}>
            <Icon icon={Search} className={styles.icon} />
            <input
                id={id}
                type="search"
                className={styles.input}
                value={text}
                placeholder={placeholder}
                aria-label={placeholder}
                autoComplete="off"
                autoFocus={autoFocus}
                onChange={(event) => setText(event.target.value)}
                onKeyDown={(event) => {
                    if (event.key === "Enter") {
                        event.preventDefault();
                        onSearch(text.trim());
                    }
                }}
            />
        </div>
    );
}
