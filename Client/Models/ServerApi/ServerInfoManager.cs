using System.IO;
using System.Threading.Tasks;

namespace Client.Models.ServerApi;

public class ServerInfoManager
{
    private const string ADDRESS_FILE_NAME = "ServerAddress.txt";

    private const string COOKIES_FILE_NAME = ".cookies";
    
    public async Task SaveServerAddressAsync(string address)
    {
        string saveFilePath = Path.Combine(Directory.GetCurrentDirectory(), ADDRESS_FILE_NAME);

        await using StreamWriter saveFileWriter = File.CreateText(saveFilePath);
        await saveFileWriter.WriteAsync(address);
    }

    public async Task<string> LoadServerAddressAsync()
    {
        string saveFilePath = Path.Combine(Directory.GetCurrentDirectory(), ADDRESS_FILE_NAME);

        using StreamReader saveFileReader = File.OpenText(ADDRESS_FILE_NAME);
        return await saveFileReader.ReadToEndAsync();
    }

    public async Task SaveLoginCookieAsync(string cookie)
    {
        string saveFilePath = Path.Combine(Directory.GetCurrentDirectory(), COOKIES_FILE_NAME);
     
        await using StreamWriter saveFileWriter = File.CreateText(saveFilePath);
        await saveFileWriter.WriteAsync(cookie);   
    }
}