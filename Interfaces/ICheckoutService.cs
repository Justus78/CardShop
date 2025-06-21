using api.DTOs.Order;
using api.DTOs.Stripe;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace api.Interfaces
{
    public interface ICheckoutService
    {
        Task<StripePaymentResultDto> CreatePaymentAndOrderAsync(CreateOrderDto createOrderDto, string userId);

    }
}
