using api.DTOs.TradeIn;
using api.Models;

namespace api.Interfaces
{
    public interface IAdminTradeInService
    {
        Task<IEnumerable<TradeInAdminSummaryDto>> GetAllTradeInsAsync(string? statusFilter = null);
        Task<TradeInDetailsDto?> GetTradeInDetailsAsync(int tradeInId);
        Task<bool> UpdateTradeInStatusAsync(int tradeInId, TradeInStatus status);
        Task<bool> UpdateItemFinalValueAsync(int tradeInItemId, decimal finalValue);
        Task<bool> SubmitFinalOfferAsync(int tradeInId);
        Task<bool> CreditUserAccountAsync(int tradeInId);
        Task<bool> ReturnCardsToUserAsync(int tradeInId);
    }
}
