namespace OpenSpace.Utils
{
    internal readonly struct ParserResult<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? Error { get; }

        private ParserResult(bool isSuccess, T? value, string? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static ParserResult<T> WithValue(T value)
        {
            return new(true, value, null);
        }

        public static ParserResult<T> WithError(string error)
        {
            return new(false, default, error);
        }
    }
}
