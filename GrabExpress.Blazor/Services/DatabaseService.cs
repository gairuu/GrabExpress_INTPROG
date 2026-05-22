using Firebase.Database;
using Firebase.Database.Query;
using GrabExpress.Blazor.Models;
using System.Linq;

namespace GrabExpress.Blazor.Services
{
    public class DatabaseService
    {
        private readonly FirebaseClient _firebaseClient;

        public DatabaseService()
        {
            _firebaseClient = new FirebaseClient(FirebaseConfig.DatabaseUrl);
        }

        public FirebaseClient GetFirebaseClient()
        {
            return _firebaseClient;
        }

        public async Task SaveUserProfileAsync(string userId, Customer profile)
        {
            await _firebaseClient
                .Child("Customers")
                .Child(userId)
                .PutAsync(profile);
        }

        public async Task<Customer> GetUserProfileAsync(string userId)
        {
            return await _firebaseClient
                .Child("Customers")
                .Child(userId)
                .OnceSingleAsync<Customer>();
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            try
            {
                var customers = await _firebaseClient
                    .Child("Customers")
                    .OnceAsync<Customer>();

                // Fetch roles to identify Admins and Suspended users
                var roles = await _firebaseClient
                    .Child("UserRoles")
                    .OnceAsync<string>();

                var adminIds = roles
                    .Where(r => r.Object == "Admin")
                    .Select(r => r.Key)
                    .ToHashSet();

                var suspendedIds = roles
                    .Where(r => r.Object == "Suspended")
                    .Select(r => r.Key)
                    .ToHashSet();

                return customers
                    .Where(c => c.Object != null)
                    .Select(c => 
                    {
                        var cust = c.Object;
                        cust.CustomerId = c.Key;
                        cust.IsSuspended = suspendedIds.Contains(c.Key);
                        return cust;
                    })
                    .Where(c => !adminIds.Contains(c.CustomerId ?? string.Empty))
                    .ToList();
            }
            catch
            {
                return new List<Customer>();
            }
        }

        public async Task DeleteCustomerAsync(string customerId)
        {
            // Suspend role instead of deleting profile to allow reactivation
            await _firebaseClient
                .Child("UserRoles")
                .Child(customerId)
                .PutAsync($"\"Suspended\"");
        }

        // Role Management
        public async Task SaveUserRoleAsync(string userId, string role)
        {
            await _firebaseClient
                .Child("UserRoles")
                .Child(userId)
                .PutAsync($"\"{role}\"");
        }

        public async Task<string?> GetUserRoleAsync(string userId)
        {
            try 
            {
                var role = await _firebaseClient
                    .Child("UserRoles")
                    .Child(userId)
                    .OnceSingleAsync<string>();
                
                // Remove outer quotes if returned by Firebase
                if (!string.IsNullOrEmpty(role) && role.StartsWith("\"") && role.EndsWith("\""))
                {
                    role = role.Substring(1, role.Length - 2);
                }

                return role; 
            }
            catch 
            {
                return null;
            }
        }

        // Driver Methods
        public async Task SaveDriverAsync(Driver driver)
        {
            await _firebaseClient
                .Child("Drivers")
                .Child(driver.DriverId)
                .PutAsync(driver);
        }

        public async Task UpdateDriverStatusAsync(string driverId, string status)
        {
            await _firebaseClient
                .Child("Drivers")
                .Child(driverId)
                .Child("Status")
                .PutAsync($"\"{status}\"");
        }

        public async Task DeleteDriverAsync(string driverId)
        {
            // Suspend role instead of deleting profile to allow reactivation
            await _firebaseClient
                .Child("UserRoles")
                .Child(driverId)
                .PutAsync($"\"Suspended\"");

            await _firebaseClient
                .Child("Drivers")
                .Child(driverId)
                .Child("Status")
                .PutAsync($"\"Offline\"");
        }

        public async Task<Vehicle?> GetVehicleAsync(string driverId)
        {
            var vehicles = await _firebaseClient
                .Child("Vehicles")
                .OnceAsync<Vehicle>();

            return vehicles
                .Where(v => v.Object != null)
                .Select(v => v.Object)
                .FirstOrDefault(v => v.DriverId == driverId);
        }

        public async Task<List<Driver>> GetAvailableDriversAsync()
        {
            var drivers = await _firebaseClient
                .Child("Drivers")
                .OnceAsync<Driver>();

            return drivers
                .Where(d => d.Object != null)
                .Select(d => 
                {
                    var drv = d.Object;
                    drv.DriverId = d.Key;
                    return drv;
                })
                .Where(d => d.Status == "Available")
                .ToList();
        }

        public async Task<List<Delivery>> GetDriverJobsAsync(string driverId)
        {
            try
            {
                var deliveries = await _firebaseClient
                    .Child("Deliveries")
                    .OnceAsync<Delivery>();

                return deliveries
                    .Where(d => d.Object != null)
                    .Select(d => 
                    {
                        var delivery = d.Object;
                        delivery.DeliveryId = d.Key;
                        return delivery;
                    })
                    .Where(d => d.DriverId == driverId)
                    .ToList();
            }
            catch
            {
                return new List<Delivery>();
            }
        }

        public async Task<List<Delivery>> GetPendingJobsAsync()
        {
            try
            {
                var deliveries = await _firebaseClient
                    .Child("Deliveries")
                    .OnceAsync<Delivery>();

                return deliveries
                    .Where(d => d.Object != null && 
                               (string.IsNullOrEmpty(d.Object.DriverId) || d.Object.DriverId == "") && 
                               d.Object.DeliveryStatus == "Pending")
                    .Select(d => 
                    {
                        var delivery = d.Object;
                        delivery.DeliveryId = d.Key;
                        return delivery;
                    })
                    .ToList();
            }
            catch
            {
                return new List<Delivery>();
            }
        }

        public async Task<List<Driver>> GetAllDriversAsync()
        {
            try
            {
                var drivers = await _firebaseClient
                    .Child("Drivers")
                    .OnceAsync<Driver>();

                // Fetch roles to identify suspended drivers
                var roles = await _firebaseClient
                    .Child("UserRoles")
                    .OnceAsync<string>();

                var suspendedIds = roles
                    .Where(r => r.Object == "Suspended")
                    .Select(r => r.Key)
                    .ToHashSet();

                return drivers
                    .Where(d => d.Object != null)
                    .Select(d => 
                    {
                        var drv = d.Object;
                        drv.DriverId = d.Key;
                        drv.IsSuspended = suspendedIds.Contains(d.Key);
                        return drv;
                    })
                    .ToList();
            }
            catch
            {
                return new List<Driver>();
            }
        }

        public async Task<Driver> GetDriverAsync(string driverId)
        {
            return await _firebaseClient
                .Child("Drivers")
                .Child(driverId)
                .OnceSingleAsync<Driver>();
        }

        public async Task<List<Delivery>> GetAllDeliveriesAsync()
        {
            try
            {
                var deliveries = await _firebaseClient
                    .Child("Deliveries")
                    .OnceAsync<Delivery>();

                return deliveries
                    .Where(d => d.Object != null)
                    .Select(d => 
                    {
                        var delivery = d.Object;
                        delivery.DeliveryId = d.Key;
                        return delivery;
                    })
                    .ToList();
            }
            catch
            {
                return new List<Delivery>();
            }
        }



        public async Task<Delivery?> GetCustomerActiveDeliveryAsync(string customerId)
        {
            var deliveries = await _firebaseClient
                .Child("Deliveries")
                .OnceAsync<Delivery>();

            return deliveries
                .Where(d => d.Object != null)
                .Select(d => 
                {
                    var delivery = d.Object;
                    delivery.DeliveryId = d.Key;
                    return delivery;
                })
                .Where(d => d.CustomerId == customerId && (d.DeliveryStatus == "Pending" || d.DeliveryStatus == "In Transit" || d.DeliveryStatus == "Delivered"))
                .OrderByDescending(d => d.BookingTime)
                .FirstOrDefault();
        }

        public async Task<List<Delivery>> GetCustomerDeliveryHistoryAsync(string customerId)
        {
            var deliveries = await _firebaseClient
                .Child("Deliveries")
                .OnceAsync<Delivery>();

            return deliveries
                .Where(d => d.Object != null)
                .Select(d =>
                {
                    var delivery = d.Object;
                    delivery.DeliveryId = d.Key;
                    return delivery;
                })
                .Where(d => d.CustomerId == customerId &&
                            (d.DeliveryStatus == "Completed" || d.DeliveryStatus == "Cancelled"))
                .OrderByDescending(d => d.BookingTime)
                .ToList();
        }


        public async Task AssignDriverToDeliveryAsync(string deliveryId, string driverId)
        {
            var delivery = await GetDeliveryAsync(deliveryId);
            if (delivery != null)
            {
                delivery.DriverId = driverId;
                delivery.DeliveryStatus = "In Transit"; // Set to In Transit automatically
                await _firebaseClient
                    .Child("Deliveries")
                    .Child(deliveryId)
                    .PutAsync(delivery);

                // Lock driver to Busy
                await UpdateDriverStatusAsync(driverId, "Busy");
            }
            else
            {
                // Fallback if delivery is somehow null
                await _firebaseClient
                    .Child("Deliveries")
                    .Child(deliveryId)
                    .Child("DriverId")
                    .PutAsync($"\"{driverId}\"");
                
                await _firebaseClient
                    .Child("Deliveries")
                    .Child(deliveryId)
                    .Child("DeliveryStatus")
                    .PutAsync($"\"In Transit\"");
            }

            // Lock the driver to Busy
            await UpdateDriverStatusAsync(driverId, "Busy");
        }

        // Delivery Methods
        public async Task<string> CreateDeliveryAsync(Delivery delivery)
        {
            var result = await _firebaseClient
                .Child("Deliveries")
                .PostAsync(delivery);
            
            return result.Key;
        }

        public async Task UpdateDeliveryStatusAsync(string deliveryId, string status)
        {
            // PutAsync treats the argument as raw JSON — the string must be quoted to be valid JSON.
            await _firebaseClient
                .Child("Deliveries")
                .Child(deliveryId)
                .Child("DeliveryStatus")
                .PutAsync($"\"{status}\"");
        }

        public async Task<Delivery> GetDeliveryAsync(string deliveryId)
        {
            return await _firebaseClient
                .Child("Deliveries")
                .Child(deliveryId)
                .OnceSingleAsync<Delivery>();
        }

        public IDisposable ListenToDelivery(string deliveryId, Action<Delivery> onDeliveryUpdated)
        {
            // Create a wrapper to manage the polling cycle
            var poller = new DeliveryPoller(this, deliveryId, onDeliveryUpdated);
            poller.Start();
            return poller;
        }

        private class DeliveryPoller : IDisposable
        {
            private readonly DatabaseService _db;
            private readonly string _deliveryId;
            private readonly Action<Delivery> _callback;
            private System.Threading.Timer? _timer;

            public DeliveryPoller(DatabaseService db, string deliveryId, Action<Delivery> callback)
            {
                _db = db;
                _deliveryId = deliveryId;
                _callback = callback;
            }

            public void Start()
            {
                // Poll immediately, then every 3 seconds
                _timer = new System.Threading.Timer(async _ => await PollAsync(), null, 0, 3000);
            }

            private async Task PollAsync()
            {
                try
                {
                    var delivery = await _db.GetDeliveryAsync(_deliveryId);
                    if (delivery != null)
                    {
                        delivery.DeliveryId = _deliveryId;
                        _callback(delivery);
                    }
                }
                catch { /* Ignore background polling errors */ }
            }

            public void Dispose()
            {
                _timer?.Dispose();
            }
        }

        // Payment Methods
        public async Task RecordPaymentAsync(Payment payment)
        {
            await _firebaseClient
                .Child("Payments")
                .PostAsync(payment);
        }

        // Business Logic
        public decimal CalculateDeliveryFee(string pickup, string dropoff)
        {
            // Simple mock logic: base fee 50 + random distance factor
            // In a real app, this would use a distance matrix API
            Random rnd = new Random();
            return 50 + rnd.Next(20, 150);
        }

    }
}
