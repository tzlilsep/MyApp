using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using TS.AWS.Factories;
using TS.Engine.Abstractions;
using TS.Engine.Contracts;

namespace TS.AWS.Services
{
    /// <summary>
    /// שירות מתכונים בענן (DynamoDB).
    /// מימוש מלא של IRecipesService:
    /// - GetMyRecipesAsync: תקצירי מתכונים של המשתמש להצגה
    /// - GetRecipeAsync: טעינת מתכון מלא לצפייה
    /// - CreateRecipeAsync: יצירת מתכון מלא (נכשל אם כבר קיים)
    /// - UpdateRecipeAsync: עדכון מלא (Upsert header + החלפת ילדים)
    /// - DeleteRecipeAsync: מחיקה מלאה (header + ילדים)
    /// מבנה המפתחות:
    /// PK = USER#{userId}
    /// SK = RECIPE#{recipeId}                (Type = "Recipe")
    /// SK = RECIPE#{recipeId}#ING#{index}    (Type = "RecipeIngredient")
    /// SK = RECIPE#{recipeId}#STEP#{index}   (Type = "RecipeStep")
    /// </summary>
    public sealed class AwsRecipesService : IRecipesService
    {
        private const string TableName = "AppData";
        private readonly IAmazonDynamoDB _ddb;

        public AwsRecipesService(string idToken)
        {
            _ddb = AwsClientsFactory.CreateDynamoDbFromIdToken(idToken);
        }

        // -------------------- READ: Summaries --------------------
        public async Task<IReadOnlyList<RecipeSummaryDto>> GetMyRecipesAsync(string userId, int take = 20, int skip = 0)
        {
            var resp = await _ddb.QueryAsync(new QueryRequest
            {
                TableName = TableName,
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
                ExpressionAttributeValues = new()
                {
                    [":pk"] = new($"USER#{userId}"),
                    [":sk"] = new("RECIPE#")
                },
                ExpressionAttributeNames = new()
                {
                    ["#T"] = "Title",
                    ["#PM"] = "PrepMinutes",
                    ["#TM"] = "TotalMinutes",
                    ["#IMG"] = "ImageUrl",
                    ["#TYPE"] = "Type"
                },
                ProjectionExpression = "SK, #T, #PM, #TM, #IMG, #TYPE",
                ScanIndexForward = true
            });

            var summaries = resp.Items
                .Where(i => i.TryGetValue("Type", out var t) && t.S == "Recipe")
                .Skip(skip)
                .Take(take)
                .Select(i =>
                {
                    var id = i["SK"].S.Replace("RECIPE#", string.Empty);
                    var title = i.TryGetValue("Title", out var tt) ? tt.S : "מתכון";
                    var prep = i.TryGetValue("PrepMinutes", out var pm) && int.TryParse(pm.N, out var p) ? p : 0;
                    var total = i.TryGetValue("TotalMinutes", out var tm) && int.TryParse(tm.N, out var to) ? to : 0;
                    var img = i.TryGetValue("ImageUrl", out var im) ? im.S : null;
                    return new RecipeSummaryDto(id, title, prep, total, img);
                })
                .ToList();

            return summaries;
        }

        // -------------------- READ: Full recipe --------------------
        public async Task<RecipeDetailDto> GetRecipeAsync(string userId, string recipeId)
        {
            var resp = await _ddb.QueryAsync(new QueryRequest
            {
                TableName = TableName,
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
                ExpressionAttributeValues = new()
                {
                    [":pk"] = new($"USER#{userId}"),
                    [":sk"] = new($"RECIPE#{recipeId}")
                }
            });

            var header = resp.Items.FirstOrDefault(i => i.TryGetValue("Type", out var t) && t.S == "Recipe");
            if (header is null)
                throw new KeyNotFoundException("Recipe not found.");

            string title = header.TryGetValue("Title", out var t1) ? t1.S : "מתכון";
            int prep = header.TryGetValue("PrepMinutes", out var pm) && int.TryParse(pm.N, out var p) ? p : 0;
            int total = header.TryGetValue("TotalMinutes", out var tm) && int.TryParse(tm.N, out var to) ? to : 0;
            int? servings = header.TryGetValue("Servings", out var sv) && int.TryParse(sv.N, out var s) ? s : (int?)null;
            string? img = header.TryGetValue("ImageUrl", out var im) ? im.S : null;
            string? notes = header.TryGetValue("Notes", out var no) ? no.S : null;

            var ingredients = new List<IngredientDto>();
            foreach (var it in resp.Items.Where(i => i.TryGetValue("Type", out var t) && t.S == "RecipeIngredient"))
            {
                var text = it.TryGetValue("Text", out var tx) ? tx.S : "";
                if (!string.IsNullOrWhiteSpace(text))
                    ingredients.Add(new IngredientDto(text));
            }

            var steps = new List<StepDto>();
            foreach (var it in resp.Items.Where(i => i.TryGetValue("Type", out var t) && t.S == "RecipeStep")
                                         .OrderBy(i => i["SK"].S))
            {
                var text = it.TryGetValue("Text", out var tx) ? tx.S : "";
                var imageUrl = it.TryGetValue("ImageUrl", out var si) ? si.S : null;
                if (!string.IsNullOrWhiteSpace(text))
                    steps.Add(new StepDto(text, imageUrl));
            }

            return new RecipeDetailDto(userId, recipeId, title, prep, total, servings, img, ingredients, steps, notes);
        }

        // -------------------- CREATE (fails if exists) --------------------
        public async Task CreateRecipeAsync(RecipeDetailDto r)
        {
            var header = BuildHeader(r);
            await _ddb.PutItemAsync(new PutItemRequest
            {
                TableName = TableName,
                Item = header,
                ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
            });

            await WriteChildrenAsync(r, clearExisting: false);
        }

        // -------------------- UPDATE (upsert header + replace children) --------------------
        public async Task UpdateRecipeAsync(RecipeDetailDto r)
        {
            var header = BuildHeader(r);
            await _ddb.PutItemAsync(new PutItemRequest { TableName = TableName, Item = header });
            await WriteChildrenAsync(r, clearExisting: true);
        }

        // -------------------- DELETE --------------------
        public async Task DeleteRecipeAsync(string userId, string recipeId)
        {
            var q = await _ddb.QueryAsync(new QueryRequest
            {
                TableName = TableName,
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
                ExpressionAttributeValues = new()
                {
                    [":pk"] = new($"USER#{userId}"),
                    [":sk"] = new($"RECIPE#{recipeId}")
                },
                ProjectionExpression = "PK, SK"
            });

            var batch = new List<WriteRequest>();
            foreach (var it in q.Items)
            {
                batch.Add(new WriteRequest(new DeleteRequest(new()
                {
                    ["PK"] = it["PK"],
                    ["SK"] = it["SK"]
                })));

                if (batch.Count == 25) await FlushAsync(batch);
            }
            if (batch.Count > 0) await FlushAsync(batch);
        }

        // -------------------- Helpers --------------------
        private static Dictionary<string, AttributeValue> BuildHeader(RecipeDetailDto r)
        {
            var item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new($"USER#{r.UserId}"),
                ["SK"] = new($"RECIPE#{r.RecipeId}"),
                ["Type"] = new("Recipe"),
                ["Title"] = new(r.Title ?? "מתכון"),
                ["PrepMinutes"] = new AttributeValue { N = Math.Max(0, r.PrepMinutes).ToString() },
                ["TotalMinutes"] = new AttributeValue { N = Math.Max(0, r.TotalMinutes).ToString() },
                ["UpdatedAt"] = new(DateTime.UtcNow.ToString("o"))
            };
            if (r.Servings is > 0) item["Servings"] = new AttributeValue { N = r.Servings.Value.ToString() };
            if (!string.IsNullOrWhiteSpace(r.ImageUrl)) item["ImageUrl"] = new(r.ImageUrl);
            if (!string.IsNullOrWhiteSpace(r.Notes)) item["Notes"] = new(r.Notes);
            return item;
        }

        private async Task WriteChildrenAsync(RecipeDetailDto r, bool clearExisting)
        {
            if (clearExisting)
            {
                var existing = await _ddb.QueryAsync(new QueryRequest
                {
                    TableName = TableName,
                    KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
                    ExpressionAttributeValues = new()
                    {
                        [":pk"] = new($"USER#{r.UserId}"),
                        [":sk"] = new($"RECIPE#{r.RecipeId}#")
                    },
                    ProjectionExpression = "PK, SK"
                });

                var deletes = existing.Items.Select(it =>
                    new WriteRequest(new DeleteRequest(new()
                    {
                        ["PK"] = it["PK"],
                        ["SK"] = it["SK"]
                    }))).ToList();

                foreach (var chunk in Chunk(deletes, 25))
                    await _ddb.BatchWriteItemAsync(new BatchWriteItemRequest { RequestItems = new() { [TableName] = chunk } });
            }

            var writes = new List<WriteRequest>();

            // Ingredients
            for (int i = 0; i < (r.Ingredients?.Count ?? 0); i++)
            {
                var it = r.Ingredients![i];
                if (string.IsNullOrWhiteSpace(it.Text)) continue;

                writes.Add(new WriteRequest(new PutRequest(new()
                {
                    ["PK"] = new($"USER#{r.UserId}"),
                    ["SK"] = new($"RECIPE#{r.RecipeId}#ING#{i:D4}"),
                    ["Type"] = new("RecipeIngredient"),
                    ["Text"] = new(it.Text.Trim())
                })));
                if (writes.Count == 25) await FlushAsync(writes);
            }

            // Steps
            for (int i = 0; i < (r.Steps?.Count ?? 0); i++)
            {
                var st = r.Steps![i];
                if (string.IsNullOrWhiteSpace(st.Text)) continue;

                var item = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new($"USER#{r.UserId}"),
                    ["SK"] = new($"RECIPE#{r.RecipeId}#STEP#{i:D4}"),
                    ["Type"] = new("RecipeStep"),
                    ["Text"] = new(st.Text.Trim())
                };
                if (!string.IsNullOrWhiteSpace(st.ImageUrl))
                    item["ImageUrl"] = new(st.ImageUrl!.Trim());

                writes.Add(new WriteRequest(new PutRequest(item)));
                if (writes.Count == 25) await FlushAsync(writes);
            }

            if (writes.Count > 0) await FlushAsync(writes);
        }

        private async Task FlushAsync(List<WriteRequest> batch)
        {
            await _ddb.BatchWriteItemAsync(new BatchWriteItemRequest { RequestItems = new() { [TableName] = batch } });
            batch.Clear();
        }

        private static IEnumerable<List<T>> Chunk<T>(IEnumerable<T> src, int size)
        {
            var buf = new List<T>(size);
            foreach (var x in src)
            {
                buf.Add(x);
                if (buf.Count == size) { yield return buf; buf = new(size); }
            }
            if (buf.Count > 0) yield return buf;
        }
    }
}
