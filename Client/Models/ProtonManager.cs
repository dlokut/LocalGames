using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Client.Models;

public class ProtonManager
{
   private const string PROTON_VERSIONS_DIR = "Proton";

   public List<string> GetProtonVersions()
   {
      if (!Directory.Exists(PROTON_VERSIONS_DIR))
      {
         Directory.CreateDirectory(PROTON_VERSIONS_DIR);
      }

      List<string> protonVersions = Directory.GetDirectories(PROTON_VERSIONS_DIR).ToList();

      return protonVersions;
   }
}