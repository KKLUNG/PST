using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using BaseComponents;
using BlazorUserManagement.Data;

namespace BlazorUserManagement.Services
{
    /// <summary>
    /// 共用資料服務 - 提供底層元件所需的參數檔與地址資料
    /// 資料變動不頻繁，讀取後快取 10 分鐘，避免每個下拉選單都查詢資料庫
    /// </summary>
    public class CommonDataService : IBaseParameterProvider, IBaseAddressProvider
    {
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly IMemoryCache _cache;

        public CommonDataService(IDbContextFactory<ApplicationDbContext> contextFactory, IMemoryCache cache)
        {
            _contextFactory = contextFactory;
            _cache = cache;
        }

        /// <summary>
        /// 取得參數群組的選項 (排除已刪除，依 SortOrder 排序)
        /// </summary>
        public async Task<IReadOnlyList<BaseOption<string>>> GetOptionsAsync(string setItem)
        {
            var options = await _cache.GetOrCreateAsync($"parameter:{setItem}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;

                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.Parameters
                    .AsNoTracking()
                    .Where(p => p.SetItem == setItem && !p.IsDeleted)
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.SetType)
                    .Select(p => new BaseOption<string>(p.SetType, p.SetValue))
                    .ToListAsync();
            });

            return options ?? [];
        }

        public async Task<IReadOnlyList<string>> GetCountiesAsync()
        {
            var areas = await GetAllAreasAsync();
            return areas
                .GroupBy(a => a.County)
                .OrderBy(g => g.Min(a => a.SortOrder))
                .Select(g => g.Key)
                .ToList();
        }

        public async Task<IReadOnlyList<BaseAddressArea>> GetDistrictsAsync(string county)
        {
            var areas = await GetAllAreasAsync();
            return areas
                .Where(a => a.County == county)
                .OrderBy(a => a.SortOrder)
                .Select(a => new BaseAddressArea(a.District, a.ZipCode))
                .ToList();
        }

        private async Task<List<AddressArea>> GetAllAreasAsync()
        {
            var areas = await _cache.GetOrCreateAsync("address:areas", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;

                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.AddressAreas.AsNoTracking().ToListAsync();
            });

            return areas ?? [];
        }
    }
}
