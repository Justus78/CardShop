using api.DTOs.TradeIn;
using api.Interfaces;
using api.Models;
using CardShop.Data;
using Microsoft.EntityFrameworkCore;
using static api.Enums.ProductEnums;

namespace api.Repositories
{
    public class TradeInService : ITradeInService
    {
        private readonly ApplicationDbContext _context;

        public TradeInService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------------------
        // USER: SUBMIT TRADE-IN
        // ---------------------------------------------------------------------
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
                    Condition = i.Condition,
                    Quantity = i.Quantity,
                    EstimatedUnitValue = i.EstimatedPrice ?? 0M,
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
                SubmittedAt = tradeIn.CreatedAt
            };
        }

        // ---------------------------------------------------------------------
        // USER: LIST TRADE-INS
        // ---------------------------------------------------------------------
        public async Task<IEnumerable<TradeInSummaryDto>> GetUserTradeInsAsync(string userId)
        {
            return await _context.TradeIns
                .Where(t => t.UserId == userId)
                .Select(t => new TradeInSummaryDto
                {
                    Id = t.Id,
                    EstimatedValue = t.EstimatedValue,
                    FinalValue = t.FinalValue,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        // ---------------------------------------------------------------------
        // USER: GET SINGLE TRADE-IN
        // ---------------------------------------------------------------------
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
                EstimatedValue = tradeIn.EstimatedValue,
                FinalValue = tradeIn.FinalValue,
                CreatedAt = tradeIn.CreatedAt,
                UpdatedAt = tradeIn.UpdatedAt,
                Items = tradeIn.TradeInItems.Select(i => new TradeInItemDto
                {
                    Id = i.Id,
                    CardName = i.CardName,
                    SetCode = i.SetCode,
                    Condition = i.Condition.ToString(),
                    Quantity = i.Quantity,
                    EstimatedUnitValue = i.EstimatedUnitValue,
                    FinalUnitValue = i.FinalUnitValue
                }).ToList()
            };
        }

        // ---------------------------------------------------------------------
        // USER: CANCEL TRADE-IN
        // ---------------------------------------------------------------------
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

        // ---------------------------------------------------------------------
        // USER: ACCEPT FINAL OFFER
        // ---------------------------------------------------------------------
        public async Task<bool> ConfirmFinalOfferAsync(string userId, int tradeInId)
        {
            var tradeIn = await _context.TradeIns
                .FirstOrDefaultAsync(t => t.Id == tradeInId && t.UserId == userId);

            if (tradeIn == null || tradeIn.Status != TradeInStatus.OfferSent)
                return false;

            tradeIn.Status = TradeInStatus.Accepted;
            tradeIn.UpdatedAt = DateTime.Now;

            return await _context.SaveChangesAsync() > 0;
        }

        // ---------------------------------------------------------------------
        // USER: DECLINE FINAL OFFER
        // ---------------------------------------------------------------------
        public async Task<bool> DeclineFinalOfferAsync(string userId, int tradeInId)
        {
            var tradeIn = await _context.TradeIns
                .FirstOrDefaultAsync(t => t.Id == tradeInId && t.UserId == userId);

            if (tradeIn == null || tradeIn.Status != TradeInStatus.OfferSent)
                return false;

            tradeIn.Status = TradeInStatus.Declined;
            tradeIn.UpdatedAt = DateTime.Now;

            return await _context.SaveChangesAsync() > 0;
        }

        // ---------------------------------------------------------------------
        // USER: ESTIMATE VALUE
        // (replace later with Scryfall price lookup)
        // ---------------------------------------------------------------------
        public async Task<decimal> GetEstimatedTradeValueAsync(List<TradeInItemCreateDto> items)
        {
            decimal total = 0M;

            foreach (var item in items)
            {
                decimal price = item.EstimatedPrice ?? 0.25M;
                total += item.Quantity * price;
            }

            return total;
        }


        // =====================================================================
        // ITEM MANAGEMENT (ADD / REMOVE / UPDATE)
        // =====================================================================

        public async Task<TradeInItemDto?> AddItemAsync(string userId, int tradeInId, TradeInItemCreateDto dto)
        {
            var tradeIn = await _context.TradeIns
                .FirstOrDefaultAsync(t => t.Id == tradeInId && t.UserId == userId);

            if (tradeIn == null || tradeIn.Status != TradeInStatus.Submitted)
                return null;

            var item = new TradeInItem
            {
                TradeInId = tradeInId,
                CardName = dto.CardName,
                SetCode = dto.SetCode,
                Quantity = dto.Quantity,
                Condition = dto.Condition,
                EstimatedUnitValue = dto.EstimatedPrice ?? 0M
            };

            _context.TradeInItems.Add(item);
            await _context.SaveChangesAsync();

            return new TradeInItemDto
            {
                Id = item.Id,
                CardName = item.CardName,
                SetCode = item.SetCode,
                Condition = item.Condition.ToString(),
                Quantity = item.Quantity,
                EstimatedUnitValue = item.EstimatedUnitValue,
                FinalUnitValue = item.FinalUnitValue
            };
        }

        public async Task<bool> RemoveItemAsync(string userId, int tradeInId, int itemId)
        {
            var item = await _context.TradeInItems
                .Include(i => i.TradeIn)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.TradeInId == tradeInId);

            if (item == null || item.TradeIn.UserId != userId)
                return false;

            if (item.TradeIn.Status != TradeInStatus.Submitted)
                return false;

            _context.TradeInItems.Remove(item);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<TradeInItemDto?> UpdateItemAsync(string userId, int tradeInId, int itemId, TradeInItemCreateDto dto)
        {
            var item = await _context.TradeInItems
                .Include(i => i.TradeIn)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.TradeInId == tradeInId);

            if (item == null || item.TradeIn.UserId != userId)
                return null;

            if (item.TradeIn.Status != TradeInStatus.Submitted)
                return null;

            item.CardName = dto.CardName;
            item.SetCode = dto.SetCode;
            item.Quantity = dto.Quantity;
            item.Condition = dto.Condition;
            item.EstimatedUnitValue = dto.EstimatedPrice ?? item.EstimatedUnitValue;

            item.TradeIn!.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return new TradeInItemDto
            {
                Id = item.Id,
                CardName = item.CardName,
                SetCode = item.SetCode,
                Condition = item.Condition.ToString(),
                Quantity = item.Quantity,
                EstimatedUnitValue = item.EstimatedUnitValue,
                FinalUnitValue = item.FinalUnitValue
            };
        }


        // =====================================================================
        // ADMIN ACTIONS
        // =====================================================================

        public async Task<bool> UpdateFinalItemValueAsync(int tradeInItemId, UpdateTradeInItemValueDto dto)
        {
            var item = await _context.TradeInItems
                .Include(i => i.TradeIn)
                .FirstOrDefaultAsync(i => i.Id == tradeInItemId);

            if (item == null)
                return false;

            item.FinalUnitValue = dto.FinalUnitValue;
            item.TradeIn!.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateTradeInStatusAsync(int tradeInId, UpdateTradeInStatusDto dto)
        {
            var tradeIn = await _context.TradeIns
                .FirstOrDefaultAsync(t => t.Id == tradeInId);

            if (tradeIn == null)
                return false;

            tradeIn.Status = dto.Status;
            tradeIn.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TradeInAdminSummaryDto>> GetAllTradeInsAsync()
        {
            return await _context.TradeIns
                .Include(t => t.User)
                .Select(t => new TradeInAdminSummaryDto
                {
                    Id = t.Id,
                    UserEmail = t.User!.Email!,
                    Status = t.Status,
                    EstimatedValue = t.EstimatedValue,
                    FinalValue = t.FinalValue,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<TradeInDetailsDto?> GetAdminTradeInByIdAsync(int tradeInId)
        {
            var tradeIn = await _context.TradeIns
                .Include(t => t.User)
                .Include(t => t.TradeInItems)
                .FirstOrDefaultAsync(t => t.Id == tradeInId);

            if (tradeIn == null) return null;

            return new TradeInDetailsDto
            {
                Id = tradeIn.Id,
                UserEmail = tradeIn.User?.Email ?? "",
                Status = tradeIn.Status,
                EstimatedValue = tradeIn.EstimatedValue,
                FinalValue = tradeIn.FinalValue,
                CreatedAt = tradeIn.CreatedAt,
                Items = tradeIn.TradeInItems.Select(i => new TradeInItemDto
                {
                    Id = i.Id,
                    CardName = i.CardName,
                    SetCode = i.SetCode,
                    Condition = i.Condition.ToString(),
                    Quantity = i.Quantity,
                    EstimatedUnitValue = i.EstimatedUnitValue,
                    FinalUnitValue = i.FinalUnitValue
                }).ToList()
            };
        }
    }
}
