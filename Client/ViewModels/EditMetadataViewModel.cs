using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Client.Models.ServerApi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class EditMetadataViewModel : ViewModelBase
{
    [ObservableProperty] private Bitmap? _coverPreviewBitmap;

    [ObservableProperty] private string _coverPreviewSource;

    [ObservableProperty] private ServerGame _serverGame;

    [ObservableProperty] private string _gameTitle;

    public EditMetadataViewModel(ServerGame serverGame)
    {
        _serverGame = serverGame;
        CoverPreviewSource = serverGame.CoverUrl;
        
        SetGameTitle(serverGame.Name);
    }

    [RelayCommand]
    private async Task UploadMetadata()
    {
        GameApiClient gameApiClient = new GameApiClient();
        await gameApiClient.UploadMetadataAsync(ServerGame);
    }
    
    partial void OnCoverPreviewSourceChanged(string? value)
    {
        if (value == null) return;

        _ = LoadCoverPreview();
    }

    private async Task LoadCoverPreview()
    {
        using HttpClient httpClient = new HttpClient();
        byte[] coverPreviewBytes = await httpClient.GetByteArrayAsync(CoverPreviewSource);

        CoverPreviewBitmap = new Bitmap(new MemoryStream(coverPreviewBytes));
    }

    private void SetGameTitle(string gameName)
    {
        GameTitle = $"{gameName} - Edit metadata";
    }
}