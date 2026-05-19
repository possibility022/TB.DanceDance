namespace TB.DanceDance.Mobile.PageModels.Intents;

public abstract record UploadVideoIntent;

public sealed record UploadToGroupIntent : UploadVideoIntent;

public sealed record UploadToEventIntent(Guid EventId) : UploadVideoIntent;

public sealed record UploadToPrivateIntent : UploadVideoIntent;
