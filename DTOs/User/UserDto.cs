using api.DTOs.Order;

namespace api.DTOs.User
{
    public class UserDto
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public List<OrderDto> Orders { get; set; } 

    }
}
