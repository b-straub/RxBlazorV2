using ObservableCollections;
using RxBlazorV2.Interface;
using RxBlazorV2.Model;
using RxBlazorV2.MudBlazor.Components;
using RxBlazorV2.MudBlazor.Components.Razor;

namespace RxBlazorV2.MudBlazor.Sample.Model;

public sealed record Contact(Guid Id, string Name, string Initial);

[ObservableModelScope(ModelScope.Scoped)]
[ObservableComponent]
public partial class ContactGroupsDemoModel : ObservableModel
{
    public partial ContactGroupsDemoModel(StatusModel statusModel);

    public const string AllListId = "all-contacts";
    public const string VipListId = "vip-group";

    public ObservableList<Contact> AllContacts { get; } = new();
    public ObservableList<Contact> VipContacts { get; } = new();

    [ObservableCommand(nameof(ReorderAsync))]
    public partial IObservableCommandAsync<SortableMove> ReorderCommand { get; }

    [ObservableCommand(nameof(ResetAsync))]
    public partial IObservableCommandAsync ResetCommand { get; }

    protected override Task OnContextReadyAsync(CancellationToken cancellationToken)
    {
        Seed();
        return Task.CompletedTask;
    }

    private void Seed()
    {
        AllContacts.Clear();
        VipContacts.Clear();
        AllContacts.Add(new Contact(Guid.NewGuid(), "Ada Lovelace", "AL"));
        AllContacts.Add(new Contact(Guid.NewGuid(), "Alan Turing", "AT"));
        AllContacts.Add(new Contact(Guid.NewGuid(), "Grace Hopper", "GH"));
        AllContacts.Add(new Contact(Guid.NewGuid(), "Donald Knuth", "DK"));
        AllContacts.Add(new Contact(Guid.NewGuid(), "Linus Torvalds", "LT"));
        AllContacts.Add(new Contact(Guid.NewGuid(), "Margaret Hamilton", "MH"));
        AllContacts.Add(new Contact(Guid.NewGuid(), "Anders Hejlsberg", "AH"));
        AllContacts.Add(new Contact(Guid.NewGuid(), "Barbara Liskov", "BL"));
    }

    private async Task ReorderAsync(SortableMove move)
    {
        var src = ListById(move.SourceListId);
        if (src is null)
        {
            return;
        }

        // Drag-out-to-remove: dropped outside any valid target on a MOVE-pull list.
        if (move.IsRemove)
        {
            var item = src[move.FromIndex];
            src.RemoveAt(move.FromIndex);
            StatusModel.AddInfo($"Removed {item.Name} from {LabelFor(move.SourceListId)}");
            await Task.CompletedTask;
            return;
        }

        var tgt = ListById(move.TargetListId);
        if (tgt is null)
        {
            return;
        }

        if (move.SourceListId == move.TargetListId)
        {
            // Intra-list reorder.
            var item = src[move.FromIndex];
            src.RemoveAt(move.FromIndex);
            src.Insert(Math.Min(move.ToIndex, src.Count), item);
            StatusModel.AddInfo($"Reordered {item.Name} in {LabelFor(move.SourceListId)}");
        }
        else
        {
            var item = src[move.FromIndex];
            if (tgt.Any(c => c.Id == item.Id))
            {
                StatusModel.AddWarning($"{item.Name} is already in {LabelFor(move.TargetListId)}");
                return;
            }
            if (move.IsClone == false)
            {
                src.RemoveAt(move.FromIndex);
            }
            tgt.Insert(Math.Min(move.ToIndex, tgt.Count), item);
            var verb = move.IsClone ? "Added" : "Moved";
            StatusModel.AddSuccess($"{verb} {item.Name} → {LabelFor(move.TargetListId)}");
        }

        await Task.CompletedTask;
    }

    private async Task ResetAsync()
    {
        Seed();
        StatusModel.AddInfo("Reset contact lists");
        await Task.CompletedTask;
    }

    private ObservableList<Contact>? ListById(string listId) => listId switch
    {
        AllListId => AllContacts,
        VipListId => VipContacts,
        _ => null
    };

    private static string LabelFor(string listId) => listId switch
    {
        AllListId => "All Contacts",
        VipListId => "VIP Group",
        _ => listId
    };
}
