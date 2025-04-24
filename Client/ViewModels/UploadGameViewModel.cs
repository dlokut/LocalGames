using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class UploadGameViewModel : ViewModelBase
{
    [ObservableProperty] private List<string> _addedFilesDirs = new List<string>();

    [ObservableProperty] private string? _inputFileDir;

    [RelayCommand]
    private void AddFileDir()
    {
        List<string> newList = AddedFilesDirs.ToList();
        newList.Add(InputFileDir);
        AddedFilesDirs = newList;
    }
}