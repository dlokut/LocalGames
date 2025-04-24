using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Client.Models.ServerApi;

public class ServerInfoManager
{
    private const string ADDRESS_FILE_NAME = "ServerAddress.txt";

    private const string COOKIES_FILE_NAME = ".cookies";
    
    public readonly JsonSerializerOptions sharedJsonOptions = new JsonSerializerOptions()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public bool AlreadySignedIn()
    {
        string cookiesFilePath = Path.Combine(Directory.GetCurrentDirectory(), COOKIES_FILE_NAME);
        return File.Exists(cookiesFilePath);
    }
    public async Task SaveServerAddressAsync(string address)
    {
        string saveFilePath = Path.Combine(Directory.GetCurrentDirectory(), ADDRESS_FILE_NAME);

        await using StreamWriter saveFileWriter = File.CreateText(saveFilePath);
        await saveFileWriter.WriteAsync(address);
    }

    public async Task<string> LoadServerAddressAsync()
    {
        string saveFilePath = Path.Combine(Directory.GetCurrentDirectory(), ADDRESS_FILE_NAME);

        using StreamReader saveFileReader = File.OpenText(saveFilePath);
        return await saveFileReader.ReadToEndAsync();
    }

    public async Task SaveLoginCookieAsync(string cookie)
    {
        string saveFilePath = Path.Combine(Directory.GetCurrentDirectory(), COOKIES_FILE_NAME);
     
        await using StreamWriter saveFileWriter = File.CreateText(saveFilePath);
        await saveFileWriter.WriteAsync(cookie);   
    }

    public async Task<HttpClient> GetClientWithLoginCookieAsync()
    {
        string cookie = await LoadLoginCookieAsync();
        string cookieName = cookie.Substring(0, cookie.IndexOf("="));
        string cookieValue = cookie.Substring(cookie.IndexOf("=") + 1);
        
        string serverAddress = await LoadServerAddressAsync();
        
        CookieContainer cookieContainer = new CookieContainer();
        cookieContainer.Add(new Uri(serverAddress), new Cookie(cookieName, cookieValue));
        
        HttpClientHandler httpClientHandler = new HttpClientHandler()
        {
            CookieContainer = cookieContainer
        };
        HttpClient httpClient = new HttpClient(httpClientHandler)
        {
            BaseAddress = new Uri(serverAddress),
        };

        return httpClient;
    }
    private async Task<string> LoadLoginCookieAsync()
    {
        string saveFilePath = Path.Combine(Directory.GetCurrentDirectory(), COOKIES_FILE_NAME);
     
        using StreamReader saveFileReader = File.OpenText(saveFilePath);
        return await saveFileReader.ReadToEndAsync();   
    }
}