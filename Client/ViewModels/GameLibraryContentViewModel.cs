using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Client.ViewModels;

public partial class GameLibraryContentViewModel : ViewModelBase
{
    [ObservableProperty] private string _fillerText =
        "Game summary | Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod  tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim  veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea  commodo consequat. Duis aute irure dolor in reprehenderit in voluptate  velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint  occaecat cupidatat non proident, sunt in culpa qui officia deserunt  mollit anim id est laborum";

    [ObservableProperty] private string _gameName;

    [ObservableProperty] private string _gameSummary;

    [ObservableProperty] private Bitmap _coverBitmap;

    public GameLibraryContentViewModel(string gameName, string gameSummary, string coverUrl)
    {
        GameName = gameName;
        GameSummary = gameSummary;

        _ = LoadCover(coverUrl);
    }

    private async Task LoadCover(string url)
    {
        CoverBitmap = await LoadBitmapFromUrl(url);
    }

    private async Task<Bitmap> LoadBitmapFromUrl(string url)
    {
        using HttpClient httpClient = new HttpClient();
        byte[] imageBytes = await httpClient.GetByteArrayAsync(url);

        return new Bitmap(new MemoryStream(imageBytes));
    }
}