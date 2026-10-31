import { useLingui } from "@lingui/react/macro";
import { Lock, Network, ShieldCheck, type LucideIcon } from "lucide-react";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Card } from "@/shared/ui/Card";
import { DescriptionList } from "@/shared/ui/DescriptionList";
import { Icon } from "@/shared/ui/Icon";
import { List } from "@/shared/ui/List";
import styles from "./SigningInfo.module.css";

/**
 * A line of the security card.
 * @param props The icon and the text.
 * @returns The line.
 */
function Point({ icon, children }: { readonly icon: LucideIcon; readonly children: string }) {
    return (
        <li className={styles.point}>
            <Icon icon={icon} />
            <span>{children}</span>
        </li>
    );
}

/**
 * What the signing tools support and how they keep the key safe.
 * @returns The cards.
 */
export function SigningInfo() {
    const { t } = useLingui();
    return (
        <div className={[layout.stack, layout.stagger].join(" ")}>
            <Card title={t`Що підтримується`} variant="muted">
                <DescriptionList
                    items={[
                        [t`Ключі`, t`Файлові: Key-6.dat, *.pfx / *.p12, *.pk8, *.jks (ПриватБанк), *.zs2 — усіх кваліфікованих надавачів`],
                        [t`Формати`, t`CAdES (зовнішній і внутрішній), XAdES, PAdES (PDF), ASiC-S, ASiC-E`],
                        [t`Рівні`, t`B-B (базовий), B-T (з позначкою часу), B-LT (з даними перевірки), B-LTA (архівний)`],
                        [t`Надавачі`, t`«Дія», ДПС, ПриватБанк, monobank, Вчасно, DepositSign та інші з переліку ЦЗО`],
                    ]}
                />
            </Card>
            <Card title={t`Безпека`}>
                <List>
                    <Point
                        icon={Lock}
                    >{t`Ключ і пароль обробляються лише у вашому браузері (Web Worker) і ніколи не надсилаються на сервер.`}</Point>
                    <Point icon={ShieldCheck}>{t`Використовується офіційна бібліотека ІІТ «Користувач ЦСК» (EUSignCP-JS).`}</Point>
                    <Point
                        icon={Network}
                    >{t`До серверів надавачів передаються лише публічні запити: перевірка статусу сертифіката та позначка часу.`}</Point>
                </List>
            </Card>
            <Alert tone="outline" title={t`Апаратні та хмарні ключі`}>
                {t`Поточна версія бібліотеки підтримує лише файлові ключі. Токени (Алмаз-1К, Кристал-1), хмарні ключі та «Дія.Підпис» потребують окремих компонентів надавачів — підключіть їх, коли вони знадобляться вашій організації.`}
            </Alert>
        </div>
    );
}
