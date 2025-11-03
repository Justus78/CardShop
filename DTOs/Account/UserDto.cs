using api.DTOs.Order;
using CardShop.Models;
using Microsoft.Extensions.Primitives;

namespace api.DTOs.Account
{
    public class UserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public List<OrderDto> Orders { get; set; } = [];
        public int? OrderCount { get; set; }
    }
}
