namespace OpenSpace.Repositories
{
    internal interface ITonConnectRepository
    {
        string? GetString(string key, string? defaultValue = null);

        void SetString(string key, string value);

        void DeleteKey(string key);

        bool HasKey(string key);
    }
}
