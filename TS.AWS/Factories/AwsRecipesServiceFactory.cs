using TS.Engine.Abstractions;
using TS.AWS.Services;

namespace TS.AWS.Factories
{
    public sealed class AwsRecipesServiceFactory : IRecipesServiceFactory
    {
        public IRecipesService Create(string idToken) => new AwsRecipesService(idToken);
    }
}
