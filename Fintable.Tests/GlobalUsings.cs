global using Xunit;

using System.Runtime.CompilerServices;
using QuestPDF.Infrastructure;

namespace Fintable.Tests;

internal static class TestAssemblyInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
}
