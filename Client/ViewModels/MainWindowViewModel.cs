using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Client.Database;
using Client.Models;
using Client.Models.ServerApi;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Welcome to !!!";

    [RelayCommand]
    private async Task ButtonClicked()
    {
        LoginApiClient loginApiClient = new LoginApiClient();

        string password = "Password1£";
        //var test = await loginApiClient.LoginAsync("http://localhost:5062", "User2", password);

        ServerInfoManager serverInfoManager = new ServerInfoManager();
        //Console.WriteLine(await serverInfoManager.LoadLoginCookieAsync());
        //Console.WriteLine(await serverInfoManager.LoadServerAddressAsync());

        //await serverInfoManager.GetClientWithLoginCookieAsync();
        GameApiClient gameApiClient = new GameApiClient();
        //await gameApiClient.GetAllGamesOnServer();
        //await gameApiClient.GetGameFileInfoAsync(new Guid("573B7415-D9A9-4A0B-8B7C-B6399EF4902D"));
        //await gameApiClient.DownloadGameFileAsync(new Guid("81126CAA-F59C-4FFD-A2C3-4BBCF9E9578F"),

        List<ServerGame> games = await gameApiClient.GetAllGamesOnServer();
        //await gameApiClient.UninstallGameAsync(games.First().Id);
        //await gameApiClient.DownloadGameAsync(games.First());

        ProtonManager protonManager = new ProtonManager();
        //await protonManager.AddProtonEnvVariableAsync(games.First().Id, "test", "testvalue");
        //await protonManager.AddProtonEnvVariableAsync(games.First().Id, "test2", "testvalue2");
        //await protonManager.RemoveProtonEnvVariableAsync(games.First().Id, "test");

        //string test = await protonManager.CreateEnvVariableStringAsync(games.First().Id);

        /*
        using (ClientDbContext dbContext = new ClientDbContext())
        {
            ProtonSettings settings = await dbContext.ProtonSettings.FindAsync(games.First().Id);
            settings.MangohudEnabled = true;
            settings.GamemodeEnabled = false;
            protonManager.CreateWrapperCommandsString(settings);
        }
        */

        //await protonManager.CanLaunchGameAsync(games.First().Id);
        //await protonManager.SetPrimaryExecutible(games.First().Id, "Yakuza 7/data/cat.xcf");

        await protonManager.LaunchGame(games.First().Id);
    }

    [RelayCommand]
    private void SecondButtonClicked()
    {
        Greeting = new Random().Next().ToString();
    }
}