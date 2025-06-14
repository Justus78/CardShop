using api.DTOs.Order;
using CardShop.Models;

namespace api.Mappers
{
    public static class OrderMapper
    {
        public static OrderDto ToDto(this Order order) => new OrderDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            Items = order.OrderItems.Select(oi => oi.ToDto()).ToList(),
            Total = (decimal)order.TotalAmout
        };

        public static OrderItemDto ToDto(this OrderItem item) => new OrderItemDto
        {
            ProductId = item.ProductId,
            ProductName = item.Product?.Name ?? "Unknown",
            Quantity = item.Quantity,
            Price = item.UnitPrice
        };
    }
}
