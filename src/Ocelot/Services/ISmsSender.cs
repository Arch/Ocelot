using System.Threading.Tasks;

namespace Ocelot.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string number, string message);
    }
}
