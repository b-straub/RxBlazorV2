using Microsoft.CodeAnalysis.Testing;

namespace RxBlazorV2.GeneratorTests.Helpers;

internal static class TestShared
{
    public const string GlobalUsing =
        """
        global using global::System;
        global using global::System.Collections.Generic;
        global using global::System.IO;
        global using global::System.Linq;
        global using global::System.Net.Http;
        global using global::System.Threading;
        global using global::System.Threading.Tasks;
        """;

    public static ReferenceAssemblies ReferenceAssemblies()
    {
        var net10 = new ReferenceAssemblies(
            "net10.0",
            new PackageIdentity(
                "Microsoft.NETCore.App.Ref",
                "10.0.0"),
            Path.Combine("ref", "net10.0"));
        
        return net10
            .AddPackages([
                new PackageIdentity("Microsoft.Net.Compilers.Toolset",
                    "5.0.0"), // Use the latest version of the compiler toolset
                new PackageIdentity("Microsoft.Extensions.DependencyInjection", "10.0.0"),
                new PackageIdentity("Microsoft.AspNetCore.Components", "10.0.0"),
                new PackageIdentity("R3", "1.3.0"),
                new PackageIdentity("ObservableCollections.R3", "3.3.4"),
                new PackageIdentity("JetBrains.Annotations", "2025.2.4"),
                new PackageIdentity("MudBlazor", "8.15.0")
            ]);
    }
}