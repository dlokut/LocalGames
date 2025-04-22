using System.Threading.Tasks;
using Client.Models.ServerApi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class RegisterViewModel : ViewModelBase
{
    [ObservableProperty] private string _serverAddress;
    
    [ObservableProperty] private string _username;
    
    [ObservableProperty] private string _password;

    [ObservableProperty] private string _errorText;

    [RelayCommand]
    public void GoBackToWelcome()
    {
        MainWindowViewModel.SwitchViews(new WelcomeViewModel());
    }

    [RelayCommand]
    public async Task Register()
    {
        LoginApiClient loginApiClient = new LoginApiClient();
        bool registerSuccess = await loginApiClient.RegisterAsync(ServerAddress, Username, Password);

        if (registerSuccess) ErrorText = "";
        else ErrorText = "Register error";
    }
    

}
