using System.Text.Json;
using TournamentSystem.Models;

namespace TournamentSystem.Services
{
    public class RiotApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RiotApiService> _logger;
        private readonly string _apiKey;

        // Regiões disponíveis
        private readonly Dictionary<string, string> _regions = new()
        {
            {"americas", "americas.api.riotgames.com"},
            {"asia", "asia.api.riotgames.com"},
            {"europe", "europe.api.riotgames.com"},
            {"br1", "br1.api.riotgames.com"},
            {"na1", "na1.api.riotgames.com"},
            {"euw1", "euw1.api.riotgames.com"}
        };

        public RiotApiService(HttpClient httpClient, IConfiguration configuration, ILogger<RiotApiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["RiotApi:ApiKey"] ?? "";
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Log para debug
            _logger.LogInformation($"RiotApiService inicializado. API Key configurada: {(!string.IsNullOrEmpty(_apiKey) ? "SIM" : "NÃO")}");
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogInformation($"API Key length: {_apiKey.Length} chars");
            }
        }

        #region ACCOUNT-V1 APIs

        /// <summary>
        /// Get account by PUUID
        /// </summary>
        public async Task<RiotAccount> GetAccountByPuuidAsync(string puuid, string region = "americas")
        {
            try
            {
                var baseUrl = _regions.GetValueOrDefault(region, _regions["americas"]);
                var url = $"https://{baseUrl}/riot/account/v1/accounts/by-puuid/{puuid}?api_key={_apiKey}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to get account by PUUID {puuid}: {response.StatusCode}");
                    return new RiotAccount { IsValid = false, ErrorMessage = "Conta não encontrada" };
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                return new RiotAccount
                {
                    IsValid = true,
                    Puuid = data.GetProperty("puuid").GetString(),
                    GameName = data.GetProperty("gameName").GetString(),
                    TagLine = data.GetProperty("tagLine").GetString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting account by PUUID {puuid}");
                return new RiotAccount { IsValid = false, ErrorMessage = "Erro na API" };
            }
        }

        /// <summary>
        /// Get account by Riot ID
        /// </summary>
        public async Task<RiotAccount> GetAccountByRiotIdAsync(string gameName, string tagLine, string region = "americas")
        {
            try
            {
                var baseUrl = _regions.GetValueOrDefault(region, _regions["americas"]);
                var url = $"https://{baseUrl}/riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}?api_key={_apiKey}";

                _logger.LogInformation($"Chamando API Riot: {url.Replace(_apiKey, "***")}");

                var response = await _httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to get account by Riot ID {gameName}#{tagLine}: {response.StatusCode} - Response: {responseContent}");

                    var errorMessage = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.NotFound => "Riot ID não encontrado",
                        System.Net.HttpStatusCode.Unauthorized => "Chave da API inválida",
                        System.Net.HttpStatusCode.Forbidden => "API Key sem permissão ou expirada",
                        System.Net.HttpStatusCode.TooManyRequests => "Muitas requisições - tente novamente em alguns segundos",
                        _ => $"Erro na API: {response.StatusCode}"
                    };

                    return new RiotAccount { IsValid = false, ErrorMessage = errorMessage };
                }

                var data = JsonSerializer.Deserialize<JsonElement>(responseContent);

                return new RiotAccount
                {
                    IsValid = true,
                    Puuid = data.GetProperty("puuid").GetString(),
                    GameName = data.GetProperty("gameName").GetString(),
                    TagLine = data.GetProperty("tagLine").GetString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting account by Riot ID {gameName}#{tagLine}");
                return new RiotAccount { IsValid = false, ErrorMessage = "Erro na API" };
            }
        }

        /// <summary>
        /// Get active shard for a player
        /// </summary>
        public async Task<PlayerActiveShard> GetActiveShardAsync(string puuid, string game, string region = "americas")
        {
            try
            {
                var baseUrl = _regions.GetValueOrDefault(region, _regions["americas"]);
                var url = $"https://{baseUrl}/riot/account/v1/active-shards/by-game/{game}/by-puuid/{puuid}?api_key={_apiKey}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to get active shard for {puuid} in {game}: {response.StatusCode}");
                    return new PlayerActiveShard { IsValid = false };
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                return new PlayerActiveShard
                {
                    IsValid = true,
                    Puuid = data.GetProperty("puuid").GetString(),
                    Game = data.GetProperty("game").GetString(),
                    ShardName = data.GetProperty("activeShard").GetString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting active shard for {puuid}");
                return new PlayerActiveShard { IsValid = false };
            }
        }

        #endregion

        #region LEAGUE OF LEGENDS APIs

        /// <summary>
        /// Get summoner by name (DEPRECATED - use GetSummonerByPuuidAsync)
        /// </summary>
        public async Task<LoLSummoner> GetSummonerByNameAsync(string summonerName, string region = "br1")
        {
            try
            {
                var baseUrl = _regions.GetValueOrDefault(region, _regions["br1"]);
                var url = $"https://{baseUrl}/lol/summoner/v4/summoners/by-name/{Uri.EscapeDataString(summonerName)}?api_key={_apiKey}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Summoner not found: {summonerName} - Status: {response.StatusCode}");
                    return new LoLSummoner { IsValid = false, ErrorMessage = $"Invocador '{summonerName}' não encontrado" };
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                return new LoLSummoner
                {
                    IsValid = true,
                    Id = data.GetProperty("id").GetString(),
                    AccountId = data.GetProperty("accountId").GetString(),
                    Puuid = data.GetProperty("puuid").GetString(),
                    Name = data.GetProperty("name").GetString(),
                    ProfileIconId = data.GetProperty("profileIconId").GetInt32(),
                    RevisionDate = data.GetProperty("revisionDate").GetInt64(),
                    SummonerLevel = data.GetProperty("summonerLevel").GetInt32()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting summoner {summonerName}");
                return new LoLSummoner { IsValid = false, ErrorMessage = "Erro na API" };
            }
        }

        /// <summary>
        /// Get summoner by PUUID (RECOMMENDED)
        /// </summary>
        public async Task<LoLSummoner> GetSummonerByPuuidAsync(string puuid, string region = "br1")
        {
            try
            {
                var baseUrl = _regions.GetValueOrDefault(region, _regions["br1"]);
                var url = $"https://{baseUrl}/lol/summoner/v4/summoners/by-puuid/{puuid}?api_key={_apiKey}";

                _logger.LogInformation($"Buscando summoner por PUUID: {url}");

                var response = await _httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Summoner not found by PUUID: {puuid} - Status: {response.StatusCode} - Response: {responseContent}");

                    var errorMessage = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.NotFound => "Jogador não encontrado neste servidor",
                        System.Net.HttpStatusCode.Unauthorized => "Chave da API inválida",
                        System.Net.HttpStatusCode.Forbidden => "API Key sem permissão ou expirada",
                        System.Net.HttpStatusCode.TooManyRequests => "Muitas requisições - tente novamente em alguns segundos",
                        _ => $"Erro na API: {response.StatusCode}"
                    };

                    return new LoLSummoner { IsValid = false, ErrorMessage = errorMessage };
                }

                var data = JsonSerializer.Deserialize<JsonElement>(responseContent);

                return new LoLSummoner
                {
                    IsValid = true,
                    Id = data.GetProperty("id").GetString(),
                    AccountId = data.GetProperty("accountId").GetString(),
                    Puuid = data.GetProperty("puuid").GetString(),
                    Name = data.GetProperty("name").GetString(),
                    ProfileIconId = data.GetProperty("profileIconId").GetInt32(),
                    RevisionDate = data.GetProperty("revisionDate").GetInt64(),
                    SummonerLevel = data.GetProperty("summonerLevel").GetInt32()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting summoner by PUUID {puuid}");
                return new LoLSummoner { IsValid = false, ErrorMessage = $"Erro na API: {ex.Message}" };
            }
        }

        /// <summary>
        /// Get ranked info for summoner
        /// </summary>
        public async Task<List<LoLRankedEntry>> GetRankedInfoAsync(string summonerId, string region = "br1")
        {
            try
            {
                var baseUrl = _regions.GetValueOrDefault(region, _regions["br1"]);
                var url = $"https://{baseUrl}/lol/league/v4/entries/by-summoner/{summonerId}?api_key={_apiKey}";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Ranked info not found for summoner: {summonerId}");
                    return new List<LoLRankedEntry>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var dataArray = JsonSerializer.Deserialize<JsonElement[]>(json);

                var entries = new List<LoLRankedEntry>();

                if (dataArray != null)
                {
                    foreach (var data in dataArray)
                    {
                        entries.Add(new LoLRankedEntry
                        {
                            LeagueId = data.GetProperty("leagueId").GetString(),
                            SummonerId = data.GetProperty("summonerId").GetString(),
                            SummonerName = data.GetProperty("summonerName").GetString(),
                            QueueType = data.GetProperty("queueType").GetString(),
                            Tier = data.GetProperty("tier").GetString(),
                            Rank = data.GetProperty("rank").GetString(),
                            LeaguePoints = data.GetProperty("leaguePoints").GetInt32(),
                            Wins = data.GetProperty("wins").GetInt32(),
                            Losses = data.GetProperty("losses").GetInt32(),
                            HotStreak = data.GetProperty("hotStreak").GetBoolean(),
                            Veteran = data.GetProperty("veteran").GetBoolean(),
                            FreshBlood = data.GetProperty("freshBlood").GetBoolean(),
                            Inactive = data.GetProperty("inactive").GetBoolean()
                        });
                    }
                }

                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting ranked info for {summonerId}");
                return new List<LoLRankedEntry>();
            }
        }

        #endregion

        #region COMPLETE PLAYER INFO

        /// <summary>
        /// Get complete player information
        /// </summary>
        public async Task<CompletePlayerInfo> GetCompletePlayerInfoAsync(string playerIdentifier, GameType gameType, string region = "br1")
        {
            try
            {
                var result = new CompletePlayerInfo { GameType = gameType };

                if (gameType == GameType.LeagueOfLegends)
                {
                    // Usar SEMPRE o mesmo padrão do Valorant - apenas Account API
                    if (!playerIdentifier.Contains('#'))
                    {
                        result.ErrorMessage = "Formato de Riot ID inválido. Use: GameName#Tag (ex: SeuNome#BR1)";
                        return result;
                    }

                    var parts = playerIdentifier.Split('#');
                    if (parts.Length != 2)
                    {
                        result.ErrorMessage = "Formato de Riot ID inválido. Use: GameName#Tag";
                        return result;
                    }

                    var gameName = parts[0].Trim();
                    var tagLine = parts[1].Trim();

                    // Get Riot account (IGUAL AO VALORANT)
                    result.RiotAccount = await GetAccountByRiotIdAsync(gameName, tagLine, "americas");
                    if (!result.RiotAccount.IsValid)
                    {
                        result.ErrorMessage = result.RiotAccount.ErrorMessage;
                        return result;
                    }

                    result.IsValid = true;
                    _logger.LogInformation($"Conta Riot encontrada. PUUID: {result.RiotAccount.Puuid}");

                    // Buscar informações do summoner no servidor especificado
                    if (!string.IsNullOrEmpty(result.RiotAccount.Puuid))
                    {
                        _logger.LogInformation($"Buscando summoner no servidor {region}...");
                        result.SummonerInfo = await GetSummonerByPuuidAsync(result.RiotAccount.Puuid, region);

                        if (result.SummonerInfo.IsValid)
                        {
                            _logger.LogInformation($"Summoner encontrado: {result.SummonerInfo.Name} (Level {result.SummonerInfo.SummonerLevel})");

                            // Get ranked info
                            _logger.LogInformation($"Buscando informações de rank...");
                            result.RankedEntries = await GetRankedInfoAsync(result.SummonerInfo.Id ?? "", region);
                            _logger.LogInformation($"Encontradas {result.RankedEntries.Count} entradas de rank");
                        }
                        else
                        {
                            _logger.LogWarning($"Summoner não encontrado no servidor {region}. Tentando outros servidores...");

                            // Tentar outros servidores brasileiros/americanos comuns
                            var serversToTry = new[] { "na1", "lan1", "las1" };

                            foreach (var server in serversToTry)
                            {
                                _logger.LogInformation($"Tentando servidor {server}...");
                                result.SummonerInfo = await GetSummonerByPuuidAsync(result.RiotAccount.Puuid, server);

                                if (result.SummonerInfo.IsValid)
                                {
                                    _logger.LogInformation($"Summoner encontrado no servidor {server}: {result.SummonerInfo.Name}");
                                    result.RankedEntries = await GetRankedInfoAsync(result.SummonerInfo.Id ?? "", server);
                                    break;
                                }
                            }

                            if (!result.SummonerInfo.IsValid)
                            {
                                _logger.LogWarning("Summoner não encontrado em nenhum servidor testado");
                            }
                        }
                    }
                }
                else if (gameType == GameType.Valorant)
                {
                    // Parse Riot ID
                    if (!playerIdentifier.Contains('#'))
                    {
                        result.ErrorMessage = "Formato de Riot ID inválido. Use: GameName#Tag";
                        return result;
                    }

                    var parts = playerIdentifier.Split('#');
                    if (parts.Length != 2)
                    {
                        result.ErrorMessage = "Formato de Riot ID inválido. Use: GameName#Tag";
                        return result;
                    }

                    var gameName = parts[0].Trim();
                    var tagLine = parts[1].Trim();

                    // Get Riot account
                    result.RiotAccount = await GetAccountByRiotIdAsync(gameName, tagLine, "americas");
                    if (!result.RiotAccount.IsValid)
                    {
                        result.ErrorMessage = result.RiotAccount.ErrorMessage;
                        return result;
                    }

                    result.IsValid = true;

                    // Get active shard for Valorant
                    result.ActiveShard = await GetActiveShardAsync(result.RiotAccount.Puuid ?? "", "val", "americas");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting complete player info for {playerIdentifier}");
                return new CompletePlayerInfo
                {
                    IsValid = false,
                    ErrorMessage = "Erro ao recuperar informações do jogador",
                    GameType = gameType
                };
            }
        }

        #endregion

        #region LEGACY METHODS

        public async Task<PlayerInfo> GetLeagueOfLegendsPlayerAsync(string playerIdentifier)
        {
            var completeInfo = await GetCompletePlayerInfoAsync(playerIdentifier, GameType.LeagueOfLegends);

            if (!completeInfo.IsValid)
            {
                return new PlayerInfo
                {
                    IsValid = false,
                    ErrorMessage = completeInfo.ErrorMessage
                };
            }

            var soloQueue = completeInfo.RankedEntries.FirstOrDefault(r => r.QueueType == "RANKED_SOLO_5x5");

            return new PlayerInfo
            {
                IsValid = true,
                PlayerName = completeInfo.DisplayName,
                Game = "League of Legends",
                Level = completeInfo.SummonerInfo?.SummonerLevel ?? 1, // Default para 1 se não encontrar
                Tier = soloQueue?.Tier ?? "UNRANKED",
                Rank = soloQueue?.Rank ?? "",
                LeaguePoints = soloQueue?.LeaguePoints ?? 0,
                Wins = soloQueue?.Wins ?? 0,
                Losses = soloQueue?.Losses ?? 0
            };
        }

        public async Task<PlayerInfo> GetValorantPlayerAsync(string riotId)
        {
            var completeInfo = await GetCompletePlayerInfoAsync(riotId, GameType.Valorant);

            return new PlayerInfo
            {
                IsValid = completeInfo.IsValid,
                ErrorMessage = completeInfo.ErrorMessage,
                PlayerName = riotId,
                Game = "Valorant",
                Tier = completeInfo.IsValid ? "Verificado" : "Erro",
                Rank = completeInfo.IsValid ? "Conta Encontrada" : "Não Encontrada",
                Level = 0,
                LeaguePoints = 0
            };
        }

        #endregion
    }

    #region DATA MODELS

    public class RiotAccount
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string? Puuid { get; set; }
        public string? GameName { get; set; }
        public string? TagLine { get; set; }
        public string FullRiotId => $"{GameName}#{TagLine}";
    }

    public class LoLSummoner
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string? Id { get; set; }
        public string? AccountId { get; set; }
        public string? Puuid { get; set; }
        public string? Name { get; set; }
        public int ProfileIconId { get; set; }
        public long RevisionDate { get; set; }
        public int SummonerLevel { get; set; }
    }

    public class LoLRankedEntry
    {
        public string? LeagueId { get; set; }
        public string? SummonerId { get; set; }
        public string? SummonerName { get; set; }
        public string? QueueType { get; set; }
        public string? Tier { get; set; }
        public string? Rank { get; set; }
        public int LeaguePoints { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public bool HotStreak { get; set; }
        public bool Veteran { get; set; }
        public bool FreshBlood { get; set; }
        public bool Inactive { get; set; }

        public string FormattedRank =>
            Tier == "UNRANKED" ? "Não Ranqueado" : $"{FormatTier(Tier)} {Rank} ({LeaguePoints} LP)";

        public double WinRate =>
            (Wins + Losses) == 0 ? 0 : Math.Round((double)Wins / (Wins + Losses) * 100, 1);

        private string FormatTier(string? tier) => tier?.ToUpper() switch
        {
            "IRON" => "Ferro",
            "BRONZE" => "Bronze",
            "SILVER" => "Prata",
            "GOLD" => "Ouro",
            "PLATINUM" => "Platina",
            "DIAMOND" => "Diamante",
            "MASTER" => "Mestre",
            "GRANDMASTER" => "Grão-Mestre",
            "CHALLENGER" => "Desafiante",
            _ => tier ?? "Não Ranqueado"
        };
    }

    public class PlayerActiveShard
    {
        public bool IsValid { get; set; }
        public string? Puuid { get; set; }
        public string? Game { get; set; }
        public string? ShardName { get; set; }
    }

    public class CompletePlayerInfo
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
        public GameType GameType { get; set; }

        // Riot Account Info
        public RiotAccount RiotAccount { get; set; } = new();

        // League of Legends specific
        public LoLSummoner? SummonerInfo { get; set; }
        public List<LoLRankedEntry> RankedEntries { get; set; } = new();

        // Valorant specific
        public PlayerActiveShard? ActiveShard { get; set; }

        // Helper properties
        public LoLRankedEntry? SoloQueueRank =>
            RankedEntries.FirstOrDefault(r => r.QueueType == "RANKED_SOLO_5x5");

        public LoLRankedEntry? FlexQueueRank =>
            RankedEntries.FirstOrDefault(r => r.QueueType == "RANKED_FLEX_SR");

        public string DisplayName => GameType == GameType.LeagueOfLegends
            ? RiotAccount.FullRiotId
            : RiotAccount.FullRiotId;

        public string PrimaryRank => GameType == GameType.LeagueOfLegends
            ? (SoloQueueRank?.FormattedRank ?? "Não Ranqueado")
            : "Jogador Valorant";
    }

    // Legacy model for backwards compatibility
    public class PlayerInfo
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string PlayerName { get; set; } = "";
        public string Game { get; set; } = "";
        public string Tier { get; set; } = "UNRANKED";
        public string Rank { get; set; } = "";
        public int LeaguePoints { get; set; } = 0;
        public int Level { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;

        public string FormattedRank
        {
            get
            {
                if (Tier == "UNRANKED" || string.IsNullOrEmpty(Tier))
                    return "Não Ranqueado";

                if (string.IsNullOrEmpty(Rank))
                    return FormatTierName(Tier);

                return $"{FormatTierName(Tier)} {Rank} ({LeaguePoints} LP)";
            }
        }

        public string WinRate
        {
            get
            {
                var totalGames = Wins + Losses;
                if (totalGames == 0) return "0%";
                return $"{(Wins * 100 / totalGames)}%";
            }
        }

        private string FormatTierName(string tier)
        {
            return tier.ToUpper() switch
            {
                "IRON" => "Ferro",
                "BRONZE" => "Bronze",
                "SILVER" => "Prata",
                "GOLD" => "Ouro",
                "PLATINUM" => "Platina",
                "DIAMOND" => "Diamante",
                "MASTER" => "Mestre",
                "GRANDMASTER" => "Grão-Mestre",
                "CHALLENGER" => "Desafiante",
                _ => tier
            };
        }
    }

    #endregion
}