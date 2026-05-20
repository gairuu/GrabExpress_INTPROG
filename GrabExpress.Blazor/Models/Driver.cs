namespace GrabExpress.Blazor.Models
{
    public class Driver
    {
        public string? DriverId { get; set; }
        public string? Name { get; set; }
        public string? ContactNumber { get; set; }
        public string? LicenseNumber { get; set; }
        public string? Status { get; set; } // Available, Busy, Offline
        public bool IsSuspended { get; set; }
    }
}
