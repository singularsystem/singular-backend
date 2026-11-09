using SingularSystem.Models.Enums;
using SingularSystem.Services;
using System.Collections.Concurrent;
using System.Text.Json;
using static SingularSystem.Services.PEIPDFGenerator;

namespace SingularSystem.Interfaces
{
    public interface IPEISuggestionLoader
    {
        IReadOnlyDictionary<string, PEISuggestionQuestion> Load(Subject subject, Serie serie);
    }

    public sealed class PEISuggestionLoader : IPEISuggestionLoader
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PEISuggestionLoader> _logger;

        private readonly ConcurrentDictionary<(Subject, Serie), IReadOnlyDictionary<string, PEISuggestionQuestion>> _cache = new();

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PEISuggestionLoader(IWebHostEnvironment env, ILogger<PEISuggestionLoader> logger)
        {
            _env = env;
            _logger = logger;
        }

        public IReadOnlyDictionary<string, PEISuggestionQuestion> Load(Subject subject, Serie serie)
        {
            return _cache.GetOrAdd((subject, serie), key =>
            {
                var path = PEISuggestionPathResolver.Resolve(_env.ContentRootPath, subject, serie);

                if (!File.Exists(path))
                {
                    _logger.LogWarning("Arquivo de sugestões não encontrado: {Path}", path);
                    return new Dictionary<string, PEISuggestionQuestion>(StringComparer.OrdinalIgnoreCase);
                }

                try
                {
                    var json = File.ReadAllText(path);
                    var file = JsonSerializer.Deserialize<PEISuggestionFile>(json, JsonOpts);

                    var list = file?.Questions ?? new List<PEISuggestionQuestion>();

                    return list.ToDictionary(x => x.Id, x => x, StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao ler sugestões: {Path}", path);
                    return new Dictionary<string, PEISuggestionQuestion>(StringComparer.OrdinalIgnoreCase);
                }
            });
        }
    }
}
