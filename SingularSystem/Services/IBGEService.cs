using SingularSystem.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace SingularSystem.Services
{
    public class IBGEService : IIBGEService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private const string CacheKeyPrefix = "Cities_";

        public IBGEService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://servicodados.ibge.gov.br/api/v1/localidades/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SingularSystem-App");
            _cache = cache;
        }

        public async Task<IEnumerable<City>> GetCitiesByState(string uf)
        {
            var cacheKey = $"{CacheKeyPrefix}{uf.ToUpper()}";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<City>? cachedCities))
            {
                return cachedCities ?? Enumerable.Empty<City>();
            }

            try
            {
                var response = await _httpClient.GetAsync($"estados/{uf}/municipios");
                response.EnsureSuccessStatusCode();

                var cities = await response.Content.ReadFromJsonAsync<IEnumerable<City>>();
                var result = cities ?? Enumerable.Empty<City>();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromDays(1))
                    .SetAbsoluteExpiration(TimeSpan.FromDays(7));

                _cache.Set(cacheKey, result, cacheOptions);

                return result;
            }
            catch (HttpRequestException ex)
            {
                throw new ApplicationException($"Erro ao consultar municípios no IBGE para a UF {uf}: {ex.Message}", ex);
            }
        }
    }

    public class City
    {
        public int ID { get; set; }
        public string Nome { get; set; } = string.Empty;
    }
}