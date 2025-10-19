using R3;

namespace RxBlazorV2.CoreTests.TestFixtures;

public partial class SelectiveFilterTestComponent
{
    /// <summary>
    /// Manually specified filter for test - only includes Counter (used in razor markup).
    /// </summary>
    protected override string[] Filter()
    {
        return [
            "Model.Counter"
        ];
    }

    protected override void InitializeGeneratedCode()
    {
        var filter = Filter();
        if (filter.Length == 0)
        {
            Subscriptions.Add(Model.Observable
                .Chunk(TimeSpan.FromMilliseconds(100))
                .Subscribe(chunks =>
                {
                    var props = chunks.SelectMany(c => c).ToArray();
                    InvokeAsync(() => StateHasChanged());
                }));
        }
        else
        {
            Subscriptions.Add(Model.Observable
                .Where(props => props.Intersect(filter).Any())
                .Chunk(TimeSpan.FromMilliseconds(100))
                .Subscribe(chunks =>
                {
                    var props = chunks.SelectMany(c => c).ToArray();
                    InvokeAsync(() => StateHasChanged());
                }));
        }
    }
}
