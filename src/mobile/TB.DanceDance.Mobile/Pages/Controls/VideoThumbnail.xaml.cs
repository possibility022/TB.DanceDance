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

    private static void OnThumbnailUrlChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (VideoThumbnail)bindable;
        control.SetValue(HasThumbnailPropertyKey, !string.IsNullOrWhiteSpace(newValue as string));
    }
}
