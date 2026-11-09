using Microsoft.EntityFrameworkCore;
using ODC.Web.Features.Chats.Domain;
using ODC.Web.Features.Documents.Domain;
using ODC.Web.Features.News.Domain;
using ODC.Web.Features.Notifications.Domain;
using ODC.Web.Features.Tickets.Domain;
using ODC.Web.Shared.Domain.Media;
using ODC.Web.Shared.Domain.Reactions;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Infrastructure.Persistence;

/// <summary>
/// The rules both databases share: configurations by placement, enums as readable strings, keys assigned by the domain,
/// and transactions under the execution strategy.
/// </summary>
internal static class ModelRules
{
    /// <summary>
    /// Applies the configurations of the entities placed in a database (<see cref="DataPlacement"/>).
    /// </summary>
    /// <param name="builder">The model builder.</param>
    /// <param name="entities">The entities of the database.</param>
    public static void ApplyConfigurations(ModelBuilder builder, IReadOnlySet<Type> entities)
    {
        builder.HasPostgresExtension("citext");
        builder.ApplyConfigurationsFromAssembly(typeof(ModelRules).Assembly, type => DataPlacement.Configures(type, entities));
    }

    /// <summary>
    /// Marks the <c>Id</c> of the given entities as assigned by the domain (UUID v7): new entities reachable through
    /// navigations must be inserted, not updated.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    /// <param name="alsoAssigned">Entities outside <see cref="Entity"/> whose keys the domain assigns.</param>
    public static void KeysAssignedByDomain(ModelBuilder builder, params Type[] alsoAssigned)
    {
        foreach (var entityType in builder.Model.GetEntityTypes().Where(t => typeof(Entity).IsAssignableFrom(t.ClrType) || alsoAssigned.Contains(t.ClrType)))
        {
            builder.Entity(entityType.ClrType).Property(nameof(Entity.Id)).ValueGeneratedNever();
        }
    }

    /// <summary>
    /// Stores enums as readable strings.
    /// </summary>
    /// <param name="configurationBuilder">The conventions.</param>
    public static void EnumsAsStrings(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        configurationBuilder.Properties<DocumentKind>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<DocumentStatus>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<SigningMode>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<SignerStatus>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<SignatureFormat>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<SignaturePackaging>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<KeyMediaType>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<NewsCategory>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<NewsBodyFormat>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<ReactionKind>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<MediaPurpose>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<TicketCategory>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<TicketPriority>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<TicketStatus>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<TicketMessageKind>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<NotificationKind>().HaveConversion<string>().HaveMaxLength(32);
    }

    /// <summary>
    /// Runs an operation inside a database transaction using the configured execution strategy.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="context">The context.</param>
    /// <param name="operation">The operation to run.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public static Task<T> InTransactionAsync<T>(DbContext context, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(operation);

        var strategy = context.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(
            (Context: context, Operation: operation),
            static async (_, state, ct) =>
            {
                await using var transaction = await state.Context.Database.BeginTransactionAsync(ct);
                var result = await state.Operation(ct);
                await transaction.CommitAsync(ct);
                return result;
            },
            verifySucceeded: null,
            cancellationToken);
    }
}
