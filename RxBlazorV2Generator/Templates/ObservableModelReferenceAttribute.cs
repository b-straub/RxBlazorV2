#nullable enable
namespace RxBlazorV2.Model;

/// <summary>
/// Marks an ObservableModel class as having a dependency on another ObservableModel.
/// The source generator will automatically inject the referenced model via dependency injection
/// and merge their observable change notifications for reactive UI updates.
/// </summary>
/// <typeparam name="T">
/// The type of the ObservableModel to reference. Must inherit from ObservableModel.
/// The referenced model will be injected as a protected property with the same name as the type.
/// </typeparam>
/// <remarks>
/// <para>Key behaviors:</para>
/// <list type="bullet">
/// <item>
/// <description>Automatically generates a protected property of type T</description>
/// </item>
/// <item>
/// <description>Adds T as a constructor parameter for dependency injection</description>
/// </item>
/// <item>
/// <description>Merges observable change notifications from the referenced model</description>
/// </item>
/// <item>
/// <description>Analyzes method bodies to detect which properties of T are used</description>
/// </item>
/// <item>
/// <description>Subscribes only to relevant property changes for optimal performance</description>
/// </item>
/// </list>
/// <para>The generated property name follows the type name (e.g., ObservableModelReference&lt;UserModel&gt; creates protected UserModel UserModel { get; })</para>
/// <para>Multiple references are supported by applying the attribute multiple times.</para>
/// </remarks>
/// <example>
/// <code>
/// [ObservableModelReference&lt;UserModel&gt;]
/// [ObservableModelReference&lt;SettingsModel&gt;]
/// [ObservableModelScope(ModelScope.Scoped)]
/// public partial class DashboardModel : ObservableModel
/// {
///     public partial string Title { get; set; }
///     
///     [ObservableCommand(nameof(UpdateTitle), nameof(CanUpdateTitle))]
///     public partial IObservableCommand UpdateTitleCommand { get; }
///     
///     private void UpdateTitle()
///     {
///         // Generator detects UserModel.Name usage and subscribes to UserModel changes
///         Title = $"Welcome {UserModel.Name}";
///     }
///     
///     private bool CanUpdateTitle()
///     {
///         // Generator detects UserModel.IsLoggedIn and SettingsModel.AllowUpdates usage
///         return UserModel.IsLoggedIn &amp;&amp; SettingsModel.AllowUpdates;
///     }
/// }
/// 
/// // Generated constructor:
/// // public DashboardModel(UserModel userModel, SettingsModel settingsModel) : base()
/// // {
/// //     UserModel = userModel;
/// //     SettingsModel = settingsModel;
/// //     // Subscribes to UserModel.Name, UserModel.IsLoggedIn, and SettingsModel.AllowUpdates changes
/// // }
/// </code>
/// </example>
#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ObservableModelReferenceAttribute<T> : Attribute
    where T : class
{
}
#pragma warning restore CS9113