namespace KOD.Application.Common.Access;

/// <summary>
/// In-memory helpers for the organization unit hierarchy.
/// </summary>
public static class OrganizationTree
{
    /// <summary>
    /// Collects the given units and all their descendants.
    /// </summary>
    /// <param name="links">All (unit, parent) pairs.</param>
    /// <param name="roots">The starting units.</param>
    /// <returns>The roots and their descendants.</returns>
    public static HashSet<Guid> WithDescendants(IEnumerable<(Guid Id, Guid? ParentId)> links, IEnumerable<Guid> roots)
    {
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(roots);

        var children = links
            .Where(static l => l.ParentId is not null)
            .ToLookup(static l => l.ParentId!.Value, static l => l.Id);

        var result = new HashSet<Guid>();
        var queue = new Queue<Guid>(roots);
        while (queue.TryDequeue(out var id))
        {
            if (!result.Add(id))
            {
                continue;
            }

            foreach (var child in children[id])
            {
                queue.Enqueue(child);
            }
        }

        return result;
    }

    /// <summary>
    /// Builds the path from the root to the given unit.
    /// </summary>
    /// <param name="parents">The parent of every unit.</param>
    /// <param name="unitId">The unit.</param>
    /// <returns>Unit identifiers from the root to <paramref name="unitId"/>.</returns>
    public static IReadOnlyList<Guid> PathTo(IReadOnlyDictionary<Guid, Guid?> parents, Guid unitId)
    {
        ArgumentNullException.ThrowIfNull(parents);

        var path = new List<Guid>();
        Guid? current = unitId;
        while (current is { } id && !path.Contains(id))
        {
            path.Add(id);
            current = parents.GetValueOrDefault(id);
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// Finds the organization a unit belongs to: the nearest ancestor (or the unit itself) whose type is an organization,
    /// or the top-level unit when no unit on the path is marked as an organization.
    /// </summary>
    /// <param name="parents">The parent of every unit.</param>
    /// <param name="organizations">The units whose type is an organization.</param>
    /// <param name="unitId">The unit.</param>
    /// <returns>The organization identifier.</returns>
    public static Guid OrganizationOf(IReadOnlyDictionary<Guid, Guid?> parents, IReadOnlySet<Guid> organizations, Guid unitId)
    {
        ArgumentNullException.ThrowIfNull(organizations);

        var path = PathTo(parents, unitId);
        for (var i = path.Count - 1; i >= 0; i--)
        {
            if (organizations.Contains(path[i]))
            {
                return path[i];
            }
        }

        return path[0];
    }
}
