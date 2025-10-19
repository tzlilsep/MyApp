namespace TS.Engine.Domain.ShoppingList
{
    public sealed class ShoppingList
    {
        public string UserId { get; }
        public string ListId { get; }     // Unique list ID (Guid or custom)
        public string Name { get; private set; }

        private readonly List<Item> _items = new();
        public IReadOnlyList<Item> Items => _items;

        public ShoppingList(string userId, string listId, string name)
        {
            UserId = userId;
            ListId = listId;
            Name = name;
        }

        // Update list name if valid
        public void Rename(string newName)
        {
            if (!string.IsNullOrWhiteSpace(newName))
                Name = newName.Trim();
        }

        // Add a new item
        public void AddItem(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                _items.Add(new Item(text.Trim()));
        }

        // Remove item by index (safe check)
        public void RemoveAt(int index)
        {
            if (index >= 0 && index < _items.Count)
                _items.RemoveAt(index);
        }
    }
}
