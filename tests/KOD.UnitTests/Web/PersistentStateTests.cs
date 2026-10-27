using System.Collections.Frozen;
using System.Reflection;
using KOD.Web.Components.Pages.Admin;
using Microsoft.AspNetCore.Components;

namespace KOD.UnitTests.Web;

public sealed class PersistentStateTests
{
    /// <summary>
    /// Collection types System.Text.Json writes but cannot create when the circuit restores the prerendered state.
    /// </summary>
    private static readonly Type[] NotRestorable = [typeof(IReadOnlySet<>), typeof(FrozenSet<>), typeof(FrozenDictionary<,>)];

    [Fact]
    public void Persisted_component_state_has_only_collections_json_can_restore()
    {
        var persisted = typeof(UserEditor).Assembly.GetTypes()
            .SelectMany(static t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(static p => p.IsDefined(typeof(PersistentStateAttribute)))
            .ToList();
        Assert.NotEmpty(persisted);

        var problems = new List<string>();
        foreach (var property in persisted)
        {
            Walk(property.PropertyType, $"{property.DeclaringType!.Name}.{property.Name}", [], problems);
        }

        Assert.Empty(problems);
    }

    /// <summary>
    /// Checks a type and the types of its public properties and generic arguments.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="path">Where the type is used (for the failure message).</param>
    /// <param name="seen">The types already checked.</param>
    /// <param name="problems">The collected problems.</param>
    private static void Walk(Type type, string path, HashSet<Type> seen, List<string> problems)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (!seen.Add(type) || type.IsPrimitive || type.IsEnum || type == typeof(string))
        {
            return;
        }

        if (type.IsGenericType && NotRestorable.Contains(type.GetGenericTypeDefinition()))
        {
            problems.Add($"{path}: {type.Name}");
            return;
        }

        foreach (var argument in type.GetGenericArguments())
        {
            Walk(argument, path, seen, problems);
        }

        if (type.IsArray)
        {
            Walk(type.GetElementType()!, path, seen, problems);
        }

        if (type.Namespace?.StartsWith("KOD.", StringComparison.Ordinal) == true)
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                Walk(property.PropertyType, $"{path}.{property.Name}", seen, problems);
            }
        }
    }
}
