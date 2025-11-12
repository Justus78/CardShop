using api.Data;
using api.DTOs.TradeIn;
using api.Interfaces;
using api.Models;
using CardShop.Data;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class TradeInService : ITradeInService
    {
        private readonly ApplicationDbContext _context;

        public TradeInService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TradeInDto?> SubmitTradeInAsync(string userId, TradeInCreateDto dto)
        {
            var tradeIn = new TradeIn
            {
                UserId = userId,
                Status = TradeInStatus.Submitted,
                EstimatedValue = 0M,
                CreatedAt = DateTime.Now,
                TradeInItems = dto.Items.Select(i => new TradeInItem
                {
                    CardName = i.CardName,
                    SetCode = i.SetCode,
                    Condition = Enum.Parse<CardCondition>(i.Condition),
                    Quantity = i.Quantity,
                    EstimatedUnitValue = 0M
                }).ToList()
            };

            tradeIn.EstimatedValue = await GetEstimatedTradeValueAsync(dto.Items);

            _context.TradeIns.Add(tradeIn);
            await _context.SaveChangesAsync();

            return new TradeInDto
            {
                Id = tradeIn.Id,
                EstimatedValue = tradeIn.EstimatedValue,
                Status = tradeIn.Status,
                CreatedAt = tradeIn.CreatedAt
            };
        }

        public async Task<IEnumerable<TradeInSummaryDto>> GetUserTradeInsAsync(string userId)
        {
            return await _context.TradeIns
                .Where(t => t.UserId == userId)
                .Select(t => new TradeInSummaryDto
                {
                    Id = t.Id,
                    EstimatedTotalValue = t.EstimatedValue,
                    Status = t.Status,
                    SubmittedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<TradeInDetailDto?> GetTradeInByIdAsync(string userId, int tradeInId)
        {
            var tradeIn = await _context.TradeIns
                .Include(t => t.TradeInItems)
                .FirstOrDefaultAsync(t => t.Id == tradeInId && t.UserId == userId);

            if (tradeIn == null) return null;

            return new TradeInDetailDto
            {
                Id = tradeIn.Id,
                Status = tradeIn.Status,
                EstimatedTotalValue = tradeIn.EstimatedValue,
                FinalValue = tradeIn.FinalValue,
                CreatedAt = tradeIn.CreatedAt,
                Items = tradeIn.TradeInItems.Select(i => new TradeInItemDto
                {
                    CardName = i.CardName,
                    SetCode = i.SetCode,
                    Condition = i.Condition.ToString(),
                    Quantity = i.Quantity,
                    EstimatedPrice = i.EstimatedUnitValue,
                    FinalPrice = i.FinalPrice
                }).ToList()
            };
        }

        public async Task<bool> CancelTradeInAsync(string userId, int tradeInId)
        {
            var tradeIn = await _context.TradeIns
                .FirstOrDefaultAsync(t => t.Id == tradeInId && t.UserId == userId);

            if (tradeIn == null || tradeIn.Status != TradeInStatus.Submitted)
                return false;

            tradeIn.Status = TradeInStatus.Returned;
            tradeIn.UpdatedAt = DateTime.Now;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ConfirmFinalOfferAsync(string userId, int tradeInId)
        {
            var tradeIn = await _context.TradeIns
                .FirstOrDefaultAsync(t => t.Id == tradeInId && t.UserId == userId);

            if (tradeIn == null || tradeIn.Status != TradeInStatus.FinalOfferSent)
                return false;

            tradeIn.Status = TradeInStatus.Approved;
            tradeIn.UpdatedAt = DateTime.Now;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeclineFinalOfferAsync(string userId, int tradeInId)
        {
            var tradeIn = await _context.TradeIns
                .FirstOrDefaultAsync(t => t.Id == tradeInId && t.UserId == userId);

            if (tradeIn == null || tradeIn.Status != TradeInStatus.FinalOfferSent)
                return false;

            tradeIn.Status = TradeInStatus.Declined;
            tradeIn.UpdatedAt = DateTime.Now;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<decimal> GetEstimatedTradeValueAsync(List<CreateTradeInItemDto> items)
        {
            // TODO: integrate Scryfall pricing here
            decimal total = 0M;

            foreach (var item in items)
            {
                decimal price = 0.25M; // placeholder default
                total += item.Quantity * price;
            }

            return total;
        }
    }
}