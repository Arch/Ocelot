using System.Threading.Tasks;

namespace Ocelot.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
