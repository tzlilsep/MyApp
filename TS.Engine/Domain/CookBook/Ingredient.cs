namespace TS.Engine.Domain.CookBook
{
    /// <summary>מצרך חופשי (פשוט: טקסט אחד). נשכלל בהמשך לכמויות/יחידות.</summary>
    public sealed class Ingredient
    {
        public string Text { get; private set; }
        public Ingredient(string text) => Text = (text ?? string.Empty).Trim();

        public void Rename(string text)
        {
            var t = (text ?? string.Empty).Trim();
            if (t.Length > 0) Text = t;
        }
    }
}
