using System.Threading.Tasks;
using Client.Models.ServerApi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class LoginViewModel : ViewModelBase
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
        public async Task Login()
        {
            LoginApiClient loginApiClient = new LoginApiClient();
            bool loginSuccess = await loginApiClient.LoginAsync(ServerAddress, Username, Password);

            if (loginSuccess) ErrorText = "";
            else ErrorText = "Login error";
        }

}