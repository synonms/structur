namespace Synonms.Structur.Application.Products.Context;

public class ProductContext<TProduct>
    where TProduct : StructurProduct
{
    private ProductContext(IEnumerable<TProduct> availableProducts, TProduct? selectedProduct)
    {
        AvailableProducts = availableProducts;
        SelectedProduct = selectedProduct;
    }
    
    public IEnumerable<TProduct> AvailableProducts { get; set; }
    
    public TProduct? SelectedProduct { get; }

    public static ProductContext<TProduct> Create(IEnumerable<TProduct> availableProducts, TProduct? selectedProduct) =>
        new (availableProducts, selectedProduct);
}