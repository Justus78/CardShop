using api.DTOs.Account;
using api.DTOs.Order;
using CardShop.Models;
using Microsoft.AspNetCore.Mvc;

namespace api.Interfaces
{
    public interface IAdminService
    {
       
        Task<List<OrderDto>> GetOrdersForAdminAsync();
        Task<OrderDto> GetOrderByIdAsync(int id);
        Task<OrderDto> UpdateOrderStatusAsync(UpdateOrderStatusDto dto);
        public Task<List<UserDto>> GetAllUsersAsync();
        Task<ApplicationUser> GetUserByIdAsync(string userId);
    }
}
