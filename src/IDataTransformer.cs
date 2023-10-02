namespace LiteMapper
{
    public interface IDataTransformer<out T>
    {
        T Transform(object data);
    }
}
