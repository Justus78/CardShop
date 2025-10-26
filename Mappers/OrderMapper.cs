using api.DTOs.Order;
using api.Models;
using CardShop.Models;

namespace CardShop.Mappers
{
    public static class OrderMapper
    {
        public static OrderDto ToOrderDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                CreatedAt = order.CreatedDate,
                Status = order.Status,
                PaymentProvider = order.PaymentProvider,
                PaymentIntentId = order.PaymentIntentId,
                TotalAmount = order.TotalAmount,
                User = order.User,
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
