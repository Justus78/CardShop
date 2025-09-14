namespace api.Models
{
    public enum OrderStatus
    {
        Pending,    // Created, awaiting payment
        Paid,       // Payment succeeded
        Shipped,    // Admin has shipped order
        Completed,  // Delivered or confirmed
        Cancelled   // Cancelled or refunded
    } // end enum
} // end model order status
