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

/// <summary>
/// Marks an ObservableModel class as having a dependency on another ObservableModel using a Type parameter.
/// This overload is primarily used for referencing open generic types (e.g., typeof(MyModel&lt;,&gt;)).
/// The source generator will automatically inject the referenced model via dependency injection
/// and merge their observable change notifications for reactive UI updates.
/// </summary>
/// <param name="modelReference">
/// The Type of the ObservableModel to reference. Must inherit from ObservableModel.
/// Supports both closed generic types (typeof(MyModel&lt;int&gt;)) and open generic types (typeof(MyModel&lt;,&gt;)).
/// For open generic types, the referencing class must have compatible type constraints.
/// </param>
/// <remarks>
/// <para>This attribute is particularly useful for referencing generic ObservableModel types where:</para>
/// <list type="bullet">
/// <item>
/// <description>You want to reference an open generic type like MyModel&lt;T, P&gt;</description>
/// </item>
/// <item>
/// <description>The referencing class has the same or compatible generic parameters</description>
/// </item>
/// <item>
/// <description>You need to maintain type parameter relationships between models</description>
/// </item>
/// </list>
/// <para><strong>Type Constraint Validation:</strong></para>
/// <list type="bullet">
/// <item>
/// <description>The referenced type must inherit from ObservableModel</description>
/// </item>
/// <item>
/// <description>For open generic types, type constraints must match between referenced and referencing classes</description>
/// </item>
/// <item>
/// <description>Generic arity (number of type parameters) must be compatible</description>
/// </item>
/// </list>
/// <para><strong>Code Generation:</strong></para>
/// <list type="bullet">
/// <item>
/// <description>Generates a protected property with the concrete generic type</description>
/// </item>
/// <item>
/// <description>Adds constructor parameter for dependency injection</description>
/// </item>
/// <item>
/// <description>Subscribes to relevant property changes from the referenced model</description>
/// </item>
/// <item>
/// <description>Handles DI registration for generic types automatically</description>
/// </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Example 1: Referencing an open generic type with matching constraints
/// [ObservableModelReference(typeof(GenericModel&lt;,&gt;))]
/// [ObservableModelScope(ModelScope.Scoped)]
/// public partial class AnotherGenericModel&lt;T, P&gt; : ObservableModel 
///     where T : class where P : struct
/// {
///     // Generated property: protected GenericModel&lt;T, P&gt; GenericModel { get; private set; }
///     
///     // Access referenced model properties
///     public IEnumerable&lt;T&gt; Items =&gt; GenericModel.Tlist;
/// }
/// 
/// // Example 2: Referencing a closed generic type
/// [ObservableModelReference(typeof(GenericModel&lt;string, int&gt;))]
/// public partial class SpecificModel : ObservableModel
/// {
///     // Generated property: protected GenericModel&lt;string, int&gt; GenericModel { get; private set; }
/// }
/// 
/// // Example 3: Multiple generic references
/// [ObservableModelReference(typeof(UserModel&lt;T&gt;))]
/// [ObservableModelReference(typeof(SettingsModel))]
/// public partial class DashboardModel&lt;T&gt; : ObservableModel where T : IEntity
/// {
///     // Generated properties:
///     // protected UserModel&lt;T&gt; UserModel { get; private set; }
///     // protected SettingsModel SettingsModel { get; private set; }
/// }
/// </code>
/// </example>
public class ObservableModelReferenceAttribute : Attribute
{
    /// <summary>
    /// Gets the Type of the referenced ObservableModel.
    /// </summary>
    public Type ModelReference { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableModelReferenceAttribute"/> class.
    /// </summary>
    /// <param name="modelReference">The Type of the ObservableModel to reference.</param>
    public ObservableModelReferenceAttribute(Type modelReference) 
    {
        ModelReference = modelReference;
    }
}
#pragma warning restore CS9113