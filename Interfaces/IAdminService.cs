using api.DTOs.Account;
using api.DTOs.Order;
using Microsoft.AspNetCore.Mvc;

namespace api.Interfaces
{
    public interface IAdminService
    {
        public Task<List<UserDto>> GetAllUsersAsync();
        Task<List<OrderDto>> GetOrdersForAdminAsync();
    }
}
