using api.DTOs.Order;
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
                OrderDate = order.OrderDate,
                PaymentStatus = order.PaymentStatus,
                PaymentProvider = order.PaymentProvider,
                TransactionId = order.TransactionId,
                TotalAmount = order.TotalAmount,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product?.Name ?? string.Empty,
                    ImageUrl = oi.Product?.ImageUrl ?? string.Empty,
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
                OrderDate = DateTime.UtcNow,
                PaymentStatus = "Pending", // default, can be updated later
                PaymentProvider = dto.PaymentProvider,
                TransactionId = dto.TransactionId,
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
