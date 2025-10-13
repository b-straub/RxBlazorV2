namespace RxBlazorV2Sample.Samples.ReactiveComposition;

public interface IValidationService
{
    public bool IsValid();
    
    public void SetCallerScope(bool fromModel);
}

public class ValidationService : IValidationService
{
    private bool _callerScopeFromModel;
    
    public bool IsValid()
    {
        return _callerScopeFromModel;
    }

    public void SetCallerScope(bool fromModel)
    {
        _callerScopeFromModel = fromModel;
    }
}