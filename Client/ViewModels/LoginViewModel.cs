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
        }

}