using System.Collections.Generic;

namespace Ocelot.Configuration.File
{
    public class FileConfiguration
    {
        public FileConfiguration()
        {
            ReRoutes = new List<FileReRoute>();
            GlobalConfiguration = new FileGlobalConfiguration();
            AdministrationSettings = new FileAdministrationSettings();
        }

        public List<FileReRoute> ReRoutes { get; set; }
        public FileGlobalConfiguration GlobalConfiguration { get; set; }
        public FileAdministrationSettings AdministrationSettings { get; set; }
    }
}
