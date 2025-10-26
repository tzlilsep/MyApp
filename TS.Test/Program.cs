using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;   // בשביל ConditionalCheckFailedException
using TS.AWS.Auth;
using TS.AWS.Services;
using TS.Engine.Contracts;

internal class Program
{
    static async Task Main()
    {
        Console.WriteLine("== TS.AWS Recipes Seed (create & keep) ==");

        // ❗ שימי כאן את המשתמש מה-Cognito User Pool שלך (לא AWS Console!)
        var username = "ts";      // למשל: testuser@example.com
        var password = "123456";  // הסיסמה של אותו משתמש ב-User Pool

        // 1) התחברות ל-Cognito וקבלת userId + idToken
        var auth = new AwsAuthService();
        var signIn = await auth.SignInAsync(username, password);
        if (!signIn.Ok || signIn.UserId is null || signIn.IdToken is null)
        {
            Console.WriteLine($"❌ Sign-in failed: {signIn.Error}");
            return;
        }

        string userId = signIn.UserId!;
        string idToken = signIn.IdToken!;
        Console.WriteLine($"UserId: {userId}");

        // 2) יצירת שירות מתכונים
        var svc = new AwsRecipesService(idToken);

        // אפשר לקבע מזהה בשביל לרוץ שוב על אותו מתכון; כאן משתמשים בחדש כל פעם:
        var recipeId = Guid.NewGuid().ToString("N");
        var dto = NewRecipe(userId, recipeId);

        try
        {
            Console.WriteLine("🚀 Create recipe...");
            await svc.CreateRecipeAsync(dto);
        }
        catch (ConditionalCheckFailedException)
        {
            Console.WriteLine("ℹ️ Recipe already exists. Updating instead...");
            await svc.UpdateRecipeAsync(dto);
        }

        // 3) וידוא שהמתכון נשמר
        var loaded = await svc.GetRecipeAsync(userId, recipeId);
        Console.WriteLine($"✅ Saved recipe: '{loaded.Title}'  (Ingredients={loaded.Ingredients.Count}, Steps={loaded.Steps.Count})");
        Console.WriteLine($"RecipeId: {recipeId}");

        // 4) מציגים גם כמה תקצירים יש למשתמש (לא חובה)
        var summaries = await svc.GetMyRecipesAsync(userId, take: 20, skip: 0);
        Console.WriteLine($"Summaries for user: {summaries.Count}");
        Console.WriteLine($"Included? {(summaries.Any(s => s.RecipeId == recipeId) ? "Yes" : "No")}");

        Console.WriteLine("🟢 Done. Recipe left in DynamoDB.");
    }

    private static RecipeDetailDto NewRecipe(string userId, string recipeId) =>
        new RecipeDetailDto(
            UserId: userId,
            RecipeId: recipeId,
            Title: "לחם מהיר",
            PrepMinutes: 15,
            TotalMinutes: 60,
            Servings: 6,
            ImageUrl: null,
            Ingredients: new[]
            {
                new IngredientDto("500 גר' קמח"),
                new IngredientDto("300 מ״ל מים"),
                new IngredientDto("כפית מלח"),
                new IngredientDto("שמרים יבשים 7 גר׳")
            },
            Steps: new[]
            {
                new StepDto("לשים עד לקבלת בצק חלק.", null),
                new StepDto("התפחה 30 דק׳.", null),
                new StepDto("אופים 25–30 דק׳ ב-190°.", null)
            },
            Notes: "נשמר לבדיקה — אפשר למחוק ידנית מהקונסול."
        );
}
