using System.IO;
using System.Threading.Tasks;

namespace Client.Models.ServerApi;

public class ServerAddressManager
{
    private const string SAVE_FILE_NAME = "ServerAddress.txt";
    
    public async Task SaveServerAddress(string address)
    {
        string saveFilePath = Path.Combine(Directory.GetCurrentDirectory(), SAVE_FILE_NAME);

        await using StreamWriter saveFileWriter = File.CreateText(saveFilePath);
        await saveFileWriter.WriteAsync(address);
    }

    public async Task<string> LoadServerAddress()
    {
        string saveFilePath = Path.Combine(Directory.GetCurrentDirectory(), SAVE_FILE_NAME);

        using StreamReader saveFileReader = File.OpenText(SAVE_FILE_NAME);
        return await saveFileReader.ReadToEndAsync();
    }
}