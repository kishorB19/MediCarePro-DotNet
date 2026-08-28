using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HMS.Models
{
    public class Prescription
    {
        [Key]
        public int PrescriptionId { get; set; }

        [Required]
        public int AppointmentId { get; set; }

        [ForeignKey("AppointmentId")]
        public Appointment? Appointment { get; set; }

        [Required]
        [StringLength(300)]
        public string Medication { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Dosage { get; set; }

        [StringLength(500)]
        public string? Instructions { get; set; }

        public DateTime PrescribedDate { get; set; } = DateTime.UtcNow;
    }
}
