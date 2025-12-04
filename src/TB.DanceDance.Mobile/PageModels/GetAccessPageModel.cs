using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public record AccessModel
{
    public AccessModel(bool hasAccess, bool isPending)
    {
        IsRequesting = hasAccess;
        CanBeRequested = !hasAccess && !isPending;
        IsPending = isPending;
    }
    public string Name { get; init; } = string.Empty;
    public DateTime? DateTime { get; init; }
    public bool IsRequesting { get; set; }
    public bool CanBeRequested { get; }
    public bool IsPending { get; set; }

    public string TypeAsFriendlyString =>
        Type switch
        {
            SharingWithType.Group => "Grupa",
            SharingWithType.Event => "Wydarzenie",
            _ => throw new Exception("Unknown type" + Type.ToString())
        };

    public SharingWithType Type { get; set; } = SharingWithType.NotSpecified;
    public Guid Id { get; set; }
}

public partial class GetAccessPageModel : ObservableObject
{
    private readonly IDanceHttpApiClient apiClient;

    public GetAccessPageModel(IDanceHttpApiClient  apiClient)
    {
        this.apiClient = apiClient;
    }

    [ObservableProperty] private List<AccessModel> accesses = [];
    
    [ObservableProperty] bool isRefreshing;
    [ObservableProperty] bool canBeRequested;
    
    bool isLoaded = false;

    
    [RelayCommand]
    private async Task Appearing()
    {
        if (!isLoaded)
            await Refresh();
    }

    [RelayCommand]
    private async Task RequestAccess()
    {
        await SendGetAccessRequest();
    }

    private async Task SendGetAccessRequest()
    {
        try
        {
            CanBeRequested = false;
            var toRequest = Accesses.Where(r => r.CanBeRequested && r.IsRequesting)
                .ToArray();

            if (toRequest.Any())
            {
                RequestAssigmentModelRequest request = new RequestAssigmentModelRequest()
                {
                    Events = toRequest.Where(r => r.Type == SharingWithType.Event)
                        .Select(r => r.Id).ToArray(),
                    Groups = toRequest.Where(r => r.Type == SharingWithType.Group)
                        .Select(group => new GroupAssignmentModel() { Id = group.Id, JoinedDate = DateTime.Now })
                        .ToArray()
                };

                await apiClient.RequestAccess(request);

                await Refresh();
            }
        }
        catch(Exception ex)
        {
            Log.Error(ex, "Error on requesting access.");
        }
        finally
        {
            CanBeRequested = true;
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        try
        {
            IsRefreshing = true;
            isLoaded = true;
            await LoadFromApi();
        }
        catch (Exception ex)
        {
            Log.Error("Could not load list for accesses.", ex);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task LoadFromApi()
    {
        var response = await apiClient.GetUserAccesses();
        var list = new List<AccessModel>();

        list.AddRange(response.Available.Groups.Select(g => MapFromGroup(g, false, response.Pending.Groups)));
        list.AddRange(response.Assigned.Groups.Select(g => MapFromGroup(g, true, response.Pending.Groups)));
        list.AddRange(response.Available.Events.Select(g => MapFromEvent(g, false, response.Pending.Events)));
        list.AddRange(response.Assigned.Events.Select(g => MapFromEvent(g, true, response.Pending.Events)));
        
        Accesses = list;
    }
    
    private AccessModel MapFromEvent(Event @event, bool hasAccess, IReadOnlyCollection<Guid> pendingEvents)
    {
        return new AccessModel(hasAccess, pendingEvents.Contains(@event.Id))
        {
            Id = @event.Id,
            DateTime = null,
            Name = @event.Name,
            Type = SharingWithType.Event
        };
    }

    private AccessModel MapFromGroup(Group group, bool hasAccess, IReadOnlyCollection<Guid> pendingGroups)
    {
        return new AccessModel(hasAccess, pendingGroups.Contains(group.Id))
        {
            Id = group.Id,
            DateTime = null,
            Name = group.Name,
            Type = SharingWithType.Group
        };
    }
}