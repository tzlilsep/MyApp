namespace TS.Engine.Domain.CookBook
{
    /// <summary>
    /// שלב בהכנת המתכון — טקסט ותמונה אופציונלית.
    /// (ללא זמן פרטני; זמן כולל יש במתכון עצמו)
    /// </summary>
    public sealed class Step
    {
        public string Text { get; private set; }
        public string? ImageUrl { get; private set; }

        public Step(string text, string? imageUrl = null)
        {
            Text = (text ?? string.Empty).Trim();
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        }

        public void Edit(string text, string? imageUrl = null)
        {
            var cleanText = (text ?? string.Empty).Trim();
            if (cleanText.Length > 0)
                Text = cleanText;

            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        }
    }
}
