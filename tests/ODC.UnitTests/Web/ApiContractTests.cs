using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ODC.Web.Endpoints;

namespace ODC.UnitTests.Web;

/// <summary>
/// Checks the request contracts of the REST API.
/// </summary>
public sealed class ApiContractTests
{
    /// <summary>
    /// Validation attributes of records belong on the primary constructor parameters (<c>[Required]</c>), never on
    /// their properties (<c>[property: Required]</c>): the request validation reads the parameters, and MVC (still used by
    /// nothing but kept as a rule) rejects the latter with a 500.
    /// </summary>
    [Fact]
    public void Validation_attributes_of_record_contracts_are_on_the_constructor_parameters()
    {
        var offenders = typeof(IEndpoint).Assembly.GetTypes()
            .Where(static type => type.GetMethod("<Clone>$") is not null)
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
