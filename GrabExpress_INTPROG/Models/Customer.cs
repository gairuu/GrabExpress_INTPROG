using System;

namespace GrabExpress_INTPROG.Models
{
    public class Customer
    {
        public string? CustomerId { get; set; }
        public string? Name { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public bool IsSuspended { get; set; }
    }
}
