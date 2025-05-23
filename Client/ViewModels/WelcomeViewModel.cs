using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    [RelayCommand]
    public void GoToRegister()
    {
        MainWindowViewModel.SwitchViews(new RegisterViewModel());
    }

    [RelayCommand]
    public void GoToLogin()
    {
        MainWindowViewModel.SwitchViews(new LoginViewModel());
    }
}