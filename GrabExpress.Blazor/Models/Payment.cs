using System;

namespace GrabExpress.Blazor.Models
{
    public class Payment
    {
        public string? PaymentId { get; set; }
        public string? DeliveryId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; } // Cash, Card, E-wallet
        public string? PaymentStatus { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    }
}
