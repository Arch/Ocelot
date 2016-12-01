using System.Collections.Generic;

namespace Ocelot.Configuration
{
    public class OcelotConfiguration : IOcelotConfiguration
    {
        public OcelotConfiguration(List<ReRoute> reRoutes, AdminstrationSettings adminstrationSettings)
        {
            ReRoutes = reRoutes;
            AdminstrationSettings = adminstrationSettings;
        }

        public List<ReRoute> ReRoutes { get; }
        public AdminstrationSettings AdminstrationSettings { get;}
    }
}