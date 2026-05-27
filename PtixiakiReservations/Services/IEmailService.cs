using System.Threading.Tasks;

namespace PtixiakiReservations.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}