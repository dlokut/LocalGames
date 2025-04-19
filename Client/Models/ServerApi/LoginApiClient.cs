using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace Client.Models.ServerApi;

public class LoginApiClient
{
    private string LOGIN_ENDPOINT = "User/v1/PostLogin";
    public async Task<bool> LoginAsync(string serverAddress, string username, string password)
    {
        CookieContainer cookieContainer = new CookieContainer();
        HttpClientHandler httpClientHandler = new HttpClientHandler()
        {
            CookieContainer = cookieContainer
        };
        
        HttpClient httpClient = new HttpClient(httpClientHandler)
        {
            BaseAddress = new Uri(serverAddress),
        };
        

        string endpoint = $"{LOGIN_ENDPOINT}?username={username}&password={password}";
        using HttpResponseMessage response = await httpClient.PostAsync(endpoint, null);
        
        if (!response.IsSuccessStatusCode) return false;
        
        ServerInfoManager serverInfoManager = new ServerInfoManager();
        await serverInfoManager.SaveServerAddressAsync(serverAddress);
        
        Cookie loginCookie = cookieContainer.GetCookies(new Uri(serverAddress)).First();
        
        await serverInfoManager.SaveLoginCookieAsync(loginCookie.ToString());
            
        return true;
    }
    
        private string REGISTER_ENDPOINT = "User/v1/PostRegister";
        public async Task<bool> RegisterAsync(string serverAddress, string username, string password)
        {
            HttpClient httpClient = new HttpClient()
            {
                BaseAddress = new Uri(serverAddress)
            };
    
            string endpoint = $"{REGISTER_ENDPOINT}?username={username}&password={password}";
            using HttpResponseMessage response = await httpClient.PostAsync(endpoint, null);

            if (!response.IsSuccessStatusCode) return false;
            
            ServerInfoManager serverInfoManager = new ServerInfoManager();
            await serverInfoManager.SaveServerAddressAsync(serverAddress);
            
            return true;
        }
}