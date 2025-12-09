using RxBlazorV2.Model;
using RxBlazorV2Sample.Samples.Helpers;

namespace RxBlazorV2Sample.Samples.PropertyTriggers;

[ObservableComponent]
[ObservableModelScope(ModelScope.Scoped)]
public partial class PropertyTriggersModel : SampleBaseModel
{
    public override string Usage => "Property triggers execute internal methods automatically when properties change";

    // Simple sync trigger
    [ObservableTrigger(nameof(ValidateEmail))]
    public partial string Email { get; set; } = "";

    // Async trigger with can-trigger guard
    [ObservableTriggerAsync(nameof(AutoSaveAsync), nameof(CanAutoSave))]
    public partial string DocumentContent { get; set; } = "";

    // Parametrized trigger - passes a value to the method
    [ObservableTrigger<string>(nameof(LogChange), "Counter changed")]
    public partial int Counter { get; set; }

    // Multiple triggers on same property
    [ObservableTrigger(nameof(UpdateFullName))]
    [ObservableTrigger(nameof(ValidateName))]
    public partial string FirstName { get; set; } = "";

    [ObservableTrigger(nameof(UpdateFullName))]
    public partial string LastName { get; set; } = "";

    // Properties updated by triggers
    public partial string EmailValidationMessage { get; set; } = "";
    public partial bool IsEmailValid { get; set; }
    public partial string FullName { get; set; } = "";
    public partial string NameValidationMessage { get; set; } = "";
    public partial string SaveStatus { get; set; } = "Not saved";
    public partial bool AutoSaveEnabled { get; set; } = true;
    public partial int TriggerExecutionCount { get; set; }

    // Sync validation method
    private void ValidateEmail()
    {
        TriggerExecutionCount++;
        LogEntries.Add(new LogEntry($"ValidateEmail triggered for: {Email}", DateTime.Now));

        if (string.IsNullOrWhiteSpace(Email))
        {
            EmailValidationMessage = "";
            IsEmailValid = false;
        }
        else if (Email.Contains('@') && Email.Contains('.'))
        {
            EmailValidationMessage = "Valid email format";
            IsEmailValid = true;
        }
        else
        {
            EmailValidationMessage = "Invalid email format";
            IsEmailValid = false;
        }
    }

    // Async save method with guard
    private async Task AutoSaveAsync()
    {
        TriggerExecutionCount++;
        SaveStatus = "Saving...";
        LogEntries.Add(new LogEntry($"AutoSaveAsync triggered, content length: {DocumentContent.Length}", DateTime.Now));

        await Task.Delay(500); // Simulate save operation

        SaveStatus = $"Saved at {DateTime.Now:HH:mm:ss}";
        LogEntries.Add(new LogEntry("AutoSaveAsync completed", DateTime.Now));
    }

    private bool CanAutoSave()
    {
        return AutoSaveEnabled && !string.IsNullOrWhiteSpace(DocumentContent);
    }

    // Parametrized trigger method
    private void LogChange(string message)
    {
        TriggerExecutionCount++;
        LogEntries.Add(new LogEntry($"{message}: Counter = {Counter}", DateTime.Now));
    }

    // Multiple triggers can call the same method
    private void UpdateFullName()
    {
        TriggerExecutionCount++;
        FullName = $"{FirstName} {LastName}".Trim();
        LogEntries.Add(new LogEntry($"UpdateFullName triggered: {FullName}", DateTime.Now));
    }

    private void ValidateName()
    {
        TriggerExecutionCount++;
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            NameValidationMessage = "First name is required";
        }
        else if (FirstName.Length < 2)
        {
            NameValidationMessage = "First name must be at least 2 characters";
        }
        else
        {
            NameValidationMessage = "";
        }
        LogEntries.Add(new LogEntry($"ValidateName triggered: {NameValidationMessage}", DateTime.Now));
    }
}
