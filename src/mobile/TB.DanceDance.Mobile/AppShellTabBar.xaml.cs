using System.ComponentModel;

namespace TB.DanceDance.Mobile;

public partial class AppShellTabBar
{
    private ShellItem Item => BindingContext as ShellItem ??
                              throw new InvalidOperationException(
                                  "AppShellTabBar must have a ShellItem as its BindingContext");

    public AppShellTabBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged += OnRequestedThemeChanged;
        if (BindingContext is ShellItem item)
            UpdateCurrentItem(item.CurrentItem);
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (Application.Current is not null)
            Application.Current.RequestedThemeChanged -= OnRequestedThemeChanged;
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) =>
        UpdateCurrentItem(Item.CurrentItem);

    protected override void OnBindingContextChanged()
    {
        if (ShellItem is not null)
        {
            ShellItem.PropertyChanged -= OnCurrentItemChanged;
        }

        if (BindingContext is ShellItem item)
        {
            ShellItem = item;
            item.PropertyChanged += OnCurrentItemChanged;
            UpdateCurrentItem(item.CurrentItem);
        }
    }

    public ShellItem? ShellItem { get; set; }

    private void OnCurrentItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == ShellItem.CurrentItemProperty.PropertyName)
        {
            UpdateCurrentItem(ShellItem!.CurrentItem);
        }
    }

    private void UpdateCurrentItem(ShellSection currentItem)
    {
        var selectedIndex = ShellItem?.Items.IndexOf(currentItem) ?? 0;
        var darkTheme = Application.Current?.RequestedTheme == AppTheme.Dark;
        var activeColor = GetColor(darkTheme ? "PrimaryDark" : "PrimaryIcon", Colors.Teal);
        var inactiveColor = GetColor(darkTheme ? "Gray400" : "Gray500", Colors.Gray);

        for (var i = 0; i < Buttons.Count; i++)
        {
            if (Buttons[i] is ImageButton { Source: FontImageSource source })
            {
                source.Color = i == selectedIndex ? activeColor : inactiveColor;
            }
        }
    }

    private async void IconClicked(object? sender, EventArgs e)
    {
        var icon = (ImageButton)sender!;
        var parent = (Layout)icon.Parent!;
        var index = parent.IndexOf(icon);

        var targetSection = Item.Items[index];
        if (targetSection == Item.CurrentItem)
        {
            return;
        }

        await Shell.Current.GoToAsync($"//{targetSection.CurrentItem.Route}");
    }

    private static Color GetColor(string key, Color fallback) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : fallback;
}
