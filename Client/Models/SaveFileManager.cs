using System;
using System.Collections.Generic;
using System.IO;

namespace Client.Models;

public class SaveFileManager
{
    private const string ROAMING_APPDATA_PATH = "drive_c/users/steamuser/AppData/Roaming";
    private readonly List<string> ROAMING_APPDATA_EXCLUDED_FOLDERS = 
    [
        "Microsoft"
    ];
    
    private const string LOCAL_APPDATA_PATH = "drive_c/users/steamuser/AppData/Local";
    private readonly List<string> LOCAL_APPDATA_EXCLUDED_FOLDERS = 
    [
        "Temp",
        "Microsoft"
    ];
    
    private const string DOCUMENTS_PATH = "drive_c/users/steamuser/Documents";
    private readonly List<string> DOCUMENTS_EXCLUDED_FOLDERS = 
    [
        "Downloads",
        "Music",
        "Pictures",
        "Templates",
        "Videos"
    ];

    public List<string> FindSaveFiles(string prefixPath)
    {
        List<string> newSaveFiles = new List<string>();
        
        string prefixLocalAppdataPath = Path.Combine(prefixPath, LOCAL_APPDATA_PATH);
        newSaveFiles.AddRange(FindFilesInPath(prefixLocalAppdataPath, LOCAL_APPDATA_EXCLUDED_FOLDERS));
        
        string prefixRoamingAppdataPath = Path.Combine(prefixPath, ROAMING_APPDATA_PATH);
        newSaveFiles.AddRange(FindFilesInPath(prefixRoamingAppdataPath, ROAMING_APPDATA_EXCLUDED_FOLDERS));
        
        string prefixDocumentsPath = Path.Combine(prefixPath, DOCUMENTS_PATH);
        newSaveFiles.AddRange(FindFilesInPath(prefixDocumentsPath, DOCUMENTS_EXCLUDED_FOLDERS));

        return newSaveFiles;

    }

    private List<string> FindFilesInPath(string path, List<string> excludedFolders)
    {
        List<string> foundFiles = new List<string>();
        
        const string ALL_FILES = "*";
        foreach (string dir in Directory.GetDirectories(path))
        {
            // Though the method is get file name, it returns the last element of the path
            string lastFolderName = Path.GetFileName(dir);
            if (excludedFolders.Contains(lastFolderName)) continue;

            foundFiles.AddRange(Directory.GetFiles(dir, ALL_FILES, SearchOption.AllDirectories));
        }

        return foundFiles;
    }
}