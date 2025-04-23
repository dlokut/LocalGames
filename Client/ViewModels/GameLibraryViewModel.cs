using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Client.Database;
using Client.Models.ServerApi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class GameLibraryViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _splitViewContentViewModel;

    [ObservableProperty] private List<ServerGame> _uninstalledGames;
    
    [ObservableProperty] private List<DownloadedGame> _installedGames;

    public GameLibraryViewModel()
    {
        _ = PopulateGamesAsync();
    }

    [RelayCommand]
    private async Task Test()
    {
        await PopulateInstalledGames();
    }
    
    private async Task PopulateUninstalledGames()
    {
        GameApiClient gameApiClient = new GameApiClient();
        UninstalledGames = await gameApiClient.GetAllGamesOnServer();
    }

    private async Task PopulateInstalledGames()
    {
        GameApiClient gameApiClient = new GameApiClient();
        InstalledGames = await gameApiClient.GetAllDownloadedGames();
    }

    private async Task PopulateGamesAsync()
    {
        GameApiClient gameApiClient = new GameApiClient();
        InstalledGames = await gameApiClient.GetAllDownloadedGames();
        
        List<ServerGame> uninstalledGames = await gameApiClient.GetAllGamesOnServer();

        List<Guid> installedGamesIds = InstalledGames.Select(ig => ig.Id).ToList();
        foreach (ServerGame uninstalledGame in uninstalledGames)
        {
            if (installedGamesIds.Contains(uninstalledGame.Id))
            {
                uninstalledGames.Remove(uninstalledGame);
            }
        }

        UninstalledGames = uninstalledGames;
    }
}