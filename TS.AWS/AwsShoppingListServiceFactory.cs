using TS.Engine.Abstractions;

namespace TS.AWS
{
    // Factory that creates AwsShoppingListService instances using the given IdToken
    public sealed class AwsShoppingListServiceFactory : IShoppingListServiceFactory
    {
        public IShoppingListService Create(string idToken)
            => new AwsShoppingListService(idToken);
    }
}
