using api.DTOs.TradeIn;
using api.Interfaces;
using api.Models;
using CardShop.Data;
using Microsoft.EntityFrameworkCore;
using static api.Enums.ProductEnums;

namespace api.Services
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
                    EstimatedUnitValue = i.EstimatedPrice ?? 0M
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

        public async Task<IEnumerable<TradeInSummaryDto>> GetUserTradeInsAsync(string userId)
        {
            return await _context.TradeIns
                .Where(t => t.UserId == userId && t.Status != TradeInStatus.Draft)
                .Select(t => new TradeInSummaryDto
                {
                    Id = t.Id,
                    Status = t.Status,
                    EstimatedValue = t.EstimatedValue,
                    FinalValue = t.FinalValue,
                    CreatedAt = t.CreatedAt
                }).ToListAsync();
        }

        public async Task<TradeInDetailDto?> GetTradeInByIdAsync(string userId, int tradeInId)
        {
            var tradeIn = await _context.TradeIns
                .Include(t => t.TradeInItems)
                .FirstOrDefaultAsync(t => t.Id == tradeInId && t.UserId == userId);

            if (tradeIn == null) return null;

            return MapToTradeInDetailDto(tradeIn);
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

            if (tradeIn == null || tradeIn.Status != TradeInStatus.OfferSent)
                return false;

            tradeIn.Status = TradeInStatus.Accepted;
            tradeIn.UpdatedAt = DateTime.Now;

            return await _context.SaveChangesAsync() > 0;
        }

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
        // DRAFT / PERSISTENT TRADE-IN METHODS
        // =====================================================================

        public async Task<TradeInDetailDto> GetOrCreateDraftAsync(string userId)
        {
            var draft = await _context.TradeIns
                .Include(t => t.TradeInItems)
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Status == TradeInStatus.Draft);

            if (draft == null)
            {
                draft = new TradeIn
                {
                    UserId = userId,
                    Status = TradeInStatus.Draft,
                    CreatedAt = DateTime.Now,
                    TradeInItems = new List<TradeInItem>()
                };
                _context.TradeIns.Add(draft);
                await _context.SaveChangesAsync();
            }

            return MapToTradeInDetailDto(draft);
        }

        public async Task<TradeInDetailDto> AddItemToDraftAsync(string userId, TradeInItemCreateDto dto)
        {
            var draft = await _context.TradeIns
                .Include(t => t.TradeInItems)
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Status == TradeInStatus.Draft);

            if (draft == null)
                draft = new TradeIn
                {
                    UserId = userId,
                    Status = TradeInStatus.Draft,
                    CreatedAt = DateTime.Now,
                    TradeInItems = new List<TradeInItem>()
                };

            var item = new TradeInItem
            {
                CardName = dto.CardName,
                SetCode = dto.SetCode,
                Quantity = dto.Quantity,
                Condition = dto.Condition,
                EstimatedUnitValue = dto.EstimatedPrice ?? 0M
            };

            draft.TradeInItems.Add(item);

            _context.TradeIns.Update(draft);
            await _context.SaveChangesAsync();

            return MapToTradeInDetailDto(draft);
        }

        public async Task<bool> RemoveItemFromDraftAsync(string userId, int itemId)
        {
            var item = await _context.TradeInItems
                .Include(i => i.TradeIn)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.TradeIn.UserId == userId && i.TradeIn.Status == TradeInStatus.Draft);

            if (item == null) return false;

            _context.TradeInItems.Remove(item);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<TradeInDto?> SubmitDraftAsync(string userId)
        {
            var draft = await _context.TradeIns
                .Include(t => t.TradeInItems)
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Status == TradeInStatus.Draft);

            if (draft == null) return null;

            draft.Status = TradeInStatus.Submitted;
            draft.EstimatedValue = draft.TradeInItems.Sum(i => i.EstimatedUnitValue * i.Quantity);
            draft.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return new TradeInDto
            {
                Id = draft.Id,
                Status = draft.Status,
                SubmittedAt = draft.UpdatedAt ?? draft.CreatedAt,
                EstimatedValue = draft.EstimatedValue
            };
        }

        // =====================================================================
        // ITEM MANAGEMENT (existing trade-ins)
        // =====================================================================
        public async Task<TradeInItemDto?> AddItemAsync(string userId, int tradeInId, TradeInItemCreateDto dto)
        {
            var tradeIn = await _context.TradeIns
                .FirstOrDefaultAsync(t => t.Id == tradeInId && t.UserId == userId && t.Status == TradeInStatus.Submitted);

            if (tradeIn == null) return null;

            var item = new TradeInItem
            {
                CardName = dto.CardName,
                SetCode = dto.SetCode,
                Quantity = dto.Quantity,
                Condition = dto.Condition,
                EstimatedUnitValue = dto.EstimatedPrice ?? 0M,
                TradeInId = tradeInId
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
                EstimatedUnitValue = item.EstimatedUnitValue
            };
        }

        public async Task<bool> RemoveItemAsync(string userId, int tradeInId, int itemId)
        {
            var item = await _context.TradeInItems
                .Include(i => i.TradeIn)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.TradeInId == tradeInId && i.TradeIn.UserId == userId && i.TradeIn.Status == TradeInStatus.Submitted);

            if (item == null) return false;

            _context.TradeInItems.Remove(item);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<TradeInItemDto?> UpdateItemAsync(string userId, int tradeInId, int itemId, TradeInItemCreateDto dto)
        {
            var item = await _context.TradeInItems
                .Include(i => i.TradeIn)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.TradeInId == tradeInId && i.TradeIn.UserId == userId && i.TradeIn.Status == TradeInStatus.Submitted);

            if (item == null) return null;

            item.CardName = dto.CardName;
            item.SetCode = dto.SetCode;
            item.Quantity = dto.Quantity;
            item.Condition = dto.Condition;
            item.EstimatedUnitValue = dto.EstimatedPrice ?? item.EstimatedUnitValue;

            item.TradeIn.UpdatedAt = DateTime.Now;
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

            if (item == null) return false;

            item.FinalUnitValue = dto.FinalUnitValue;
            item.TradeIn.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateTradeInStatusAsync(int tradeInId, UpdateTradeInStatusDto dto)
        {
            var tradeIn = await _context.TradeIns.FirstOrDefaultAsync(t => t.Id == tradeInId);

            if (tradeIn == null) return false;

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
                }).ToListAsync();
        }

        public async Task<TradeInDetailsDto?> GetAdminTradeInByIdAsync(int tradeInId)
        {
            var tradeIn = await _context.TradeIns
                .Include(t => t.User)
                .Include(t => t.TradeInItems)
                .FirstOrDefaultAsync(t => t.Id == tradeInId);

            if (tradeIn == null) return null;

            return MapToTradeInDetailsDto(tradeIn);
        }

        // ---------------------------------------------------------------------
        // HELPERS
        // ---------------------------------------------------------------------
        private static TradeInDetailDto MapToTradeInDetailDto(TradeIn tradeIn)
        {
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

        private static TradeInDetailsDto MapToTradeInDetailsDto(TradeIn tradeIn)
        {
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
