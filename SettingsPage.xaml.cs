using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Komik.Models;
using Komik.ViewModels;

namespace Komik;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; } = new();
    private bool _isInitializing = true;

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MainWindow.Instance?.UpdateTitle("Settings");
        _isInitializing = true;
        ViewModel.IsNotificationOpen = false;
        await ViewModel.InitializeAsync();
        ViewModel.IsNotificationOpen = false;
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            _isInitializing = false;
            ViewModel.IsNotificationOpen = false;
        });
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
        else
        {
            Frame.Navigate(typeof(LibraryPage));
        }
    }

    private void SettingChanged(object sender, object e)
    {
        if (_isInitializing) return;
        _ = ViewModel.SaveSettingsAsync();
    }

    private async void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: WatchedFolder folder })
        {
            var dialog = new ContentDialog
            {
                Title = "Remove Library Folder?",
                Content = new TextBlock
                {
                    Text = $"Are you sure you want to remove this folder from your library?\n\n{folder.Path}\n\nComics located in this folder will no longer appear in your library. Your actual comic files on your hard drive will NOT be deleted.",
                    TextWrapping = TextWrapping.Wrap
                },
                PrimaryButtonText = "Remove Folder",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.RemoveFolderAsync(folder);
            }
        }
    }

    private async void CacheAllCoversButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Cache All Comic Covers?",
            Content = new TextBlock
            {
                Text = "Are you sure you want to generate and cache cover thumbnails for all comics in your library?\n\nThis will scan your library and pre-render cover images so your comics display instantaneously during browsing and scrolling.",
                TextWrapping = TextWrapping.Wrap
            },
            PrimaryButtonText = "Cache All Covers",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.CacheAllCoversAsync();
        }
    }

    private async void ClearCacheButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Clear Cover Thumbnail Cache?",
            Content = new TextBlock
            {
                Text = "Are you sure you want to clear the cover thumbnail cache?\n\nThis will remove all cached cover images from disk to free up storage space. Your actual comic files, reading progress, tags, and bookmarks will NOT be affected.",
                TextWrapping = TextWrapping.Wrap
            },
            PrimaryButtonText = "Clear Cache",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.ClearThumbnailCacheAsync();
        }
    }
}
