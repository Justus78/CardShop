using System.ComponentModel.DataAnnotations;
using api.Models;

namespace api.DTOs.Order
{
    public class UpdateOrderStatusDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        [EnumDataType(typeof(OrderStatus))]
        public OrderStatus Status { get; set; }
    }
}
