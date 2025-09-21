#nullable enable
namespace RxBlazorV2.Model;

/// <summary>
/// Defines the dependency injection lifetime scope for ObservableModel instances.
/// Determines how the model is registered in the DI container and how instances are shared.
/// </summary>
/// <remarks>
/// <para>Important considerations:</para>
/// <list type="bullet">
/// <item>
/// <term>Singleton</term>
/// <description>Required when multiple ObservableComponent&lt;T&gt; instances use the same model. Ensures data consistency across components.</description>
/// </item>
/// <item>
/// <term>Scoped</term>
/// <description>Good for models that should live for the duration of a request/page but can have multiple instances in different scopes.</description>
/// </item>
/// <item>
/// <term>Transient</term>
/// <description>Creates a new instance every time the model is requested. Use sparingly as it can break reactive bindings.</description>
/// </item>
/// </list>
/// <para>The analyzer will report diagnostic RXBG010 if multiple components use the same model with non-Singleton scope.</para>
/// </remarks>
public enum ModelScope
{
    /// <summary>
    /// Single instance shared across the entire application lifetime.
    /// Required when multiple ObservableComponent&lt;T&gt; instances use the same model.
    /// This is the default scope when no attribute is specified.
    /// </summary>
    /// <remarks>
    /// Use for:
    /// <list type="bullet">
    /// <item>Global application state</item>
    /// <item>User session data</item>
    /// <item>Settings and configuration</item>
    /// <item>Any model used by multiple components</item>
    /// </list>
    /// </remarks>
    Singleton,
    
    /// <summary>
    /// Single instance per scope (typically per HTTP request in server scenarios, or per component tree in client scenarios).
    /// Can only be used when the model is consumed by a single component.
    /// </summary>
    /// <remarks>
    /// Use for:
    /// <list type="bullet">
    /// <item>Page-specific data that shouldn't be shared across pages</item>
    /// <item>Form models that should reset between uses</item>
    /// <item>Models that hold temporary state</item>
    /// </list>
    /// </remarks>
    Scoped,
    
    /// <summary>
    /// New instance created every time the model is requested.
    /// Use with extreme caution as it can break reactive bindings and component communication.
    /// Can only be used when the model is consumed by a single component.
    /// </summary>
    /// <remarks>
    /// Use for:
    /// <list type="bullet">
    /// <item>Lightweight data transfer objects</item>
    /// <item>Models that need to be completely isolated</item>
    /// <item>Testing scenarios</item>
    /// </list>
    /// Generally avoid this scope unless you have a specific need for complete isolation.
    /// </remarks>
    Transient
}

/// <summary>
/// Specifies the dependency injection lifetime scope for an ObservableModel class.
/// Controls how instances of the model are created, shared, and disposed of by the DI container.
/// </summary>
/// <param name="scope">
/// The lifetime scope for this model. If not specified, defaults to Singleton.
/// </param>
/// <remarks>
/// <para>Default behavior: If this attribute is not applied, the model defaults to Singleton scope.</para>
/// <para>Validation: The analyzer enforces that models used by multiple ObservableComponent&lt;T&gt; instances must have Singleton scope (diagnostic RXBG010).</para>
/// <para>Registration: The source generator automatically registers the model with the specified scope in the DI container via the generated AddObservableModels() extension method.</para>
/// </remarks>
/// <example>
/// <code>
/// // Singleton scope (recommended for shared models)
/// [ObservableModelScope(ModelScope.Singleton)]
/// public partial class UserSessionModel : ObservableModel
/// {
///     public partial string Username { get; set; }
///     public partial bool IsAuthenticated { get; set; }
/// }
/// 
/// // Scoped (for page-specific models used by single component)
/// [ObservableModelScope(ModelScope.Scoped)]
/// public partial class ContactFormModel : ObservableModel
/// {
///     public partial string Name { get; set; }
///     public partial string Email { get; set; }
/// }
/// 
/// // Default Singleton (no attribute needed)
/// public partial class AppSettingsModel : ObservableModel
/// {
///     public partial string Theme { get; set; }
///     public partial string Language { get; set; }
/// }
/// </code>
/// </example>
#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ObservableModelScopeAttribute(ModelScope scope) : Attribute
{
    /// <summary>
    /// Gets the dependency injection lifetime scope for this ObservableModel.
    /// </summary>
    public ModelScope Scope { get; } = scope;
}
#pragma warning restore CS9113