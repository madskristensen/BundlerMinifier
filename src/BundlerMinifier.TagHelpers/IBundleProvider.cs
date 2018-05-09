namespace BundlerMinifier.TagHelpers
{
    public interface IBundleProvider
    {
        Bundle GetBundle(string name);
    }
}