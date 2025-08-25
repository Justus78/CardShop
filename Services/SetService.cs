using api.DTOs.Sets;
using api.Models;
using CardShop.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace api.Services
{ 

    public interface ISetService
    {
        Task SyncSetsAsync();
    }

    public class SetService : ISetService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;

        public SetService(HttpClient httpClient, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _context = context;
        }

        public async Task SyncSetsAsync()
        {
            var httpResponse = await _httpClient.GetAsync("https://api.scryfall.com/sets");
            var json = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Scryfall returned {httpResponse.StatusCode}: {json}");
            }

            var response = JsonSerializer.Deserialize<ScryfallResponse>(json);
            if (response == null || response.Data == null)
                throw new Exception("Failed to deserialize Scryfall sets.");

            foreach (var s in response.Data)
            {
                var existing = await _context.Sets.FirstOrDefaultAsync(x => x.Code == s.Code);
                if (existing == null)
                {
                    _context.Sets.Add(new Set
                    {
                        Code = s.Code,
                        Name = s.Name,
                        IconUrl = s.IconSvgUri,
                        ReleasedAt = s.ReleasedAt
                    });
                }
                else
                {
                    existing.Name = s.Name;
                    existing.IconUrl = s.IconSvgUri;
                    existing.ReleasedAt = s.ReleasedAt;
                }
            }

            await _context.SaveChangesAsync();
        }

    }

}
