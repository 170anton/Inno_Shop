namespace ProductService.Application.Models
{
    public class ProductSearchCriteria
    {
        public string? Name { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? IsAvailable { get; set; }
    }
}