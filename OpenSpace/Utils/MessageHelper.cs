using System.Text;

namespace OpenSpace.Utils
{
    internal static class MessageHelper
    {
        public static string EscapeMarkdown(string text)
        {
            StringBuilder sb = new(text);
            sb.Replace("_", "\\_")
              .Replace("*", "\\*")
              .Replace("[", "\\[")
              .Replace("]", "\\]")
              .Replace("(", "\\(")
              .Replace(")", "\\)")
              .Replace("~", "\\~")
              .Replace("`", "\\`")
              .Replace(">", "\\>")
              .Replace("#", "\\#")
              .Replace("+", "\\+")
              .Replace("-", "\\-")
              .Replace("=", "\\=")
              .Replace("|", "\\|")
              .Replace("{", "\\{")
              .Replace("}", "\\}")
              .Replace(".", "\\.")
              .Replace("!", "\\!");
            return sb.ToString();
        }
    }
}
