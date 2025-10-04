using RxBlazorV2.Model;

namespace RxBlazorV2Sample.Models;

public record TestRecord(string Name, int Age);

public class TestClass
{
}

public partial class EquatableTestModel : ObservableModel
{
    public static readonly TestClass Test1 = new TestClass();
    public static readonly TestRecord TestRecord1 = new TestRecord("Test", 30);
    public static readonly TestRecord TestRecord2 = new TestRecord("Test", 31);
    
    public partial TestRecord TestRecord { get; set; } = TestRecord1;
    
    public partial TestClass TestClass { get; set; } = new();
    
    public partial Guid Guid  { get; set; }
    
    public partial DateTime DateTime  { get; set; }
    
    public partial Decimal Decimal  { get; set; }
}