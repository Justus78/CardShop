using static api.Enums.ProductEnums;
using System.ComponentModel.DataAnnotations;

namespace api.DTOs.Product
{
    public class UpdateProductDto
    {      
        
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int StockQuantity { get; set; }
       
    } // end dto
} // end namespace
