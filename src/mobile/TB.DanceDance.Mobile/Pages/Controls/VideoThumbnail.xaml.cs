using Microsoft.Extensions.DependencyInjection;
using TB.DanceDance.Mobile.Library.Services.Network;

namespace TB.DanceDance.Mobile.Pages.Controls;

public partial class VideoThumbnail
{
    public static readonly BindableProperty ThumbnailUrlProperty = BindableProperty.Create(
        nameof(ThumbnailUrl),
        typeof(string),
        typeof(VideoThumbnail),
        propertyChanged: OnThumbnailUrlChanged);

    private static readonly BindablePropertyKey HasThumbnailPropertyKey = BindableProperty.CreateReadOnly(
        nameof(HasThumbnail),
        typeof(bool),
        typeof(VideoThumbnail),
        false);

    public static readonly BindableProperty HasThumbnailProperty = HasThumbnailPropertyKey.BindableProperty;

    private static readonly BindablePropertyKey ResolvedThumbnailUrlPropertyKey = BindableProperty.CreateReadOnly(
        nameof(ResolvedThumbnailUrl),
        typeof(string),
        typeof(VideoThumbnail),
        null);

    public static readonly BindableProperty ResolvedThumbnailUrlProperty = ResolvedThumbnailUrlPropertyKey.BindableProperty;

    private readonly NetworkAddressResolver? networkAddressResolver =
        IPlatformApplication.Current?.Services.GetService<NetworkAddressResolver>();

    public VideoThumbnail()
    {
        InitializeComponent();
    }

    public string? ThumbnailUrl
    {
        get => (string?)GetValue(ThumbnailUrlProperty);
        set => SetValue(ThumbnailUrlProperty, value);
    }

    public bool HasThumbnail => (bool)GetValue(HasThumbnailProperty);

    /// <summary>
    /// ThumbnailUrl rewritten for the running platform (e.g. host.docker.internal -> 10.0.2.2
    /// on the Android emulator), since &lt;Image&gt;/UriImageSource loads bypass the app's
    /// HttpClient pipeline and the DebuggingUrlHandler that normally handles this.
    /// </summary>
    public string? ResolvedThumbnailUrl => (string?)GetValue(ResolvedThumbnailUrlProperty);

    private static void OnThumbnailUrlChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (VideoThumbnail)bindable;
        var url = newValue as string;

        control.SetValue(HasThumbnailPropertyKey, !string.IsNullOrWhiteSpace(url));
        control.SetValue(ResolvedThumbnailUrlPropertyKey,
            string.IsNullOrWhiteSpace(url) ? null : control.networkAddressResolver?.Resolve(url) ?? url);
    }
}
