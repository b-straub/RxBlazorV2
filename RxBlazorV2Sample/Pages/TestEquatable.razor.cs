using R3;
using RxBlazorV2Sample.Components;
using RxBlazorV2Sample.Models;

namespace RxBlazorV2Sample.Pages
{
    public partial class TestEquatable : EquatableTestModelComponent
    {
        private int _stateChangedCounter;

        protected override void OnInitialized()
        {
            Subscriptions.Add(Model.Observable.Subscribe(r => { _stateChangedCounter++; }));
        }

        private void SetTestRecordUnchanged()
        {
            if (Model.TestRecord == EquatableTestModel.TestRecord1)
            {
                Model.TestRecord = EquatableTestModel.TestRecord1;
            }
            
            if (Model.TestRecord == EquatableTestModel.TestRecord2)
            {
                Model.TestRecord = EquatableTestModel.TestRecord2;
            }
        }
        
        private void SetTestRecordChanged()
        {
            if (Model.TestRecord == EquatableTestModel.TestRecord1)
            {
                Model.TestRecord = EquatableTestModel.TestRecord2;
            }
            
            if (Model.TestRecord == EquatableTestModel.TestRecord2)
            {
                Model.TestRecord = EquatableTestModel.TestRecord1;
            }
        }
    }
}