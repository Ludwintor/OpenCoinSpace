using System.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenSpace.Logging;
using OpenSpace.Toncenter.Entities;

namespace OpenSpace.Toncenter
{
    internal sealed class ToncenterClient
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public ToncenterClient(HttpClient client, ILogger logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<JettonWallet?> GetJettonWalletAsync(string owner, string jettonMaster)
        {
            UrlQueryBuilder builder = new("jetton/wallets");
            builder.AddParameter("owner_address", owner);
            builder.AddParameter("jetton_address", jettonMaster);
            string content = await ExecuteGetRequestAsync(builder.Build()).ConfigureAwait(false);
            JettonWallet? wallet = JsonConvert.DeserializeObject<JettonWallet[]>(content)?.SingleOrDefault() ?? null;
            if (wallet != null)
                _logger.LogDebug(LogEvents.Toncenter, "{Address} wallet found. Owner {Owner}", wallet.Value.Address, owner);
            else
                _logger.LogDebug(LogEvents.Toncenter, "Jetton wallet not found. Owner {Owner}", owner);
            return wallet;
        }

        public async Task<NftItem[]> GetNftItemsAsync(string collection, string? owner = null, ulong? limit = null, ulong? offset = null)
        {
            UrlQueryBuilder builder = new("nft/items");
            builder.AddParameter("collection_address", collection);
            builder.AddParameter("owner_address", owner);
            if (limit != null)
                builder.AddParameter("limit", limit.Value.ToString());
            if (offset != null)
                builder.AddParameter("offset", offset.Value.ToString());
            string content = await ExecuteGetRequestAsync(builder.Build()).ConfigureAwait(false);
            NftItem[] items = JsonConvert.DeserializeObject<NftItem[]>(content) ?? [];
            _logger.LogDebug(LogEvents.Toncenter, "{Length} nft items found", items.Length.ToString());
            return items;
        }

        private async Task<string> ExecuteGetRequestAsync(string url)
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync(url).ConfigureAwait(false);
                string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogTrace(LogEvents.RestRecv, "{Content}", content);
                HttpStatusCode statusCode = response.StatusCode;
                if (statusCode != HttpStatusCode.OK)
                    throw new HttpRequestException("Bad status code", null, statusCode);
                return content;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(LogEvents.RestError, ex, "Request to {Url} failed", $"{_client.BaseAddress}{url}");
                throw;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(LogEvents.RestError, ex, "Request to {Url} timed out", $"{_client.BaseAddress}{url}");
                throw;
            }
        }
    }
}
