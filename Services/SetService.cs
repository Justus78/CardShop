using api.DTOs.Sets;
using api.Exceptions;
using api.Models;
using CardShop.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace api.Services
{

    public interface ISetService
    {
        Task SyncSetsAsync();
        Task<List<Set>> GetAllAsync();
    }

    public class SetService : ISetService
    {
        private const string ScryfallSetsUrl = "https://api.scryfall.com/sets";

        // Shared by every instance in this process, so two sync requests at the same time
        // can't both insert the same new sets. (The service itself is created per request,
        // which is why the lock has to be static.) If the app ever runs on several servers,
        // a unique index on Sets.Code is what protects against duplicates across them.
        private static readonly SemaphoreSlim SyncLock = new(1, 1);

        // The public set list is cached, because it is read on every page by anonymous
        // visitors but only changes when an admin runs a sync.
        private const string SetsCacheKey = "sets:all";
        private static readonly TimeSpan SetsCacheDuration = TimeSpan.FromMinutes(10);

        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SetService> _logger;

        public SetService(HttpClient httpClient, ApplicationDbContext context, IMemoryCache cache, ILogger<SetService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task SyncSetsAsync()
        {
            // Wait(0) = "don't wait": if a sync is already running, refuse immediately.
            // ConflictException becomes a 409 through the global exception middleware.
            if (!await SyncLock.WaitAsync(0))
            {
                throw new ConflictException("A set sync is already running. Please wait for it to finish.");
            }

            try
            {
                var response = await FetchSetsFromScryfallAsync();

                // ONE query for the existing sets, then matching happens in memory.
                // Before, every set from Scryfall (about a thousand) caused its own
                // database round trip.
                var existingSets = await _context.Sets.ToListAsync();

                var existingByCode = existingSets
                    .Where(x => !string.IsNullOrEmpty(x.Code))
                    .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var duplicateRows = existingSets.Count(x => !string.IsNullOrEmpty(x.Code)) - existingByCode.Count;
                if (duplicateRows > 0)
                {
                    _logger.LogWarning(
                        "The Sets table contains {DuplicateRows} duplicate set code row(s). Only the first of each is updated. " +
                        "A unique index on Sets.Code would prevent this.",
                        duplicateRows);
                }

                var added = 0;
                var updated = 0;

                foreach (var s in response.Data!)
                {
                    if (string.IsNullOrWhiteSpace(s.Code)) continue;

                    if (!existingByCode.TryGetValue(s.Code, out var existing))
                    {
                        var newSet = new Set
                        {
                            Code = s.Code,
                            Name = s.Name,
                            IconUrl = s.IconSvgUri,
                            ReleasedAt = s.ReleasedAt
                        };

                        _context.Sets.Add(newSet);

                        // Also remember it, so the same code appearing twice in one
                        // response can't be inserted twice.
                        existingByCode[s.Code] = newSet;
                        added++;
                    }
                    else if (existing.Name != s.Name ||
                             existing.IconUrl != s.IconSvgUri ||
                             existing.ReleasedAt != s.ReleasedAt)
                    {
                        existing.Name = s.Name;
                        existing.IconUrl = s.IconSvgUri;
                        existing.ReleasedAt = s.ReleasedAt;
                        updated++;
                    }
                }

                await _context.SaveChangesAsync();

                // Drop the cached list so the next visitor sees the new data straight away
                // instead of waiting for the cache to expire.
                _cache.Remove(SetsCacheKey);

                _logger.LogInformation(
                    "Scryfall set sync finished: {Added} added, {Updated} updated, {Total} sets received.",
                    added, updated, response.Data!.Count);
            }
            finally
            {
                SyncLock.Release();
            }
        }

        // This is read by anonymous visitors on every page, so it is cached for a few
        // minutes. AsNoTracking: the sets are only displayed, never modified, which also
        // makes it safe to share one cached list between requests.
        // (Each server instance keeps its own cache, so on several servers a sync can take
        // up to the cache duration to show everywhere.)
        public async Task<List<Set>> GetAllAsync()
        {
            if (_cache.TryGetValue(SetsCacheKey, out List<Set>? cached) && cached != null)
            {
                return cached;
            }

            var sets = await _context.Sets
                .AsNoTracking()
                .OrderByDescending(s => s.ReleasedAt)
                .ToListAsync();

            _cache.Set(SetsCacheKey, sets, SetsCacheDuration);

            _logger.LogInformation("Set list loaded from the database and cached ({Count} sets).", sets.Count);
            return sets;
        }

        // Calls Scryfall and turns every way it can fail into one clear 502, with the
        // technical detail in the logs instead of in the response.
        private async Task<ScryfallResponse> FetchSetsFromScryfallAsync()
        {
            string json;

            try
            {
                using var httpResponse = await _httpClient.GetAsync(ScryfallSetsUrl);
                json = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    // The body can be a big HTML error page, so only an excerpt is logged.
                    _logger.LogError("Scryfall returned {StatusCode} while syncing sets: {BodyExcerpt}",
                        (int)httpResponse.StatusCode, json.Length > 300 ? json[..300] : json);

                    throw new ExternalServiceException("Scryfall returned an error. Please try again later.");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Could not reach Scryfall while syncing sets.");
                throw new ExternalServiceException("Could not reach Scryfall right now. Please try again later.");
            }
            catch (TaskCanceledException ex)
            {
                // HttpClient reports a timeout as a cancelled task.
                _logger.LogError(ex, "The request to Scryfall timed out while syncing sets.");
                throw new ExternalServiceException("Scryfall took too long to respond. Please try again later.");
            }

            ScryfallResponse? response;
            try
            {
                response = JsonSerializer.Deserialize<ScryfallResponse>(json);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Scryfall returned a response that could not be read while syncing sets.");
                throw new ExternalServiceException("Scryfall returned an unexpected response. Please try again later.");
            }

            if (response?.Data == null)
            {
                _logger.LogError("Scryfall's set list was empty or in an unexpected format.");
                throw new ExternalServiceException("Scryfall returned an unexpected response. Please try again later.");
            }

            return response;
        }
    }

}