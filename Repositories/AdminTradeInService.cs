using api.DTOs.TradeIn;
using api.Interfaces;
using api.Models;
using CardShop.Data;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories
{
    public class AdminTradeInService : IAdminTradeInService
    {
        private readonly ApplicationDbContext _context;

        public AdminTradeInService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TradeInAdminSummaryDto>> GetAllTradeInsAsync(string? statusFilter = null)
        {
            var query = _context.TradeIns
                .Include(t => t.TradeInItems)
                .Include(t => t.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter) &&
                Enum.TryParse<TradeInStatus>(statusFilter, true, out var filter))
            {
                query = query.Where(t => t.Status == filter);
            }

            return await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TradeInAdminSummaryDto
                {
                    Id = t.Id,
                    UserEmail = t.User.Email!,
                    Status = t.Status,
                    EstimatedValue = t.EstimatedValue,
                    FinalValue = t.FinalValue,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<TradeInDetailsDto?> GetTradeInDetailsAsync(int tradeInId)
        {
            var tradeIn = await _context.TradeIns
                .Include(t => t.TradeInItems)
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == tradeInId);

            if (tradeIn == null) return null;

            return new TradeInDetailsDto
            {
                Id = tradeIn.Id,
                UserEmail = tradeIn.User?.Email ?? string.Empty,
                Status = tradeIn.Status,
                EstimatedValue = tradeIn.EstimatedValue,
                FinalValue = tradeIn.FinalValue,
                CreatedAt = tradeIn.CreatedAt,
                Items = tradeIn.TradeInItems.Select(i => new TradeInItemDto
                {
                    Id = i.Id,
                    CardName = i.CardName!,
                    SetCode = i.SetCode!,
                    Condition = i.Condition.ToString(),
                    Quantity = i.Quantity,
                    EstimatedUnitValue = i.EstimatedUnitValue,
                    FinalUnitValue = i.FinalUnitValue
                }).ToList()
            };
        }

        public async Task<bool> UpdateTradeInStatusAsync(int tradeInId, TradeInStatus status)
        {
            var tradeIn = await _context.TradeIns.FindAsync(tradeInId);
            if (tradeIn == null) return false;

            // Validate allowed transition
            if (tradeIn.Status == status) return true; // no-op
          
            tradeIn.Status = status;
            tradeIn.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateItemFinalValueAsync(int tradeInItemId, decimal finalValue)
        {
            if (finalValue < 0) return false;

            var item = await _context.TradeInItems
                .Include(i => i.TradeIn)
                .FirstOrDefaultAsync(i => i.Id == tradeInItemId);

            if (item == null) return false;

            // Update the item's final unit value
            item.FinalUnitValue = finalValue;
            item.TradeIn.UpdatedAt = DateTime.UtcNow;

            // Optionally recalc the TradeIn.FinalValue? We'll leave that to SubmitFinalOfferAsync,
            // but we keep UpdatedAt for auditability.
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SubmitFinalOfferAsync(int tradeInId)
        {
            var tradeIn = await _context.TradeIns
                .Include(t => t.TradeInItems)
                .FirstOrDefaultAsync(t => t.Id == tradeInId);

            if (tradeIn == null) return false;

            // Make sure every item has a final value before submitting
            if (tradeIn.TradeInItems.Any(i => i.FinalUnitValue == null))
                return false; // Admin must set final prices first

            // Calculate total final offer
            decimal finalOffer = tradeIn.TradeInItems.Sum(i =>
                i.FinalUnitValue * i.Quantity
            );

            tradeIn.FinalValue = finalOffer;
            tradeIn.Status = TradeInStatus.OfferSent;
            tradeIn.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> CreditUserAccountAsync(int tradeInId)
        {
            // Only credit if tradeIn exists and has a final value and hasn't been credited already
            var tradeIn = await _context.TradeIns
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == tradeInId);

            if (tradeIn == null) return false;
            if (tradeIn.FinalValue == null) return false;
            if (tradeIn.Status == TradeInStatus.Credited) return false; // prevent double-credit

            // Use a transaction to ensure storeCredit + transaction + tradeIn status are atomic
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Find or create the user's StoreCredit
                var storeCredit = await _context.StoreCredits
                    .FirstOrDefaultAsync(sc => sc.UserId == tradeIn.UserId);

                if (storeCredit == null)
                {
                    storeCredit = new StoreCredit
                    {
                        UserId = tradeIn.UserId,
                        CurrentBalance = 0m,
                        CreatedAt = DateTime.UtcNow,
                        SourceId = tradeIn.Id
                    };
                    _context.StoreCredits.Add(storeCredit);
                    await _context.SaveChangesAsync(); // ensure storeCredit.Id is populated
                }

                // Add amount
                storeCredit.CurrentBalance += tradeIn.FinalValue.Value;

                // Add transaction log (storeCredit is tracked so Id is available)
                var trx = new StoreCreditTransaction
                {
                    StoreCreditId = storeCredit.Id,
                    ChangeAmount = tradeIn.FinalValue.Value,
                    NewBalance = storeCredit.CurrentBalance,
                    Reason = StoreCreditSource.TradeIn,
                    CreatedAt = DateTime.UtcNow
                };
                _context.StoreCreditTransactions.Add(trx);

                // Update tradeIn
                tradeIn.Status = TradeInStatus.Credited;
                tradeIn.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw; // bubble up — controller can map this to 500
            }
        }

        public async Task<bool> ReturnCardsToUserAsync(int tradeInId)
        {
            var tradeIn = await _context.TradeIns.FindAsync(tradeInId);
            if (tradeIn == null) return false;

            // Only allow return if not already returned or credited
            if (tradeIn.Status == TradeInStatus.Returned || tradeIn.Status == TradeInStatus.Credited)
                return false;

            tradeIn.Status = TradeInStatus.Returned;
            tradeIn.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
