using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using RxBlazorV2.GeneratorTests.Helpers;
using RxBlazorV2Generator;
using RxBlazorV2Generator.Diagnostics;
using Model = RxBlazorV2.Model;

namespace RxBlazorV2.GeneratorTests.GeneratorTests;

public class GeneratorTests
{
    [Fact]
    public async Task ConstructorParameterOrder_PreservesUserDeclaredOrder()
    {
        // This test verifies that constructor parameters are generated in the order
        // declared by the user, NOT alphabetically sorted.
        //
        // User declares: ZetaModel, IAlphaService, BetaModel
        // Without the fix, generator would produce: BetaModel, IAlphaService, ZetaModel (alphabetical)
        // With the fix, generator preserves: ZetaModel, IAlphaService, BetaModel

        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            // Service interface (not an ObservableModel)
            public interface IAlphaService
            {
                void DoSomething();
            }

            // Referenced ObservableModel - first in alphabet but third in declaration
            public partial class BetaModel : ObservableModel
            {
                public partial int Value { get; set; }
            }

            // Referenced ObservableModel - last in alphabet but first in declaration
            public partial class ZetaModel : ObservableModel
            {
                public partial string Name { get; set; }
            }

            // Main model with constructor parameters in specific non-alphabetical order
            public partial class MainModel : ObservableModel
            {
                // Declared order: ZetaModel (Z), IAlphaService (I/A), BetaModel (B)
                // If sorted alphabetically would be: BetaModel, IAlphaService, ZetaModel
                public partial MainModel(ZetaModel zeta, IAlphaService alpha, BetaModel beta);

                public partial int Counter { get; set; }

                private void UseModels()
                {
                    // Use properties from referenced models to avoid unused reference warnings
                    var name = Zeta.Name;
                    var value = Beta.Value;
                }
            }
        }
        """;

        // Generated BetaModel
        // lang=csharp
        const string generatedBetaModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class BetaModel
        {
            public override string ModelID => "Test.BetaModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public partial int Value
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Value");
                    }
                }
            }

        }

        """;

        // Generated ZetaModel
        // lang=csharp
        const string generatedZetaModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class ZetaModel
        {
            public override string ModelID => "Test.ZetaModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public partial string Name
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Name");
                    }
                }
            }

        }

        """;

        // Generated MainModel - the key is that the constructor parameters are in the DECLARED order:
        // (Test.ZetaModel zeta, Test.IAlphaService alpha, Test.BetaModel beta)
        // NOT alphabetically: (Test.BetaModel beta, Test.IAlphaService alpha, Test.ZetaModel zeta)
        // lang=csharp
        const string generatedMainModel = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;
        using System.Linq;

        namespace Test;

        public partial class MainModel
        {
            public override string ModelID => "Test.MainModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                var usedProps = new[] { "Model.Zeta.Name", "Model.Beta.Value" };

                return propertyNames.Intersect(usedProps).Any();
            }

            public Test.ZetaModel Zeta { get; }
            public Test.BetaModel Beta { get; }

            public Test.IAlphaService Alpha { get; }

            public partial int Counter
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Counter");
                    }
                }
            }


            public partial MainModel(Test.ZetaModel zeta, Test.IAlphaService alpha, Test.BetaModel beta) : base()
            {
                Zeta = zeta;
                Beta = beta;
                Alpha = alpha;

                // Subscribe to referenced model changes
                // Transform referenced model property names: Model.X -> Model.{RefName}.X
                // Filtering happens at component level via Filter() method
                Subscriptions.Add(Zeta.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Zeta.")).ToArray())
                    .Subscribe(props => StateHasChanged(props)));
                Subscriptions.Add(Beta.Observable
                    .Select(props => props.Select(p => p.Replace("Model.", "Model.Beta.")).ToArray())
                    .Subscribe(props => StateHasChanged(props)));

                // Subscribe to internal model observers (auto-detected)
                Subscriptions.Add(Zeta.Observable.Where(p => p.Intersect(["Model.Name"]).Any())
                    .Subscribe(_ => UseModels()));
                Subscriptions.Add(Beta.Observable.Where(p => p.Intersect(["Model.Value"]).Any())
                    .Subscribe(_ => UseModels()));
            }
            private bool _contextReadyInternCalled;

            protected override void OnContextReadyIntern()
            {
                if (_contextReadyInternCalled)
                {
                    return;
                }
                _contextReadyInternCalled = true;

                // Initialize referenced ObservableModel dependencies
                Zeta.ContextReady();
                Beta.ContextReady();

            }

            private bool _contextReadyInternAsyncCalled;

            protected override async Task OnContextReadyInternAsync()
            {
                if (_contextReadyInternAsyncCalled)
                {
                    return;
                }
                _contextReadyInternAsyncCalled = true;

                // Initialize referenced ObservableModel dependencies (async)
                await Zeta.ContextReadyAsync();
                await Beta.ContextReadyAsync();
            }

        }

        """;

        // Use inline test setup to avoid verifier class issues
        var testRunner = new RxBlazorGeneratorTest { TestCode = test };
        testRunner.TestState.Sources.Add(("GlobalUsings.cs", SourceText.From(TestShared.GlobalUsing, Encoding.UTF8)));

        // Add each generated source file
        testRunner.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "Test.BetaModel.g.cs",
            SourceText.From(generatedBetaModel.TrimStart().Replace("\r\n", Environment.NewLine), Encoding.UTF8)));
        testRunner.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "Test.ZetaModel.g.cs",
            SourceText.From(generatedZetaModel.TrimStart().Replace("\r\n", Environment.NewLine), Encoding.UTF8)));
        testRunner.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "Test.MainModel.g.cs",
            SourceText.From(generatedMainModel.TrimStart().Replace("\r\n", Environment.NewLine), Encoding.UTF8)));

        // Add service extension files
        string[] modelNames = ["BetaModel", "ZetaModel", "MainModel"];
        testRunner.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "ObservableModelsServiceCollectionExtension.g.cs",
            SourceText.From(RxBlazorGeneratorTest.ObservableModelsServiceExtension(modelNames, ""), Encoding.UTF8)));
        testRunner.TestState.GeneratedSources.Add((typeof(RxBlazorGenerator), "GenericModelsServiceCollectionExtension.g.cs",
            SourceText.From(RxBlazorGeneratorTest.GenricModelsServiceExtension(modelNames, ""), Encoding.UTF8)));

        // Expect diagnostic for unregistered service
        var expectedDiagnostic = new DiagnosticResult(DiagnosticDescriptors.UnregisteredServiceWarning)
            .WithSpan(30, 50, 30, 63)
            .WithArguments("alpha", "IAlphaService", "'services.AddScoped<IAlphaService, YourImplementation>()'");
        testRunner.ExpectedDiagnostics.Add(expectedDiagnostic);

        testRunner.TestState.ReferenceAssemblies = TestShared.ReferenceAssemblies();
        testRunner.TestState.AdditionalReferences.Add(typeof(Model.ObservableModel).Assembly);

        await testRunner.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GeneratorTest_Simple()
    {
        // lang=csharp
        const string test = """

        using RxBlazorV2.Model;
        using RxBlazorV2.Interface;

        namespace Test
        {
            public partial class TestModel : ObservableModel
            {
                public partial int Test { get; set; }
            }
        }
        """;
        
        // lang=csharp
        const string generated = """

        #nullable enable
        using JetBrains.Annotations;
        using Microsoft.Extensions.DependencyInjection;
        using ObservableCollections;
        using R3;
        using RxBlazorV2.Interface;
        using RxBlazorV2.Model;
        using System;

        namespace Test;

        public partial class TestModel
        {
            public override string ModelID => "Test.TestModel";

            public override bool FilterUsedProperties(params string[] propertyNames)
            {
                if (propertyNames.Length == 0)
                {
                    return false;
                }

                // No filtering information available - pass through all
                return true;
            }

            public partial int Test
            {
                get => field;
                [UsedImplicitly]
                set
                {
                    if (field != value)
                    {
                        field = value;
                        StateHasChanged("Model.Test");
                    }
                }
            }

        }

        """;
        
        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, generated, "TestModel", string.Empty);
    }
}