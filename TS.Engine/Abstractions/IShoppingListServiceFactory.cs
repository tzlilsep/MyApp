namespace TS.Engine.Abstractions
{
    // Factory interface for creating shopping list services using an ID token
    public interface IShoppingListServiceFactory
    {
        IShoppingListService Create(string idToken);
    }
}
