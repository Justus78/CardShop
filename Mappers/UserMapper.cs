using api.DTOs.Account;
using CardShop.Models;
using System.Reflection.Metadata.Ecma335;

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
                OrderCount = user.Orders?.Count
            };
        }
    }
}
