using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventRegistration.Core.Entities
{
    [Table("Registrations")]
    public sealed class Registration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }

        [Required]
        public int EventId { get; set; }

        /// <summary>
        /// Navigation property
        /// </summary>
        [ForeignKey(nameof(EventId))]
        public Event? Event { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Name { get; set; }

        [Required]
        [Phone]
        [MaxLength(20)]
        public required string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public required string EmailAddress { get; set; }

        public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Soft delete for audit trail
        /// </summary>
        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// For analytics - which channel the registration came from
        /// </summary>
        [MaxLength(50)]
        public string? RegistrationSource { get; set; }
    }
}
