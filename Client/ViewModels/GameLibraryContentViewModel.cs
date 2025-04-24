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
using Client.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class GameLibraryContentViewModel : ViewModelBase
{
    private readonly Guid _gameId;

    private readonly ServerGame _serverGame;
    

    private readonly GameLibraryViewModel _splitViewPaneModel;

    [ObservableProperty] private string _fillerText =
        "Game summary | Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod  tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim  veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea  commodo consequat. Duis aute irure dolor in reprehenderit in voluptate  velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint  occaecat cupidatat non proident, sunt in culpa qui officia deserunt  mollit anim id est laborum";

    [ObservableProperty] private string _gameName;

    [ObservableProperty] private string _gameSummary;

    [ObservableProperty] private Bitmap _coverBitmap;

    [ObservableProperty] private string _playtime;

    
    [ObservableProperty] private bool _playButtonVisible;

    [ObservableProperty] private bool _playButtonEnabled = false;
    
    [ObservableProperty] private bool _settingsButtonVisible;
    
    [ObservableProperty] private bool _uninstallButtonVisible;
    
    [ObservableProperty] private bool _installButtonVisible;
    

    [ObservableProperty] private List<Bitmap> _artworksBitmaps = new List<Bitmap>();

    public GameLibraryContentViewModel(DownloadedGame downloadedGame, int playtimeMins,
        GameLibraryViewModel splitViewPaneModel)
    {
        _gameId = downloadedGame.Id;
        _splitViewPaneModel = splitViewPaneModel;
        
        GameName = downloadedGame.Name;
        GameSummary = downloadedGame.Summary;
        SetPlaytime(playtimeMins);


        PlayButtonVisible = true;
        SettingsButtonVisible = true;
        UninstallButtonVisible = true;
        InstallButtonVisible = false;
        
        _ = LoadCover(downloadedGame.CoverUrl);
        _ = SetPlayButtonEnabled(downloadedGame.Id);
        //_ = LoadArtworks(artworkUrls);

    }

    public GameLibraryContentViewModel(ServerGame serverGame, int playtimeMins, GameLibraryViewModel splitViewPaneModel)
    {
        _gameId = serverGame.Id;
        _serverGame = serverGame;
        _splitViewPaneModel = splitViewPaneModel;

        GameName = serverGame.Name;
        _gameSummary = serverGame.Summary;
        SetPlaytime(playtimeMins);

        _ = LoadCover(serverGame.CoverUrl);

        PlayButtonVisible = false;
        SettingsButtonVisible = false;
        UninstallButtonVisible = false;
        InstallButtonVisible = true;

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
        _splitViewPaneModel.SetContentViewToEmpty();
    }

    [RelayCommand]
    private async Task InstallGame()
    {
        GameApiClient gameApiClient = new GameApiClient();
        await gameApiClient.DownloadGameAsync(_serverGame);
        
        await _splitViewPaneModel.PopulateGamesAsync();
        _splitViewPaneModel.SetContentViewToEmpty();
    }

    [RelayCommand]
    private async Task GoToSettings()
    {
        GameApiClient gameApiClient = new GameApiClient();
        ProtonSettings protonSettings = await gameApiClient.GetProtonSettingsAsync(_gameId);
        
        MainWindowViewModel.SwitchViews(new SettingsViewModel(protonSettings)
        {
            MainWindowViewModel = this.MainWindowViewModel
        });
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

    private async Task SetPlayButtonEnabled(Guid gameId)
    {
        ProtonManager protonManager = new ProtonManager();
        PlayButtonEnabled = await protonManager.CanLaunchGameAsync(gameId);
    }
}