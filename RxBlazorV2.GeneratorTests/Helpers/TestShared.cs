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
            Package("Microsoft.NETCore.App.Ref"),
            Path.Combine("ref", "net10.0"));

        return net10
            .AddPackages([
                Package("Microsoft.Net.Compilers.Toolset"),
                Package("Microsoft.Extensions.DependencyInjection"),
                Package("Microsoft.AspNetCore.Components"),
                Package("R3"),
                Package("ObservableCollections.R3"),
                Package("JetBrains.Annotations"),
                Package("MudBlazor")
            ]);
    }

    /// <summary>
    /// Resolves a package identity from the centrally managed version in Directory.Packages.props,
    /// so the harness always compiles against the same versions the solution builds against.
    /// </summary>
    private static PackageIdentity Package(string packageId)
    {
        return new PackageIdentity(packageId, PackageVersions.For(packageId));
    }
}