namespace Synonms.Structur.Application.Products.Context;

public interface IProductContextAccessor<TProduct>
    where TProduct : StructurProduct
{
    ProductContext<TProduct>? ProductContext { get; set; } 
}