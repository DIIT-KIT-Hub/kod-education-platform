import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { CircleCheck, Download, FileSpreadsheet, ListChecks, Table, Upload, UserCog, UserPlus } from "lucide-react";
import { useState } from "react";
import { useSearchParams } from "react-router";
import { describeError } from "@/shared/api/problem";
import type { ImportRowError, SpreadsheetFormat, UserImportReport } from "@/shared/api/schema";
import { formatNumber } from "@/shared/i18n/format";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { DataGrid, type GridColumn } from "@/shared/ui/DataGrid";
import { FileDrop } from "@/shared/ui/FileDrop";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Pagination } from "@/shared/ui/Pagination";
import { Progress } from "@/shared/ui/Progress";
import { StatCard } from "@/shared/ui/StatCard";
import { toast } from "@/shared/ui/Toast";
import { adminApi, downloadCredentials, importRolesQuery } from "../api";
import styles from "./admin.module.css";

/** The largest import file (matches FileRules.MaxImportSize). */
const maxImportBytes = 5 * 1024 * 1024;

/** The columns of the import file (the template has the same headers). */
const importColumns = "Email, Прізвище, Ім'я, По батькові, Телефон, Код підрозділу, Код ролі, Посада, Статус";

/** Errors per page of the report. */
const errorPageSize = 10;

/**
 * Bulk creation and update of accounts from XLSX or CSV (checked first, applied only without errors) and the export.
 * @returns The page.
 */
export function UsersImportPage() {
    const { t } = useLingui();
    const user = useCurrentUser();
    const canImport = hasPermission(user, Permissions.usersImport);
    const canExport = hasPermission(user, Permissions.usersExport);
    const roles = useQuery({ ...importRolesQuery, enabled: canImport });
    const [file, setFile] = useState<File | null>(null);
    const [report, setReport] = useState<UserImportReport | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    /**
     * Checks the file (dry run) or applies it.
     * @param dryRun Whether only to check.
     */
    async function run(dryRun: boolean): Promise<void> {
        if (!file) {
            return;
        }

        setBusy(true);
        setError(null);
        try {
            setReport(await adminApi.importUsers(file, dryRun));
        } catch (failure) {
            setReport(null);
            setError(describeError(failure, t`Не вдалося обробити файл. Спробуйте ще раз.`));
        } finally {
            setBusy(false);
        }
    }

    /**
     * Downloads the temporary passwords of the created accounts.
     * @param format The file format.
     */
    async function download(format: SpreadsheetFormat): Promise<void> {
        try {
            await downloadCredentials(report?.credentials ?? [], format);
        } catch {
            toast(t`Не вдалося завантажити файл.`, { danger: true });
        }
    }

    return (
        <>
            <PageHeader
                title={t`Імпорт і експорт користувачів`}
                subtitle={t`Масове створення та оновлення облікових записів із файлів XLSX або CSV.`}
                breadcrumbs={[
                    { text: t`Адміністрування`, to: "/admin" },
                    { text: t`Користувачі`, to: "/admin/users" },
                    { text: t`Імпорт і експорт` },
                ]}
            />

            <div className={layout.split}>
                <div className={layout.stackLg}>
                    {canImport && (
                        <>
                            <Card
                                title={t`1. Оберіть файл`}
                                subtitle={t`Рядки з однаковою електронною поштою — це кілька посад одного користувача.`}
                            >
                                <FileDrop
                                    icon={FileSpreadsheet}
                                    accept=".xlsx,.csv"
                                    disabled={busy}
                                    title={file?.name ?? t`Перетягніть файл XLSX або CSV`}
                                    hint={t`До 5 МБ, до 10 000 рядків. Наявні облікові записи оновлюються за e-mail. Посади — лише в підрозділах організацій, якими ви керуєте.`}
                                    onFiles={(files) => {
                                        const chosen = files[0] ?? null;
                                        setReport(null);
                                        if (chosen && chosen.size > maxImportBytes) {
                                            setFile(null);
                                            setError(t`Файл імпорту не може бути більшим за 5 МБ.`);
                                            return;
                                        }

                                        setError(null);
                                        setFile(chosen);
                                    }}
                                />
                                <div className={layout.cluster}>
                                    <Button icon={ListChecks} disabled={!file} busy={busy} onClick={() => void run(true)}>
                                        {t({ message: "Перевірити", context: "import" })}
                                    </Button>
                                    <Button
                                        variant="secondary"
                                        icon={Upload}
                                        disabled={busy || !report || report.applied || report.errors.length > 0}
                                        onClick={() => void run(false)}
                                    >
                                        {t`Імпортувати`}
                                    </Button>
                                </div>
                            </Card>

                            {busy && <Progress />}
                            {error && <Alert tone="danger">{error}</Alert>}
                            {report && <ImportReport report={report} onDownload={(format) => void download(format)} />}
                        </>
                    )}
                </div>

                <div className={layout.stackLg}>
                    {canImport && (
                        <Card title={t`Шаблон`} variant="muted">
                            <p className={[layout.textSm, layout.muted].join(" ")}>{t`Стовпці: ${importColumns}.`}</p>
                            <div className={layout.cluster}>
                                <ButtonLink
                                    href="/api/v1/user-imports/template?format=Xlsx"
                                    download=""
                                    variant="secondary"
                                    size="sm"
                                    icon={Download}
                                >
                                    XLSX
                                </ButtonLink>
                                <ButtonLink
                                    href="/api/v1/user-imports/template?format=Csv"
                                    download=""
                                    variant="secondary"
                                    size="sm"
                                    icon={Download}
                                >
                                    CSV
                                </ButtonLink>
                            </div>
                            {roles.data && roles.data.length > 0 && (
                                <details className={styles.details}>
                                    <summary>{t`Коди ролей`}</summary>
                                    <dl className={styles.codes}>
                                        {roles.data.map((role) => (
                                            <div key={role.id}>
                                                <dt>{role.code}</dt>
                                                <dd>{role.name}</dd>
                                            </div>
                                        ))}
                                    </dl>
                                </details>
                            )}
                        </Card>
                    )}
                    {canExport && (
                        <Card title={t`Експорт`}>
                            <p className={[layout.textSm, layout.muted].join(" ")}>
                                {t`Вивантажує всіх користувачів у форматі, сумісному з імпортом (без паролів).`}
                            </p>
                            <div className={layout.cluster}>
                                <ButtonLink href="/api/v1/user-exports?format=Xlsx" download="" size="sm" icon={Download}>
                                    XLSX
                                </ButtonLink>
                                <ButtonLink
                                    href="/api/v1/user-exports?format=Csv"
                                    download=""
                                    variant="secondary"
                                    size="sm"
                                    icon={Download}
                                >
                                    CSV
                                </ButtonLink>
                            </div>
                        </Card>
                    )}
                </div>
            </div>
        </>
    );
}

/**
 * Props of {@link ImportReport}.
 */
interface ImportReportProps {
    readonly report: UserImportReport;
    /** Downloads the temporary passwords. */
    readonly onDownload: (format: SpreadsheetFormat) => void;
}

/**
 * The result of a check or an import: counts, errors by row (paged) and the temporary passwords of new accounts.
 * @param props The report.
 * @returns The card.
 */
function ImportReport({ report, onDownload }: ImportReportProps) {
    const { t } = useLingui();
    const [params] = useSearchParams();
    const page = Math.max(1, Number(params.get("errors")) || 1);
    const errors = report.errors.slice((page - 1) * errorPageSize, page * errorPageSize);

    const columns: GridColumn<ImportRowError>[] = [
        { key: "row", title: t`Рядок`, numeric: true, render: (item) => item.row },
        { key: "email", title: "E-mail", primary: true, render: (item) => item.email },
        { key: "message", title: t`Помилка`, render: (item) => item.message },
    ];

    return (
        <Card title={report.applied ? t`2. Імпорт виконано` : t`2. Результат перевірки`}>
            <div className={[layout.colsStats, layout.stagger].join(" ")}>
                <StatCard label={t`Рядків у файлі`} value={formatNumber(report.totalRows)} icon={Table} />
                <StatCard
                    label={report.applied ? t`Створено` : t`Буде створено`}
                    value={formatNumber(report.usersToCreate)}
                    icon={UserPlus}
                    inverse={report.applied}
                />
                <StatCard
                    label={report.applied ? t`Оновлено` : t`Буде оновлено`}
                    value={formatNumber(report.usersToUpdate)}
                    icon={UserCog}
                />
            </div>

            {report.errors.length > 0 ? (
                <>
                    <Alert tone="danger" title={t`Знайдено помилок: ${report.errors.length}`}>
                        {t`Виправте файл і перевірте його ще раз — імпорт можливий лише без помилок.`}
                    </Alert>
                    <DataGrid
                        columns={columns}
                        result={{ items: errors, page, pageSize: errorPageSize, totalCount: report.errors.length }}
                        paged={false}
                        rowKey={(item) => `${item.row}:${item.email}:${item.message}`}
                        caption={t`Помилки імпорту`}
                        emptyIcon={CircleCheck}
                        emptyTitle={t`Помилок не знайдено`}
                    />
                    <Pagination
                        page={page}
                        pageSize={errorPageSize}
                        totalCount={report.errors.length}
                        pageParam="errors"
                        pageSizes={null}
                    />
                </>
            ) : (
                !report.applied && (
                    <Alert tone="outline" icon={CircleCheck}>
                        {t`Помилок не знайдено. Натисніть «Імпортувати», щоб зберегти зміни.`}
                    </Alert>
                )
            )}

            {report.credentials.length > 0 && (
                <div className={styles.secret}>
                    <span className={layout.strong}>
                        {t`Створено облікових записів: ${report.credentials.length}. Завантажте тимчасові паролі — вони показуються лише зараз.`}
                    </span>
                    <div className={layout.cluster}>
                        <Button icon={Download} onClick={() => onDownload("Xlsx")}>
                            {t`Паролі (XLSX)`}
                        </Button>
                        <Button variant="secondary" icon={Download} onClick={() => onDownload("Csv")}>
                            {t`Паролі (CSV)`}
                        </Button>
                    </div>
                </div>
            )}
        </Card>
    );
}
