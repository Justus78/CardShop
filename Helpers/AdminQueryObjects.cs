using api.Models;

namespace api.Helpers
{
    // Shared paging inputs. The setters clamp the values, so no caller can ask for page 0,
    // a negative page, or a million rows at once, and the response reports the values that
    // were actually used.
    public class PagedQueryObject
    {
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        private int _page = 1;
        private int _pageSize = DefaultPageSize;

        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 1 : Math.Min(value, MaxPageSize);
        }
    }

    // GET api/admin/orders?page=1&pageSize=20&status=Paid
    public class AdminOrderQueryObject : PagedQueryObject
    {
        // Optional. Accepts a status name (Paid) or number (1). An unknown value is
        // rejected with a 400 automatically.
        public OrderStatus? Status { get; set; }
    }

    // GET api/admin/users?page=1&pageSize=20&searchTerm=matt
    public class AdminUserQueryObject : PagedQueryObject
    {
        // Optional. Matches part of the username or email address.
        public string? SearchTerm { get; set; }
    }
}