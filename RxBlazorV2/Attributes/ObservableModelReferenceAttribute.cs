#nullable enable
namespace RxBlazorV2.Model;

/// <summary>
/// Marks an ObservableModel class as having a dependency on another ObservableModel.
/// The source generator automatically injects the referenced model and merges change notifications.
/// </summary>
/// <typeparam name="T">
/// The type of the ObservableModel to reference. Must inherit from ObservableModel.
/// </typeparam>
/// <remarks>
/// <para>Multiple references are supported by applying the attribute multiple times.</para>
/// <para><b>Common diagnostics:</b> <see href="https://github.com/b-straub/RxBlazorV2/blob/master/Diagnostics/Help/RXBG006.md">RXBG006</see> (circular reference), <see href="https://github.com/b-straub/RxBlazorV2/blob/master/Diagnostics/Help/RXBG007.md">RXBG007</see> (invalid target), <see href="https://github.com/b-straub/RxBlazorV2/blob/master/Diagnostics/Help/RXBG008.md">RXBG008</see> (unused reference).</para>
/// </remarks>
#pragma warning disable CS9113
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ObservableModelReferenceAttribute<T> : Attribute
    where T : class
{
}

/// <summary>
/// Marks an ObservableModel class as having a dependency on another ObservableModel.
/// The source generator automatically injects the referenced model and merges change notifications.
/// Use this overload for open generic types (typeof(MyModel&lt;,&gt;)).
/// </summary>
/// <param name="modelReference">
/// The Type of the ObservableModel to reference. For open generic types (typeof(MyModel&lt;,&gt;)),
/// the referencing class must have compatible type parameters and constraints.
/// </param>
/// <remarks>
/// <para><b>Common diagnostics:</b> <see href="https://github.com/b-straub/RxBlazorV2/blob/master/Diagnostics/Help/RXBG006.md">RXBG006</see> (circular reference), <see href="https://github.com/b-straub/RxBlazorV2/blob/master/Diagnostics/Help/RXBG007.md">RXBG007</see> (invalid target), <see href="https://github.com/b-straub/RxBlazorV2/blob/master/Diagnostics/Help/RXBG008.md">RXBG008</see> (unused reference).</para>
/// <para><b>For generic types:</b> <see href="https://github.com/b-straub/RxBlazorV2/blob/master/Diagnostics/Help/RXBG013.md">RXBG013</see> (arity mismatch), <see href="https://github.com/b-straub/RxBlazorV2/blob/master/Diagnostics/Help/RXBG014.md">RXBG014</see> (constraint mismatch), <see href="https://github.com/b-straub/RxBlazorV2/blob/master/Diagnostics/Help/RXBG015.md">RXBG015</see> (invalid open generic).</para>
/// </remarks>
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