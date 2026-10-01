using System;

namespace Inventory.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public required string PasswordHash { get; set; }
        public required string Email { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = [];
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
        public ICollection<InventoryReservation> InventoryReservations { get; set; } = [];
    }
}
