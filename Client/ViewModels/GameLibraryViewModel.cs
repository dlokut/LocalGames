using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Client.Database;
using Client.Models.ServerApi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class GameLibraryViewModel : ViewModelBase
{
    private Dictionary<Guid, int> playtimesById = new Dictionary<Guid, int>();
    
    [ObservableProperty] private GameLibraryContentViewModel _splitViewContentViewModel;

    [ObservableProperty] private List<ServerGame> _uninstalledGames = new List<ServerGame>();
    
    [ObservableProperty] private List<DownloadedGame> _installedGames;

    [ObservableProperty] private DownloadedGame? _selectedDownloadedGame;
    
    [ObservableProperty] private ServerGame? _selectedUninstalledGame;

    public GameLibraryViewModel()
    {
        _ = PopulateGamesAsync();
    }

    [RelayCommand]
    private void GoToUploadGame()
    {
        MainWindowViewModel.SwitchViews(new UploadGameViewModel()
        {
            MainWindowViewModel = this.MainWindowViewModel
        });
    }
    
    partial void OnSelectedDownloadedGameChanged(DownloadedGame? value)
    {
        if (value == null) return;


        SplitViewContentViewModel = new GameLibraryContentViewModel(value, playtimesById[value.Id],
            this);
        SplitViewContentViewModel.MainWindowViewModel = this.MainWindowViewModel;
        
        SelectedUninstalledGame = null;
    }
    
    partial void OnSelectedUninstalledGameChanged(ServerGame? value)
    {
        if (value == null) return;

       SplitViewContentViewModel = new GameLibraryContentViewModel(value, playtimesById[value.Id], this);
       SplitViewContentViewModel.MainWindowViewModel = this.MainWindowViewModel;
        
       SelectedDownloadedGame = null;
    }

    [RelayCommand]
    private async Task RefreshGames()
    {
        await PopulateGamesAsync();
    }
    
    public async Task PopulateGamesAsync()
    {
        GameApiClient gameApiClient = new GameApiClient();
        InstalledGames = await gameApiClient.GetAllDownloadedGames();
        
        List<ServerGame> gamesOnServer = await gameApiClient.GetAllGamesOnServer();
        List<ServerGame> uninstalledGames = new List<ServerGame>();

        List<Guid> installedGamesIds = InstalledGames.Select(ig => ig.Id).ToList();
        foreach (ServerGame uninstalledGame in gamesOnServer)
        {
            if (!installedGamesIds.Contains(uninstalledGame.Id))
            {
                uninstalledGames.Add(uninstalledGame);
            }
        }

        UninstalledGames = uninstalledGames;

        List<Guid> uninstalledGamesIds = UninstalledGames.Select(ug => ug.Id).ToList();
        List<Guid> allGamesIds = installedGamesIds.Concat(uninstalledGamesIds).ToList();
        await GetPlaytimesAsync(allGamesIds);
    }

    private async Task GetPlaytimesAsync(List<Guid> gameIds)
    {
        GameApiClient gameApiClient = new GameApiClient();
        foreach (Guid gameId in gameIds)
        {
            int playtimeMins = await gameApiClient.GetPlaytimeAsync(gameId);
            playtimesById[gameId] = playtimeMins;
        }
    }

    public void SetContentViewToEmpty()
    {
        SplitViewContentViewModel = null;

        SelectedDownloadedGame = null;
        SelectedUninstalledGame = null;
    }
}