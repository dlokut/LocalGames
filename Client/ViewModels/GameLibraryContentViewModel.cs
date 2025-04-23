using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Client.Database;
using Client.Models;
using Client.Models.ServerApi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class GameLibraryContentViewModel : ViewModelBase
{
    private readonly Guid _gameId;

    private readonly GameLibraryViewModel _splitViewPaneModel;

    [ObservableProperty] private string _fillerText =
        "Game summary | Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod  tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim  veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea  commodo consequat. Duis aute irure dolor in reprehenderit in voluptate  velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint  occaecat cupidatat non proident, sunt in culpa qui officia deserunt  mollit anim id est laborum";

    [ObservableProperty] private string _gameName;

    [ObservableProperty] private string _gameSummary;

    [ObservableProperty] private Bitmap _coverBitmap;

    [ObservableProperty] private string _playtime;

    [ObservableProperty] private List<Bitmap> _artworksBitmaps = new List<Bitmap>();

    public GameLibraryContentViewModel(Guid gameId, string gameName, string gameSummary, string coverUrl,
        int playtimeMins, GameLibraryViewModel splitViewPaneModel)
    {
        _gameId = gameId;
        _splitViewPaneModel = splitViewPaneModel;
        
        GameName = gameName;
        GameSummary = gameSummary;
        SetPlaytime(playtimeMins);

        _ = LoadCover(coverUrl);
        //_ = LoadArtworks(artworkUrls);
    }

    [RelayCommand]
    private async Task PlayGame()
    {
        ProtonManager protonManager = new ProtonManager();
        await protonManager.LaunchGame(_gameId);
    }

    [RelayCommand]
    private async Task UninstallGame()
    {
        GameApiClient gameApiClient = new GameApiClient();
        await gameApiClient.UninstallGameAsync(_gameId);

        await _splitViewPaneModel.PopulateGamesAsync();
    }

    private async Task LoadCover(string url)
    {
        CoverBitmap = await LoadBitmapFromUrl(url);
    }

    private void SetPlaytime(int playtimeMins)
    {
        if (playtimeMins == 0) return;
        
        int hours = playtimeMins / 60;
        int mins = playtimeMins % 60;

        string playtimeString = "Playtime: ";

        if (hours > 0) playtimeString += $"{hours} Hours ";
        if (mins > 0) playtimeString += $"{mins} Minutes";

        Playtime = playtimeString;
    }

    private async Task LoadArtworks(List<string> urls)
    {
        foreach (string url in urls)
        {
            ArtworksBitmaps.Add(await LoadBitmapFromUrl(url));
        }
    }

    private async Task<Bitmap> LoadBitmapFromUrl(string url)
    {
        using HttpClient httpClient = new HttpClient();
        byte[] imageBytes = await httpClient.GetByteArrayAsync(url);

        return new Bitmap(new MemoryStream(imageBytes));
    }
}