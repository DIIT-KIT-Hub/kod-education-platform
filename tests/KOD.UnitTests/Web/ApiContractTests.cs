using System.ComponentModel.DataAnnotations;
using System.Reflection;
using KOD.Web.Api.Contracts;

namespace KOD.UnitTests.Web;

/// <summary>
/// Checks the request contracts of the REST API.
/// </summary>
public sealed class ApiContractTests
{
    /// <summary>
    /// MVC rejects (with an exception, so 500) a record whose validation attributes sit on the properties of its
    /// primary constructor (<c>[property: Required]</c>): they must be on the constructor parameters (<c>[Required]</c>).
    /// </summary>
    [Fact]
    public void Validation_attributes_of_record_contracts_are_on_the_constructor_parameters()
    {
        var offenders = typeof(CommentRequest).Assembly.GetTypes()
            .Where(static type => type.Namespace == typeof(CommentRequest).Namespace && type.GetMethod("<Clone>$") is not null)
            .SelectMany(static type => type.GetConstructors()
                .SelectMany(static constructor => constructor.GetParameters())
                .Select(parameter => type.GetProperty(parameter.Name!, BindingFlags.Public | BindingFlags.Instance))
                .Where(static property => property?.GetCustomAttributes<ValidationAttribute>().Any() == true)
                .Select(property => $"{type.Name}.{property!.Name}"))
            .Distinct()
            .ToList();

        Assert.Empty(offenders);
    }
}
