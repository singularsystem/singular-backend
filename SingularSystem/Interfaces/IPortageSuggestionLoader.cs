using SingularSystem.Extensions;
using SingularSystem.Models;
using SingularSystem.Models.Enums;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace SingularSystem.Interfaces
{
    public interface IPortageSuggestionLoader
    {
        IReadOnlyDictionary<string, PortageQuestion> LoadBank(AgeRange ageRange);

        string? GetSuggestion(
            AgeRange ageRange,
            string key,
            PlanResponse response);
    }

    public sealed class PortageSuggestionLoader : IPortageSuggestionLoader
    {
        private const string CacheKey = "portage_suggestions_json";
        private readonly IMemoryCache _cache;
        private readonly ILogger<PortageSuggestionLoader> _logger;

        public PortageSuggestionLoader(IMemoryCache cache, ILogger<PortageSuggestionLoader> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public IReadOnlyDictionary<string, PortageQuestion> LoadBank(AgeRange ageRange)
        {
            var data = LoadAll();

            var display = ageRange.GetDisplayName();
            if (string.IsNullOrWhiteSpace(display))
                return new Dictionary<string, PortageQuestion>();

            if (!data.TryGetValue(display, out var list) || list == null || list.Count == 0)
                return new Dictionary<string, PortageQuestion>();

            var dict = new Dictionary<string, PortageQuestion>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < list.Count; i++)
            {
                var key = (i + 1).ToString();
                dict[key] = list[i];
            }

            return dict;
        }

        public string? GetSuggestion(AgeRange ageRange, string key, PlanResponse response)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            if (response != PlanResponse.NotYet && response != PlanResponse.WithAssistence)
                return null;

            var bank = LoadBank(ageRange);

            if (!bank.TryGetValue(key.Trim(), out var q) || q == null)
                return null;

            return response == PlanResponse.NotYet
                ? q.SuggestionNotYet
                : q.SuggestionWithAssistance;
        }

        private Dictionary<string, List<PortageQuestion>> LoadAll()
        {
            return _cache.GetOrCreate(CacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(6);

                try
                {
                    var json = File.ReadAllText("Resources/portageData.json");

                    var data = JsonSerializer.Deserialize<Dictionary<string, List<PortageQuestion>>>(json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    return data ?? new Dictionary<string, List<PortageQuestion>>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao carregar Resources/portageData.json");
                    return new Dictionary<string, List<PortageQuestion>>();
                }
            })!;
        }
    }
}
