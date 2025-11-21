namespace Synonms.Structur.Application.Products.Context;

public class ProductContextAccessor<TProduct> : IProductContextAccessor<TProduct>
    where TProduct : StructurProduct
{
    public ProductContext<TProduct>? ProductContext { get; set; }
}