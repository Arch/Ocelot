namespace Ocelot.Configuration
{
    public class AdminstrationSettings
    {
        public AdminstrationSettings(string adminPagePath)
        {
            AdminPagePath = adminPagePath;
        }

        public string AdminPagePath { get; private set; }
    }
}