using System.ComponentModel.DataAnnotations;

namespace APIEcommerce.Models.Dtos
{
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required")]
        [MaxLength(50, ErrorMessage = "Category name cannot exceed 50 characters")]
        [MinLength(3, ErrorMessage = "Category name must be at least 3 characters")]
        public string Name { get; set; } = string.Empty;
    }
}
