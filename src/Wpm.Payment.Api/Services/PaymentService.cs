namespace Wpm.Payment.Api.Services
{
    public interface IPaymentService
    {
        Task<bool> ProcessPayment(int patientId);
    }

    public class PaymentService: IPaymentService
    {

        public async Task<bool> ProcessPayment(int patientId)
        {
            return false;
        }
    }
}
