using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Infrastructure.Persistence;
using ODC.Web.Messaging;
using ODC.Web.Shared.Access;
using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Email;
using ODC.Web.Shared.Localization;
using ODC.Web.Shared.Text;

namespace ODC.Web.Features.Tickets;

/// <summary>
/// E-mails about requests: everyone who handles requests learns about a new one by e-mail, in their own language (a
/// reaction to <see cref="TicketSubmitted"/>, so submitting a request never waits for it).
/// </summary>
/// <param name="db">The master database (requests, accounts).</param>
/// <param name="organization">The organization's database (who handles requests).</param>
/// <param name="catalog">The shared catalogs (roles).</param>
/// <param name="directory">The positions of people.</param>
/// <param name="outbox">Sends the e-mails in the background.</param>
/// <param name="options">The e-mail settings (the address of the cabinet).</param>
internal sealed class TicketMail(MasterDbContext db, OrganizationDbContext organization, Catalog catalog, PositionDirectory directory, IEmailOutbox outbox, IOptions<EmailOptions> options) : IDomainEventHandler<TicketSubmitted>
{
    /// <summary>
    /// The length of the beginning of the description quoted in the e-mail.
    /// </summary>
    private const int QuoteLength = 600;

    /// <summary>
    /// Sends every handler of requests except the author an e-mail about a new request.
    /// </summary>
    /// <param name="domainEvent">The new request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the e-mails are queued.</returns>
    public async Task HandleAsync(TicketSubmitted domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        if (!outbox.IsEnabled)
        {
            return;
        }

        var ticket = await db.Tickets.FirstAsync(t => t.Id == domainEvent.TicketId, cancellationToken);

        var handlers = await organization.MemberIdsWithAsync(catalog, Permissions.Tickets.Manage, cancellationToken);
        var recipients = await db.Users
            .Where(u => handlers.Contains(u.Id) && u.Id != ticket.AuthorId && u.Email != null)
            .Select(static u => new { Email = u.Email!, Name = u.LastName + " " + u.FirstName, u.Language })
            .ToListAsync(cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var author = await db.Users
            .Where(u => u.Id == ticket.AuthorId)
            .Select(static u => new
            {
                Name = u.LastName + " " + u.FirstName + (u.MiddleName != null ? " " + u.MiddleName : string.Empty),
            })
            .FirstAsync(cancellationToken);
        var position = (await directory.PrimaryAsync(ticket.AuthorId, cancellationToken))?.InUnit;
        var url = $"{options.Value.SiteUrl.TrimEnd('/')}/requests/{ticket.Id}";
        var description = domainEvent.Description;
        var quote = description.Length > QuoteLength ? description[..QuoteLength].TrimEnd() + "…" : description;

        foreach (var recipient in recipients)
        {
            outbox.Enqueue(EmailLayout.InLanguage(recipient.Language, () => EmailLayout.Render(recipient.Email, recipient.Name, new EmailContent(
                Texts.T("Нове звернення №{0}: {1}", ticket.Number, ticket.Subject),
                Texts.T("Нове звернення"),
                ticket.Subject,
                Texts.T("{0} створює звернення. Відкрийте його в кабінеті, щоб відповісти або взяти в роботу.", author.Name),
                [
                    new(Texts.T("Номер"), $"№{ticket.Number}"),
                    new(Texts.T("Категорія"), CategoryName(ticket.Category)),
                    new(Texts.T("Пріоритет"), PriorityName(ticket.Priority)),
                    new(Texts.T("Автор"), position is not null ? $"{author.Name}, {position}" : author.Name),
                    new(Texts.T("Подано"), TimeZoneInfo.ConvertTime(ticket.CreatedAt, KyivTime.Zone).ToString("d MMMM yyyy, HH:mm", System.Globalization.CultureInfo.CurrentCulture)),
                ],
                quote,
                Texts.T("Відкрити звернення"),
                url,
                Texts.T("Ви отримали цей лист, бо опрацьовуєте звернення в КОД. Лист надіслано автоматично, відповідати на нього не потрібно.")))));
        }
    }

    /// <summary>
    /// Names a category of requests in the current language.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <returns>The name.</returns>
    private static string CategoryName(TicketCategory category) => category switch
    {
        TicketCategory.Bug => Texts.In("ticket-category", "Помилка"),
        TicketCategory.Suggestion => Texts.T("Пропозиція"),
        TicketCategory.Question => Texts.T("Запитання"),
        TicketCategory.Access => Texts.T("Доступ і права"),
        _ => Texts.In("ticket-category", "Інше"),
    };

    /// <summary>
    /// Names a priority of requests in the current language.
    /// </summary>
    /// <param name="priority">The priority.</param>
    /// <returns>The name.</returns>
    private static string PriorityName(TicketPriority priority) => priority switch
    {
        TicketPriority.Low => Texts.T("Низький"),
        TicketPriority.High => Texts.T("Високий"),
        TicketPriority.Critical => Texts.T("Критичний"),
        _ => Texts.T("Звичайний"),
    };
}
