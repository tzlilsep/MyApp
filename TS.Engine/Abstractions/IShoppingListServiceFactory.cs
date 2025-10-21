namespace TS.Engine.Abstractions
{
    public interface IShoppingListServiceFactory
    {
        IShoppingListService Create(string idToken);
    }
}
