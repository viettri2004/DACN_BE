using System;
using System.Text.Json;
using System.Threading.Tasks;
using AccountService.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using src.Shared.Domain.Entities;

namespace AccountService.Infrastructure.Repositories
{
    public class CachedDashboardRepository : IDashboardRepository
    {
        private readonly IDashboardRepository _inner;
        private readonly IDistributedCache _cache;
        private const string CacheKey = "dashboard:data";

        public CachedDashboardRepository(IDashboardRepository inner, IDistributedCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        public async Task<ApiResponse> GetDashboardDataAsync()
        {
            var cachedData = await _cache.GetStringAsync(CacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<ApiResponse>(cachedData);
            }

            var response = await _inner.GetDashboardDataAsync();

            if (response.Success)
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) // Cache for 10 minutes
                };
                await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(response), options);
            }

            return response;
        }
    }
}
