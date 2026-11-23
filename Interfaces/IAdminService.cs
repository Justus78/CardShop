using api.DTOs.Account;
using api.DTOs.Order;
using api.Helpers;

namespace api.Interfaces
{
    public interface IAdminService
    {
        // Both list methods now take a query (page, page size, filter) and return one
        // page of results plus the total count, instead of every row.
        Task<PagedResult<OrderDto>> GetOrdersForAdminAsync(AdminOrderQueryObject query);
        Task<OrderDto?> GetOrderByIdAsync(int id);
        Task<OrderDto?> UpdateOrderStatusAsync(UpdateOrderStatusDto dto);

        Task<PagedResult<UserDto>> GetAllUsersAsync(AdminUserQueryObject query);
        Task<UserDto?> GetUserByIdAsync(string userId);
    }
}