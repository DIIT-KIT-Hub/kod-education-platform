import { zodResolver } from "@hookform/resolvers/zod";
import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { Bug, CircleHelp, KeyRound, Lightbulb, MoreHorizontal, Send, type LucideIcon } from "lucide-react";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { useNavigate } from "react-router";
import { z } from "zod";
import { describeError } from "@/shared/api/problem";
import type { TicketCategory, TicketPriority } from "@/shared/api/schema";
import { ticketCategoryNames, ticketPriorityNames } from "@/shared/i18n/display";
import layout from "@/shared/styles/layout.module.css";
import { Button } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { Field } from "@/shared/ui/Field";
import { Icon } from "@/shared/ui/Icon";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Select } from "@/shared/ui/Select";
import { Textarea } from "@/shared/ui/Textarea";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { ticketApi, ticketKeys } from "@/features/tickets/api";
import { FilePicker } from "@/features/tickets/components/FilePicker";
import styles from "@/features/tickets/components/tickets.module.css";

/**
 * The icons of the categories.
 */
const categoryIcons: Record<TicketCategory, LucideIcon> = {
    Bug: Bug,
    Suggestion: Lightbulb,
    Question: CircleHelp,
    Access: KeyRound,
    Other: MoreHorizontal,
};

/**
 * A new request: what it is about, how urgent, the subject, the description and files.
 * @returns The page.
 */
export function NewTicketPage() {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [files, setFiles] = useState<readonly File[]>([]);

    const schema = z.object({
        category: z.enum(["Bug", "Suggestion", "Question", "Access", "Other"]),
        priority: z.enum(["Low", "Normal", "High", "Critical"]),
        subject: z
            .string()
            .trim()
            .min(1, t`Вкажіть тему.`)
            .max(200, t`Тема задовга.`),
        description: z
            .string()
            .trim()
            .min(10, t`Опишіть звернення докладніше.`)
            .max(10_000, t`Опис задовгий.`),
    });

    const { register, control, handleSubmit, formState } = useForm({
        resolver: zodResolver(schema),
        defaultValues: { category: "Bug" as TicketCategory, priority: "Normal" as TicketPriority, subject: "", description: "" },
    });

    /**
     * Sends the request and opens it.
     * @param values The form values.
     */
    async function submit(values: z.infer<typeof schema>): Promise<void> {
        try {
            const { id } = await ticketApi.submit({ ...values, files });
            await queryClient.invalidateQueries({ queryKey: ticketKeys.all });
            toast(t`Звернення надіслано`);
            await navigate(`/requests/${id}`, { replace: true });
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося надіслати звернення. Спробуйте ще раз.`), { tone: "error" });
        }
    }

    return (
        <>
            <PageHeader title={t`Нове звернення`} breadcrumbs={[{ text: t`Звернення`, to: "/requests" }, { text: t`Нове` }]} />

            <form className={layout.split} noValidate onSubmit={handleSubmit(submit)}>
                <div className={layout.stackLg}>
                    <Card title={t`Що сталося?`}>
                        <Controller
                            control={control}
                            name="category"
                            render={({ field }) => (
                                <div className={styles.categories} role="radiogroup" aria-label={t`Категорія`}>
                                    {(Object.keys(ticketCategoryNames) as TicketCategory[]).map((category) => (
                                        <label key={category} className={styles.category}>
                                            <input
                                                type="radio"
                                                name={field.name}
                                                value={category}
                                                checked={field.value === category}
                                                onChange={() => field.onChange(category)}
                                            />
                                            <Icon icon={categoryIcons[category]} />
                                            <span>{t(ticketCategoryNames[category])}</span>
                                        </label>
                                    ))}
                                </div>
                            )}
                        />
                    </Card>
                    <Card>
                        <div className={styles.formGrid}>
                            <Field
                                label={t({ message: "Тема", context: "ticket" })}
                                required
                                error={formState.errors.subject?.message}
                                className={styles.full}
                            >
                                {(ids) => (
                                    <TextInput
                                        {...ids}
                                        {...register("subject")}
                                        maxLength={200}
                                        placeholder={t`Наприклад: не завантажується файл документа`}
                                    />
                                )}
                            </Field>
                            <Field
                                label={t`Опис`}
                                required
                                hint={t`Що ви робили, що очікували побачити і що сталося натомість. Для помилки — кроки, щоб її повторити.`}
                                error={formState.errors.description?.message}
                                className={styles.full}
                            >
                                {(ids) => <Textarea {...ids} {...register("description")} rows={8} maxLength={10_000} />}
                            </Field>
                            <div className={styles.full}>
                                <span className={styles.label}>{t`Вкладення`}</span>
                                <FilePicker files={files} onChange={setFiles} disabled={formState.isSubmitting} />
                            </div>
                        </div>
                    </Card>
                </div>

                <div className={layout.stackLg}>
                    <Card variant="muted">
                        <div className={layout.stack}>
                            <Field label={t`Пріоритет`}>
                                {(ids) => (
                                    <Controller
                                        control={control}
                                        name="priority"
                                        render={({ field }) => (
                                            <Select
                                                {...ids}
                                                value={field.value}
                                                onChange={field.onChange}
                                                options={(Object.keys(ticketPriorityNames) as TicketPriority[]).map((priority) => ({
                                                    value: priority,
                                                    label: t(ticketPriorityNames[priority]),
                                                }))}
                                            />
                                        )}
                                    />
                                )}
                            </Field>
                            <Button type="submit" block icon={Send} busy={formState.isSubmitting}>
                                {t`Надіслати звернення`}
                            </Button>
                            <span className={[layout.textSm, layout.muted].join(" ")}>
                                {t`Звернення побачать ті, хто опрацьовує звернення. Про відповідь ви дізнаєтеся зі сповіщення.`}
                            </span>
                        </div>
                    </Card>
                </div>
            </form>
        </>
    );
}
