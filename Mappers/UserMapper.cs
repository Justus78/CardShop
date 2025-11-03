using api.DTOs.Account;
using api.DTOs.Order;
using CardShop.Mappers;
using CardShop.Models;

namespace api.Mappers
{
    public static class UserMapper
    {
        public static UserDto ToUserDto(ApplicationUser user)
        {
            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Orders = user.Orders?
                    .Select(o => o.ToOrderDto())
                    .ToList() ?? new List<OrderDto>(),
                OrderCount = user.Orders?.Count
            };
        }
    }
}
