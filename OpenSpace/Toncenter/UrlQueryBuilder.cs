using System.Text;

namespace OpenSpace.Toncenter
{
    internal sealed class UrlQueryBuilder
    {
        public string BaseUrl { get; }

        private readonly StringBuilder _sb;

        public UrlQueryBuilder(string url)
        {
            BaseUrl = url;
            _sb = new(url);
        }

        public UrlQueryBuilder AddParameter(string name, string? value)
        {
            if (string.IsNullOrEmpty(value))
                return this;

            _sb.Append(_sb.Length == BaseUrl.Length ? '?' : '&')
               .Append(Uri.EscapeDataString(name)).Append('=').Append(Uri.EscapeDataString(value));

            return this;
        }

        public string Build()
        {
            return _sb.ToString();
        }
    }
}
