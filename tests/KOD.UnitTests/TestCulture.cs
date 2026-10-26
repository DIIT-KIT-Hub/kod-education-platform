using System.Globalization;
using System.Runtime.CompilerServices;

namespace KOD.UnitTests;

/// <summary>
/// Runs the tests in Ukrainian, the source language of the interface texts, whatever the machine language is.
/// </summary>
internal static class TestCulture
{
    /// <summary>
    /// Sets the default culture of the test threads.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        var ukrainian = CultureInfo.GetCultureInfo("uk-UA");
        CultureInfo.DefaultThreadCurrentCulture = ukrainian;
        CultureInfo.DefaultThreadCurrentUICulture = ukrainian;
        CultureInfo.CurrentCulture = ukrainian;
        CultureInfo.CurrentUICulture = ukrainian;
    }
}
