using RxBlazorV2Test.Helpers;

namespace RxBlazorV2Test.GeneratorTests;

public class GeneratorTests
{
    [Fact]
    public async Task GeneratorTest_Simple()
    {
        // lang=csharp
        const string test = @"
using RxBlazorV2.Model;
using RxBlazorV2.Interface;

namespace Test
{
    public partial class TestModel : ObservableModel
    {
        public partial int Test { get; set; }
    }
}";
        
        // lang=csharp
        const string generated = @"
#nullable enable
using RxBlazorV2.Model;
using RxBlazorV2.Interface;
using Microsoft.Extensions.DependencyInjection;
using R3;
using System;

namespace Test;

public partial class TestModel
{
    public override string ModelID => ""Test.TestModel"";

    private readonly CompositeDisposable _subscriptions = new();
    protected override IDisposable Subscriptions => _subscriptions;

    public partial int Test
    {
        get => field;
        set
        {
            field = value;
            StateHasChanged(nameof(Test));
        }
    }


}
";
        
        await RxBlazorGeneratorVerifier.VerifySourceGeneratorAsync(test, generated, "TestModel");
    }
}