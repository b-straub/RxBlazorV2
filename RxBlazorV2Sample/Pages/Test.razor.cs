using R3;
using Microsoft.AspNetCore.Components;
using RxBlazorV2.Component;
using RxBlazorV2Sample.HelperModel;

namespace RxBlazorV2Sample.Pages
{
     
    public partial class Test : ObservableComponent<AnotherGenericModel<string, int>>
    {
        [Inject]
        public required GenericModel<string, int> GenericModel { get; init; }

        protected override void OnInitialized()
        {
            GenericModel.Observable.Subscribe(r =>
            {
                Console.WriteLine($"AnotherGenericModel.Observable: {r}");
            });
        }
    }
}