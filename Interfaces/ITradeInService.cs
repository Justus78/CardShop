using api.DTOs.TradeIn;

namespace api.Interfaces
{
    public interface ITradeInService
    {
        // USER: Create a trade-in with multiple items
        Task<TradeInDto?> SubmitTradeInAsync(string userId, TradeInCreateDto dto);

        // USER: Get all trade-ins for a user
        Task<IEnumerable<TradeInSummaryDto>> GetUserTradeInsAsync(string userId);

        // USER: Get a single trade-in with its items
        Task<TradeInDetailDto?> GetTradeInByIdAsync(string userId, int tradeInId);

        // USER: Cancel while still in SUBMITTED or REVIEWING
        Task<bool> CancelTradeInAsync(string userId, int tradeInId);

        // USER: Confirm the final offer
        Task<bool> ConfirmFinalOfferAsync(string userId, int tradeInId);

        // USER: Decline the final offer
        Task<bool> DeclineFinalOfferAsync(string userId, int tradeInId);

        // USER: Estimate values before submitting (optional)
        Task<decimal> GetEstimatedTradeValueAsync(List<TradeInItemCreateDto> items);


        // ──────────────────────────────────────────────
        // INDIVIDUAL ITEM MANAGEMENT
        // ──────────────────────────────────────────────

        // USER: Add a single item to an existing trade-in
        Task<TradeInItemDto?> AddItemAsync(string userId, int tradeInId, TradeInItemCreateDto itemDto);

        // USER: Remove a single item from an existing trade-in
        Task<bool> RemoveItemAsync(string userId, int tradeInId, int itemId);

        // USER: Update item (qty, condition)
        Task<TradeInItemDto?> UpdateItemAsync(string userId, int tradeInId, int itemId, TradeInItemCreateDto itemDto);


        // ──────────────────────────────────────────────
        // ADMIN ACTIONS
        // ──────────────────────────────────────────────

        // ADMIN: Set the final per-unit value for an item
        Task<bool> UpdateFinalItemValueAsync(int tradeInItemId, UpdateTradeInItemValueDto dto);

        // ADMIN: Update Trade-In status
        Task<bool> UpdateTradeInStatusAsync(int tradeInId, UpdateTradeInStatusDto dto);

        // ADMIN: Get all trade-ins (optional filters later)
        Task<IEnumerable<TradeInAdminSummaryDto>> GetAllTradeInsAsync();

        // ADMIN: Get full details
        Task<TradeInDetailsDto?> GetAdminTradeInByIdAsync(int tradeInId);
    }
}
