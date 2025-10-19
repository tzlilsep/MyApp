using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using TS.Engine.Abstractions;
using TS.Engine.Contracts;

namespace TS.AWS;

public sealed class AwsShoppingListService : IShoppingListService
{
    private readonly IAmazonDynamoDB _ddb;
    private const string TableName = "AppData";

    public AwsShoppingListService(string idToken)
    {
        _ddb = AwsClientsFactory.CreateDynamoDbFromIdToken(idToken);
    }

    public async Task<IReadOnlyList<(string ListId, string Name)>> GetListsAsync(string userId)
    {
        var resp = await _ddb.QueryAsync(new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            ExpressionAttributeValues = new()
            {
                [":pk"] = new($"USER#{userId}"),
                [":sk"] = new("LIST#")
            }
        });

        // Filter only "List" items and map to (ListId, Name)
        return resp.Items
            .Where(i => i.TryGetValue("Type", out var t) && t.S == "List")
            .Select(i => (
                ListId: i["SK"].S.Replace("LIST#", string.Empty),
                Name: i.TryGetValue("ListName", out var n) ? n.S : "רשימה"
            ))
            .ToList();
    }

    public async Task CreateListAsync(string userId, string listId, string name)
    {
        // Conditional put to avoid overwriting existing list (PK+SK must not exist)
        await _ddb.PutItemAsync(new PutItemRequest
        {
            TableName = TableName,
            Item = new()
            {
                ["PK"] = new($"USER#{userId}"),
                ["SK"] = new($"LIST#{listId}"),
                ["Type"] = new("List"),
                ["ListName"] = new(name),
                ["UpdatedAt"] = new(DateTime.UtcNow.ToString("o"))
            },
            ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)"
        });
    }

    public async Task DeleteListAsync(string userId, string listId)
    {
        // Delete all rows with SK starting with LIST#{listId} (header + items)
        var q = await _ddb.QueryAsync(new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            ExpressionAttributeValues = new()
            {
                [":pk"] = new($"USER#{userId}"),
                [":sk"] = new($"LIST#{listId}")
            },
            ProjectionExpression = "PK, SK"
        });

        // BatchWrite limit is 25 requests per call
        var batch = new List<WriteRequest>();
        foreach (var it in q.Items)
        {
            batch.Add(new WriteRequest(new DeleteRequest(new()
            {
                ["PK"] = it["PK"],
                ["SK"] = it["SK"]
            })));

            if (batch.Count == 25)
            {
                await _ddb.BatchWriteItemAsync(new BatchWriteItemRequest { RequestItems = new() { [TableName] = batch } });
                batch.Clear();
            }
        }
        if (batch.Count > 0)
            await _ddb.BatchWriteItemAsync(new BatchWriteItemRequest { RequestItems = new() { [TableName] = batch } });
    }

    public Task ShoppingListExistsOrThrow(string userId, string listId) => Task.CompletedTask; // reserved for future use

    public async Task<ShoppingListDto> LoadAsync(string userId, string listId)
    {
        var resp = await _ddb.QueryAsync(new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            ExpressionAttributeValues = new()
            {
                [":pk"] = new($"USER#{userId}"),
                [":sk"] = new($"LIST#{listId}")
            }
        });

        var name = "רשימה";
        var items = new List<ItemDto>();

        foreach (var av in resp.Items)
        {
            if (!av.TryGetValue("Type", out var t)) continue;

            if (t.S == "List")
                name = av.TryGetValue("ListName", out var n) ? n.S : name;
            else if (t.S == "ListItem")
            {
                var text = av.TryGetValue("Text", out var txt) ? txt.S : "";
                var isChecked = av.TryGetValue("IsChecked", out var chk) && (chk.BOOL ?? false);
                if (!string.IsNullOrWhiteSpace(text))
                    items.Add(new ItemDto(text, isChecked));
            }
        }

        return new ShoppingListDto(userId, listId, name, items);
    }

    public async Task SaveAsync(ShoppingListDto list)
    {
        // Remove all current items (ITEM#) to rewrite fresh state
        var existing = await _ddb.QueryAsync(new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            ExpressionAttributeValues = new()
            {
                [":pk"] = new($"USER#{list.UserId}"),
                [":sk"] = new($"LIST#{list.ListId}#ITEM#")
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

        // Upsert list header
        await _ddb.PutItemAsync(new PutItemRequest
        {
            TableName = TableName,
            Item = new()
            {
                ["PK"] = new($"USER#{list.UserId}"),
                ["SK"] = new($"LIST#{list.ListId}"),
                ["Type"] = new("List"),
                ["ListName"] = new(list.Name),
                ["UpdatedAt"] = new(DateTime.UtcNow.ToString("o"))
            }
        });

        // Write items (ordered keys via index, zero-padded)
        var puts = new List<WriteRequest>();
        for (int i = 0; i < list.Items.Count; i++)
        {
            var it = list.Items[i];
            if (string.IsNullOrWhiteSpace(it.Text)) continue;

            puts.Add(new WriteRequest(new PutRequest(new()
            {
                ["PK"] = new($"USER#{list.UserId}"),
                ["SK"] = new($"LIST#{list.ListId}#ITEM#{i:D4}"),
                ["Type"] = new("ListItem"),
                ["Text"] = new(it.Text),
                ["IsChecked"] = new AttributeValue { BOOL = it.IsChecked }
            })));

            if (puts.Count == 25)
            {
                await _ddb.BatchWriteItemAsync(new BatchWriteItemRequest { RequestItems = new() { [TableName] = puts } });
                puts.Clear();
            }
        }
        if (puts.Count > 0)
            await _ddb.BatchWriteItemAsync(new BatchWriteItemRequest { RequestItems = new() { [TableName] = puts } });
    }

    // Small helper to split sequences into batches (e.g., for BatchWrite 25 limit)
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
