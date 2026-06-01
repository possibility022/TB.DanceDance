namespace TB.DanceDance.Utilities.Mediating;

public interface INotificationHandler<TNotification>
    where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken = default);
}
