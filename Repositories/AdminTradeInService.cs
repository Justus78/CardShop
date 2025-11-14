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
            var query = _context.TradeIns.Include(t => t.TradeInItems).Include(t => t.User).AsQueryable();

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
                UserEmail = tradeIn.User.Email!,
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

            tradeIn.Status = status;
            tradeIn.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateItemFinalValueAsync(int tradeInItemId, decimal finalValue)
        {
            var item = await _context.TradeInItems.FindAsync(tradeInItemId);
            if (item == null) return false;

            item.FinalUnitValue = finalValue;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SubmitFinalOfferAsync(int tradeInId)
        {
            var tradeIn = await _context.TradeIns.FindAsync(tradeInId);
            if (tradeIn == null) return false;

            tradeIn.Status = TradeInStatus.OfferSent;
            tradeIn.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreditUserAccountAsync(int tradeInId)
        {
            var tradeIn = await _context.TradeIns
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == tradeInId);

            if (tradeIn == null || tradeIn.FinalValue == null) return false;

            var storeCredit = await _context.StoreCredits
                .FirstOrDefaultAsync(s => s.UserId == tradeIn.UserId);

            if (storeCredit == null)
            {
                storeCredit = new StoreCredit
                {
                    UserId = tradeIn.UserId,
                    CurrentBalance = tradeIn.FinalValue.Value,
                    CreatedAt = DateTime.UtcNow,
                    SourceId = tradeIn.Id
                };
                _context.StoreCredits.Add(storeCredit);
            }
            else
            {
                storeCredit.CurrentBalance += tradeIn.FinalValue.Value;
            }

            _context.StoreCreditTransactions.Add(new StoreCreditTransaction
            {
                StoreCreditId = storeCredit.Id,
                ChangeAmount = tradeIn.FinalValue.Value,
                NewBalance = storeCredit.CurrentBalance,
                Reason = StoreCreditSource.TradeIn
            });

            tradeIn.Status = TradeInStatus.Credited;
            tradeIn.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReturnCardsToUserAsync(int tradeInId)
        {
            var tradeIn = await _context.TradeIns.FindAsync(tradeInId);
            if (tradeIn == null) return false;

            tradeIn.Status = TradeInStatus.Returned;
            tradeIn.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
