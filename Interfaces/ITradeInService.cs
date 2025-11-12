using api.DTOs.TradeIn;

namespace api.Interfaces
{
    public interface ITradeInService
    {
        Task<TradeInDto?> SubmitTradeInAsync(string userId, TradeInCreateDto dto);
        Task<IEnumerable<TradeInSummaryDto>> GetUserTradeInsAsync(string userId);
        Task<TradeInDetailDto?> GetTradeInByIdAsync(string userId, int tradeInId);
        Task<bool> CancelTradeInAsync(string userId, int tradeInId);
        Task<bool> ConfirmFinalOfferAsync(string userId, int tradeInId);
        Task<bool> DeclineFinalOfferAsync(string userId, int tradeInId);
        Task<decimal> GetEstimatedTradeValueAsync(List<TradeInItemCreateDto> items);
    }
}
