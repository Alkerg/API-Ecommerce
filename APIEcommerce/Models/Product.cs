using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIEcommerce.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        [Column(TypeName ="decimal(18,2)")]
        public decimal Price { get; set; }

        public string? ImgUrl { get; set; }
        public string? ImgUrlLocal { get; set; }

        [Required]
        public string SKU { get; set; } = string.Empty; // PROD-001-BLK-M

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdateDate { get; set; } = null;


        // Relación con el modelo Category
        
        [ForeignKey("CategoryId")]
        public int CategoryId { get; set; }

        public required Category category { get; set; }
    }
}
