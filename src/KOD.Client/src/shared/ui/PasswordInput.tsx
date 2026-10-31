import { useLingui } from "@lingui/react/macro";
import { Eye, EyeOff } from "lucide-react";
import { useState } from "react";
import { inkHandlers } from "@/shared/lib/ink";
import { Icon } from "./Icon";
import styles from "./PasswordInput.module.css";
import { TextInput, type TextInputProps } from "./TextInput";

/**
 * A password input with a show / hide button.
 * @param props The input props.
 * @returns The input.
 */
export function PasswordInput(props: Omit<TextInputProps, "type">) {
    const { t } = useLingui();
    const [visible, setVisible] = useState(false);
    const label = visible ? t`Сховати пароль` : t`Показати пароль`;

    return (
        <div className={styles.password}>
            <TextInput {...props} type={visible ? "text" : "password"} className={styles.input} />
            <button
                type="button"
                {...inkHandlers}
                className={styles.toggle}
                aria-pressed={visible}
                aria-label={label}
                title={label}
                onClick={() => setVisible(!visible)}
            >
                <Icon icon={visible ? EyeOff : Eye} className={visible ? styles.pop : undefined} />
            </button>
        </div>
    );
}
