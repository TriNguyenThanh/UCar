using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UCar.Models.Enums;

namespace UCar.Models;

public class CustomerDocument
{
    [Key]
    public Guid DocId { get; set; }

    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public CustomerDocumentType DocType { get; set; }

    [MaxLength(100)]
    public string? DocNumber { get; set; }

    public DateTime? IssuedDate { get; set; }

    [MaxLength(200)]
    public string? IssuedPlace { get; set; }

    [MaxLength(500)]
    public string? ImageFrontUrl { get; set; }

    [MaxLength(500)]
    public string? ImageBackUrl { get; set; }

    public bool IsVerified { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;
}
