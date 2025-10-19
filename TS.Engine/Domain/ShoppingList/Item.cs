namespace TS.Engine.Domain.ShoppingList
{
    public class Item
    {
        public string Text { get; private set; }   // Item description
        public bool IsChecked { get; private set; } // Indicates if the item is marked as done

        public Item(string text, bool isChecked = false)
        {
            Text = text.Trim();
            IsChecked = isChecked;
        }

        // Change checked state
        public void Toggle(bool value) => IsChecked = value;
    }
}
