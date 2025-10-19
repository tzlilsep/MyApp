namespace TS.Engine.Contracts
{
    // Represents a single checklist item in data transfer form
    public record ItemDto(string Text, bool IsChecked);

    // Represents a full shopping list for API/service communication
    public record ShoppingListDto(
        string UserId,
        string ListId,
        string Name,
        IReadOnlyList<ItemDto> Items
    );
}
