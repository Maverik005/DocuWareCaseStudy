using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventRegistration.Core.Entities
{
    [Table("Events")]
    public sealed class Event
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }

        [Required]
        [MaxLength(200)]
        public required string Name { get; set; }

        [Required]
        [MaxLength(2000)]
        public required string Description { get; set; }

        [Required]
        [MaxLength(300)]
        public required string Location { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Azure Entra ID (Object ID) of the creator
        /// </summary>
        [Required]
        [MaxLength(100)]
        public required string CreatedBy { get; set; }

        [MaxLength(200)]
        public string? CreatedByName { get; set; }

        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<Registration> Registrations { get; init; } = [];

        /// <summary>
        /// Computed property - not stored in database
        /// Use for display only, not for queries
        /// </summary>
        [NotMapped]
        public int RegistrationCount => Registrations?.Count ?? 0;

        /// <summary>
        /// Soft delete flag instead of hard delete for audit trail
        /// </summary>
        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        [NotMapped]
        public bool IsUpcoming => StartTime > DateTime.UtcNow && !IsDeleted;

        /// <summary>
        /// C# 14: Property pattern matching helper
        /// </summary>
        [NotMapped]
        public bool IsActive => StartTime <= DateTime.UtcNow && EndTime >= DateTime.UtcNow && !IsDeleted;
    }
}
