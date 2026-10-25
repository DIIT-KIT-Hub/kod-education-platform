namespace KOD.DesignSystem.Components;

/// <summary>
/// A step of a breadcrumb trail.
/// </summary>
/// <param name="Text">The visible text.</param>
/// <param name="Href">The link; <see langword="null"/> for the current page.</param>
public sealed record BreadcrumbItem(string Text, string? Href = null);
