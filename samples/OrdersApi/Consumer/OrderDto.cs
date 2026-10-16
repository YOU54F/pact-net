using System;

namespace Consumer
{
    /// <summary>
    /// Order DTO
    /// </summary>
    /// <param name="Id">Order ID</param>
    /// <param name="Status">Order status</param>
    /// <param name="Date">Order date</param>
    public record OrderDto(int Id, OrderStatus Status, DateTimeOffset Date)
    {
        /// <summary>
        /// The date the order was shipped
        /// </summary>
        public DateTimeOffset? ShippedDate { get; init; }
    }
}
