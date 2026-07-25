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

    private static readonly BindablePropertyKey ResolvedThumbnailSourcePropertyKey = BindableProperty.CreateReadOnly(
        nameof(ResolvedThumbnailSource),
        typeof(ImageSource),
        typeof(VideoThumbnail),
        null);

    public static readonly BindableProperty ResolvedThumbnailSourceProperty = ResolvedThumbnailSourcePropertyKey.BindableProperty;

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
    public ImageSource? ResolvedThumbnailSource => (ImageSource?)GetValue(ResolvedThumbnailSourceProperty);

    private static void OnThumbnailUrlChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (VideoThumbnail)bindable;
        var url = newValue as string;

        control.SetValue(HasThumbnailPropertyKey, !string.IsNullOrWhiteSpace(url));
        var resolvedUrl = string.IsNullOrWhiteSpace(url)
            ? null
            : control.networkAddressResolver?.Resolve(url) ?? url;

        control.SetValue(
            ResolvedThumbnailSourcePropertyKey,
            Uri.TryCreate(resolvedUrl, UriKind.Absolute, out var uri)
                ? new UriImageSource
                {
                    Uri = uri,
                    CachingEnabled = true,
                    CacheValidity = TimeSpan.FromDays(1)
                }
                : null);
    }
}
