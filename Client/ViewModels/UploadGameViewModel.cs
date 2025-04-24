using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Client.Models.ServerApi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class UploadGameViewModel : ViewModelBase
{
    [ObservableProperty] private List<string> _addedFilesDirs = new List<string>();

    [ObservableProperty] private string? _inputFileDir;

    [ObservableProperty] private string _gameName;

    [RelayCommand]
    private void AddFileDir()
    {
        if (InputFileDir == null) return;
        
        List<string> newList = AddedFilesDirs.ToList();
        newList.Add(InputFileDir);
        AddedFilesDirs = newList;
    }

    [RelayCommand]
    private void GoToGameLibrary()
    {
        MainWindowViewModel.SwitchViews(new GameLibraryViewModel()
        {
            MainWindowViewModel = this.MainWindowViewModel
        });
    }

    [RelayCommand]
    private async Task UploadGame()
    {
        GameApiClient gameApiClient = new GameApiClient();
        await gameApiClient.UploadGameAsync(GameName, AddedFilesDirs);
        
        GoToGameLibrary();
    }
}