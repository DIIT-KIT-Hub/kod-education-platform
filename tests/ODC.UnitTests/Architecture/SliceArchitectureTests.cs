using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using ODC.Web.Endpoints;
using ODC.Web.Features;
using ODC.Web.Messaging;

namespace ODC.UnitTests.Architecture;

/// <summary>
/// The rules of the vertical slices, checked on the sources and the compiled assembly (our own tests, no library):
/// features meet only along known edges, nobody injects another feature's internals, handlers and endpoints stay
/// internal and registered, the domain knows nothing of persistence or HTTP.
/// </summary>
public sealed partial class SliceArchitectureTests
{
    /// <summary>
    /// The namespaces a feature may use from other features. A new edge is a design decision: prefer a domain event or a
    /// shared type, and add the edge here only when the dependency is meant.
    /// </summary>
    private static readonly Dictionary<string, string[]> AllowedEdges = new()
    {
        // Lists of units for the user filter and the organizations of news (a public query of Structure).
        ["Admin"] = ["Structure.Units"],
        ["News"] = ["Structure.Units"],

        // The home page shows documents and news through their public queries.
        ["Home"] = ["Documents", "Documents.Domain", "News"],

        // Notifications react to the events of the other features (never the other way round).
        ["Notifications"] = ["Chats.Domain", "Documents.Domain", "News.Domain", "Tickets.Domain"],

        // The recovery codes answer is the contract of the sign-in.
        ["Profile"] = ["Auth"],

        // The file signing tool records the formats of signatures.
        ["Signing"] = ["Documents.Domain"],
    };

    /// <summary>
    /// The assembly of the application.
    /// </summary>
    private static readonly Assembly App = typeof(IEndpoint).Assembly;

    [Fact]
    public void Features_use_each_other_only_along_the_known_edges()
    {
        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(FeaturesFolder(), "*.cs", SearchOption.AllDirectories))
        {
            var parts = Path.GetRelativePath(FeaturesFolder(), file).Split(Path.DirectorySeparatorChar);
            if (parts.Length < 2)
            {
                continue;
            }

            var feature = parts[0];
            var allowed = AllowedEdges.GetValueOrDefault(feature, []);
            foreach (Match match in FeatureReference().Matches(File.ReadAllText(file)))
            {
                var target = NamespaceOf(match.Groups["name"].Value);
                if (target.Length > 0 && target.Split('.')[0] != feature && !allowed.Contains(target))
                {
                    violations.Add($"{Path.GetRelativePath(FeaturesFolder(), file)} uses ODC.Web.Features.{target}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations.Distinct()));
    }

    [Fact]
    public void No_type_takes_the_internal_types_of_another_feature()
    {
        var violations =
            from type in App.GetTypes()
            let feature = FeatureOf(type)
            where feature is not null
            from constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            from parameter in constructor.GetParameters()
            let dependency = parameter.ParameterType
            let other = FeatureOf(dependency)
            where other is not null && other != feature && !dependency.IsVisible
            select $"{type.FullName} takes {dependency.FullName}";

        Assert.Empty(violations.Distinct());
    }

    [Fact]
    public void Handlers_endpoints_and_reactions_are_internal_and_sealed()
    {
        var violations =
            from type in App.GetTypes()
            where FeatureOf(type) is not null
                  && (typeof(IEndpoint).IsAssignableFrom(type)
                      || type.GetInterfaces().Any(static i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) || i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))))
                  && (type.IsVisible || !type.IsSealed)
            select type.FullName;

        Assert.Empty(violations);
    }

    [Fact]
    public void Every_request_has_exactly_one_handler_and_it_is_registered_with_the_pipeline()
    {
        var handlers = MessagingServiceCollectionExtensions.FindHandlers(App).ToList();
        var duplicates = handlers.GroupBy(static h => h.Contract).Where(static g => g.Count() > 1).Select(static g => g.Key.ToString());
        Assert.Empty(duplicates);

        var requests = App.GetTypes()
            .Where(static t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces().Any(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .Where(static t => !t.IsNested || t.DeclaringType?.Namespace?.StartsWith("ODC.Web.Features", StringComparison.Ordinal) == true);
        var handled = handlers.Select(static h => h.Contract.GetGenericArguments()[0]).ToHashSet();
        Assert.Empty(requests.Where(r => !handled.Contains(r)).Select(static r => r.FullName));

        var services = new ServiceCollection().AddApplication();
        var registered = services.Where(static d => d.ImplementationFactory is not null).Select(static d => d.ServiceType).ToHashSet();
        Assert.Empty(handlers.Where(h => !registered.Contains(h.Contract)).Select(static h => h.Handler.FullName));
    }

    [Fact]
    public void The_domain_knows_nothing_of_persistence_http_or_the_pipeline()
    {
        string[] forbidden = ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore.Http", "Microsoft.AspNetCore.Mvc", "ODC.Web.Infrastructure", "ODC.Web.Endpoints", "ODC.Web.Messaging", "ODC.Web.Features"];
        var root = Path.Combine(RepositoryRoot(), "src", "ODC.Web");
        var violations =
            from file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            let relative = Path.GetRelativePath(root, file)
            where relative.Split(Path.DirectorySeparatorChar).Contains("Domain") && !relative.StartsWith("bin", StringComparison.Ordinal) && !relative.StartsWith("obj", StringComparison.Ordinal)
            let ownFeature = relative.StartsWith("Features", StringComparison.Ordinal) ? "ODC.Web.Features." + relative.Split(Path.DirectorySeparatorChar)[1] + "." : null
            from line in File.ReadLines(file)
            where line.StartsWith("using ", StringComparison.Ordinal)
            let ns = line["using ".Length..].TrimEnd(';')
            where forbidden.Any(f => ns.StartsWith(f, StringComparison.Ordinal)) && (ownFeature is null || !(ns + ".").StartsWith(ownFeature, StringComparison.Ordinal))
            select $"{relative}: {line}";

        Assert.Empty(violations);
    }

    /// <summary>
    /// Finds the feature of a type: the namespace segment after <c>ODC.Web.Features</c>.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The feature, or <see langword="null"/> outside the features.</returns>
    private static string? FeatureOf(Type type)
    {
        var parts = (type.Namespace ?? string.Empty).Split('.');
        return parts is ["ODC", "Web", "Features", var feature, ..] ? feature : null;
    }

    /// <summary>
    /// Finds the namespace part of a qualified name below <c>ODC.Web.Features</c> (a using directive names a namespace,
    /// a qualified name in code ends with a type and maybe a member).
    /// </summary>
    /// <param name="name">The name after <c>ODC.Web.Features.</c>.</param>
    /// <returns>The namespace without the prefix, or an empty string.</returns>
    private static string NamespaceOf(string name)
    {
        var namespaces = App.GetTypes().Select(static t => t.Namespace).OfType<string>().ToHashSet();
        var parts = name.Split('.');
        for (var length = parts.Length; length > 0; length--)
        {
            var candidate = string.Join('.', parts[..length]);
            if (namespaces.Contains("ODC.Web.Features." + candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Finds the repository root (the folder with the solution file).
    /// </summary>
    /// <returns>The path.</returns>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrganizationalDocumentsCabinet.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }

    /// <summary>
    /// Gets the folder of the features.
    /// </summary>
    /// <returns>The path.</returns>
    private static string FeaturesFolder() => Path.Combine(RepositoryRoot(), "src", "ODC.Web", "Features");

    /// <summary>
    /// A reference to a feature namespace (a using directive or a qualified name).
    /// </summary>
    /// <returns>The expression.</returns>
    [GeneratedRegex(@"\bODC\.Web\.Features\.(?<name>\w+(?:\.\w+)*)")]
    private static partial Regex FeatureReference();
}
