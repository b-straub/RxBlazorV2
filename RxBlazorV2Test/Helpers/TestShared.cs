using Microsoft.CodeAnalysis.Testing;

namespace RxBlazorV2Test.Helpers;

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
        return Microsoft.CodeAnalysis.Testing.ReferenceAssemblies.Net.Net90
            .AddPackages([
                new PackageIdentity("Microsoft.Net.Compilers.Toolset",
                    "4.14.0"), // Use the latest version of the compiler toolset
                new PackageIdentity("Microsoft.Extensions.DependencyInjection", "9.0.6"),
                new PackageIdentity("Microsoft.AspNetCore.Components", "9.0.6"),
                new PackageIdentity("R3", "1.3.0")
            ]);
    }
}