using CommunityToolkit.Mvvm.ComponentModel;

namespace Client.ViewModels;

public partial class GameLibraryContentViewModel : ViewModelBase
{
    [ObservableProperty] private string _fillerText =
        "Game summary | Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod  tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim  veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea  commodo consequat. Duis aute irure dolor in reprehenderit in voluptate  velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint  occaecat cupidatat non proident, sunt in culpa qui officia deserunt  mollit anim id est laborum";

    [ObservableProperty] private string _gameName;

    [ObservableProperty] private string _gameSummary;

    public GameLibraryContentViewModel(string gameName, string gameSummary)
    {
        GameName = gameName;
        GameSummary = gameSummary;
    }
}