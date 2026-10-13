using Microsoft.EntityFrameworkCore;

namespace BlazorUserManagement.Data.Cms
{
    /// <summary>
    /// 一筆 CMSData 資料 (含轉換後的表單模型)
    /// </summary>
    public record CmsRecord<TModel>(Guid PKey, DateTime CreateDate, DateTime? ModifiedDate, TModel Model);

    /// <summary>
    /// CMSData 存取服務：以 PageName 區分畫面，表單模型以 [CmsColumn] 對應欄位
    /// </summary>
    public class CmsDataService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly TimeProvider _clock;

        public CmsDataService(IDbContextFactory<ApplicationDbContext> contextFactory, TimeProvider? clock = null)
        {
            _contextFactory = contextFactory;
            _clock = clock ?? TimeProvider.System;
        }

        private DateTime Now => _clock.GetLocalNow().DateTime;

        /// <summary>
        /// 儲存：pkey 為 null 時新增，否則修改該筆 (須屬於同一畫面)；回傳 PKey
        /// </summary>
        public async Task<Guid> SaveAsync<TModel>(string pageName, TModel model, Guid? pkey = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            CmsData entity;
            if (pkey is null)
            {
                entity = new CmsData { PageName = pageName, CreateDate = Now };
                context.CmsData.Add(entity);
            }
            else
            {
                entity = await context.CmsData.SingleOrDefaultAsync(x => x.PKey == pkey.Value)
                    ?? throw new InvalidOperationException("找不到要修改的資料，可能已被刪除");
                if (entity.PageName != pageName)
                {
                    throw new InvalidOperationException($"資料不屬於畫面 {pageName}，不可修改");
                }
                entity.ModifiedDate = Now;
            }

            CmsMapper.Apply(model, entity);
            await context.SaveChangesAsync();
            return entity.PKey;
        }

        /// <summary>
        /// 一次新增多筆 (例如批次匯入)，全部成功或全部不新增；回傳新增筆數
        /// </summary>
        public async Task<int> AddRangeAsync<TModel>(string pageName, IEnumerable<TModel> models)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var now = Now;
            foreach (var model in models)
            {
                var entity = new CmsData { PageName = pageName, CreateDate = now };
                CmsMapper.Apply(model, entity);
                context.CmsData.Add(entity);
            }

            return await context.SaveChangesAsync();
        }

        /// <summary>
        /// 讀取一筆；不存在時回傳 null
        /// </summary>
        public async Task<CmsRecord<TModel>?> GetAsync<TModel>(Guid pkey) where TModel : new()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var entity = await context.CmsData.AsNoTracking().SingleOrDefaultAsync(x => x.PKey == pkey);
            return entity is null ? null : ToRecord<TModel>(entity);
        }

        /// <summary>
        /// 讀取畫面的所有資料 (依建立時間排序，最早的在前)
        /// </summary>
        public async Task<List<CmsRecord<TModel>>> ListAsync<TModel>(string pageName) where TModel : new()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var entities = await context.CmsData.AsNoTracking()
                .Where(x => x.PageName == pageName)
                .OrderBy(x => x.CreateDate)
                .ToListAsync();
            return entities.Select(ToRecord<TModel>).ToList();
        }

        /// <summary>
        /// 刪除一筆；回傳是否有刪除
        /// </summary>
        public async Task<bool> DeleteAsync(Guid pkey)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var entity = await context.CmsData.SingleOrDefaultAsync(x => x.PKey == pkey);
            if (entity is null)
            {
                return false;
            }

            context.CmsData.Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }

        private static CmsRecord<TModel> ToRecord<TModel>(CmsData entity) where TModel : new() =>
            new(entity.PKey, entity.CreateDate, entity.ModifiedDate, CmsMapper.ToModel<TModel>(entity));
    }
}
