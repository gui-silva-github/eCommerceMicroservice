using System.ComponentModel.DataAnnotations;

namespace eCommerce.DataAccessLayer.Entities
{
    public class Product
    {
        [Key]
        public Guid ProductID { get; set; }
        public required string ProductName { get; set; }
        public required string Category { get; set; }
        public double? UnitPrice { get; set; }
        public int? QuantityInStock { get; set; }
    }
}