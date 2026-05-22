using System;

namespace GrabExpress_INTPROG.Models
{
    public class Delivery
    {
        public string? DeliveryId { get; set; }
        public string? CustomerId { get; set; }
        public string? DriverId { get; set; }
        public string? PickupLocation { get; set; }
        public string? DropoffLocation { get; set; }
        public DateTime BookingTime { get; set; } = DateTime.UtcNow;
        public string? DeliveryStatus { get; set; } // Pending, In Transit, Delivered, Cancelled
        public decimal DeliveryFee { get; set; }
        public System.Collections.Generic.Dictionary<string, bool>? DeclinedDrivers { get; set; }
    }
}
