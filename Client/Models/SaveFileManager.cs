using System;
using System.Collections.Generic;
using System.IO;

namespace Client.Models;

public class SaveFileManager
{
    public const string ROAMING_APPDATA_PATH = "drive_c/users/steamuser/AppData/Roaming";
    
    public const string LOCAL_APPDATA_PATH = "drive_c/users/steamuser/AppData/Local";
    
    public const string DOCUMENT_PATH = "drive_c/users/steamuser/Documents";

    public List<string> FindNewFiles(string path, List<string> excludedFolders, List<string> alreadyFoundFiles)
    {
        List<string> foundFiles = new List<string>();
        
        const string ALL_FILES = "*";
        foreach (string dir in Directory.GetDirectories(path))
        {
            // Though the method is get file name, it returns the last element of the path
            string lastFolderName = Path.GetFileName(dir);
            if (excludedFolders.Contains(lastFolderName)) continue;

            foreach (string filePath in Directory.GetFiles(dir, ALL_FILES,
                         SearchOption.AllDirectories))
            {
                if (!alreadyFoundFiles.Contains(filePath)) foundFiles.Add(filePath);
            }
        }

        return foundFiles;
    }
}