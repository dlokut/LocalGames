using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class RegisterViewModel : ViewModelBase
{
    [ObservableProperty] private string _serverAddress;
    
    [ObservableProperty] private string _username;
    
    [ObservableProperty] private string _password;

    [RelayCommand]
    public void GoBackToWelcome()
    {
        MainWindowViewModel.SwitchViews(new WelcomeViewModel());
    }

}
