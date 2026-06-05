using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Serilog;
using TB.DanceDance.API.Contracts.Features.AccessManagement;
using TB.DanceDance.API.Contracts.Features.AccessManagement.Models;
using TB.DanceDance.API.Contracts.Features.Events.Models;
using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Features.Videos;
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
    public string Season { get; set; }

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

public partial class GetAccessPageModel : ObservableObject, IAppearingAware
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

    
    public async ValueTask OnAppearingAsync()
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
                RequestAccessRequest request = new RequestAccessRequest()
                {
                    Events = toRequest.Where(r => r.Type == SharingWithType.Event)
                        .Select(r => r.Id).ToArray(),
                    Groups = toRequest.Where(r => r.Type == SharingWithType.Group)
                        .Select(group => new RequestAccessGroupModel() { Id = group.Id, JoinedDate = DateTime.Now })
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
    
    private AccessModel MapFromEvent(EventModel eventModel, bool hasAccess, IReadOnlyCollection<Guid> pendingEvents)
    {
        return new AccessModel(hasAccess, pendingEvents.Contains(eventModel.Id))
        {
            Id = eventModel.Id,
            DateTime = null,
            Name = eventModel.Name,
            Type = SharingWithType.Event
        };
    }

    private AccessModel MapFromGroup(GroupModel groupModel, bool hasAccess, IReadOnlyCollection<Guid> pendingGroups)
    {
        return new AccessModel(hasAccess, pendingGroups.Contains(groupModel.Id))
        {
            Id = groupModel.Id,
            DateTime = null,
            Name = groupModel.Name,
            Type = SharingWithType.Group,
            Season = $"{groupModel.SeasonStart.Year} - {groupModel.SeasonEnd.Year}",
        };
    }
}