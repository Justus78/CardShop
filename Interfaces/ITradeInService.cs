using api.DTOs.TradeIn;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace api.Interfaces
{
    public interface ITradeInService
    {
        // ──────────────────────────────────────────────
        // USER: Trade-In CRUD
        // ──────────────────────────────────────────────

        Task<TradeInDto?> SubmitTradeInAsync(string userId, TradeInCreateDto dto);
        Task<IEnumerable<TradeInSummaryDto>> GetUserTradeInsAsync(string userId);
        Task<TradeInDetailDto?> GetTradeInByIdAsync(string userId, int tradeInId);
        Task<bool> CancelTradeInAsync(string userId, int tradeInId);
        Task<bool> ConfirmFinalOfferAsync(string userId, int tradeInId);
        Task<bool> DeclineFinalOfferAsync(string userId, int tradeInId);
        Task<decimal> GetEstimatedTradeValueAsync(List<TradeInItemCreateDto> items);

        // ──────────────────────────────────────────────
        // USER: Draft / Persistent trade-ins
        // ──────────────────────────────────────────────

        Task<TradeInDetailDto> GetOrCreateDraftAsync(string userId);
        Task<TradeInDetailDto> AddItemToDraftAsync(string userId, TradeInItemCreateDto dto);
        Task<bool> RemoveItemFromDraftAsync(string userId, int itemId);
        Task<TradeInDto?> SubmitDraftAsync(string userId);

        // ──────────────────────────────────────────────
        // USER: Item management (for submitted trade-ins)
        // ──────────────────────────────────────────────

        Task<TradeInItemDto?> AddItemAsync(string userId, int tradeInId, TradeInItemCreateDto itemDto);
        Task<bool> RemoveItemAsync(string userId, int tradeInId, int itemId);
        Task<TradeInItemDto?> UpdateItemAsync(string userId, int tradeInId, int itemId, TradeInItemCreateDto itemDto);

        // ──────────────────────────────────────────────
        // ADMIN ACTIONS
        // ──────────────────────────────────────────────

        Task<bool> UpdateFinalItemValueAsync(int tradeInItemId, UpdateTradeInItemValueDto dto);
        Task<bool> UpdateTradeInStatusAsync(int tradeInId, UpdateTradeInStatusDto dto);
        Task<IEnumerable<TradeInAdminSummaryDto>> GetAllTradeInsAsync();
        Task<TradeInDetailsDto?> GetAdminTradeInByIdAsync(int tradeInId);
    }
}
