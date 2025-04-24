using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class EditMetadataViewModel : ViewModelBase
{
    [ObservableProperty] private Bitmap? _coverPreviewBitmap;

    [ObservableProperty] private string _coverPreviewSource;

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
}