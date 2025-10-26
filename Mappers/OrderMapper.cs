using api.DTOs.Order;
using api.Models;
using CardShop.Models;

namespace CardShop.Mappers
{
    public static class OrderMapper
    {
        public static OrderDto ToOrderDto(this Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                Username = order.User.UserName,
                CreatedAt = order.CreatedDate,
                Status = order.Status,
                PaymentProvider = order.PaymentProvider,
                PaymentIntentId = order.PaymentIntentId,
                PaidAt = order.PaidAt,
                RecipientName = order.RecipientName,
                Street = order.Street,
                City = order.City,
                State = order.State,
                PostalCode = order.PostalCode,
                Country = order.Country,
                TotalAmount = order.TotalAmount,                
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? string.Empty,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            };
        }

        public static Order ToOrder(CreateOrderDto dto, string userId)
        {
            return new Order
            {
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                Status = OrderStatus.Paid, // default, can be updated later
                PaymentProvider = dto.PaymentProvider,
                PaymentIntentId = dto.PaymentIntentId,
                OrderItems = dto.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };
        }
    }
}
