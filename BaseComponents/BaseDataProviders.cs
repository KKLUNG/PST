namespace BaseComponents
{
    /// <summary>
    /// 參數檔資料來源 (供 BaseParameterSelect 使用)
    /// 底層元件不直接存取資料庫，由各系統實作此介面並註冊到 DI，
    /// 例如：builder.Services.AddScoped&lt;IBaseParameterProvider, CommonDataService&gt;();
    /// </summary>
    public interface IBaseParameterProvider
    {
        /// <summary>
        /// 取得參數群組的選項 (已排序、不含已刪除)
        /// </summary>
        /// <param name="setItem">參數群組代碼，例如 "UNIT"</param>
        /// <returns>選項清單：Value 為代碼、Text 為名稱</returns>
        Task<IReadOnlyList<BaseOption<string>>> GetOptionsAsync(string setItem);
    }

    /// <summary>
    /// 地址資料來源 (供 BaseAddress 使用)
    /// </summary>
    public interface IBaseAddressProvider
    {
        /// <summary>取得縣市清單</summary>
        Task<IReadOnlyList<string>> GetCountiesAsync();

        /// <summary>取得縣市下的鄉鎮市區與郵遞區號</summary>
        Task<IReadOnlyList<BaseAddressArea>> GetDistrictsAsync(string county);
    }

    /// <summary>
    /// 鄉鎮市區
    /// </summary>
    /// <param name="District">鄉鎮市區名稱</param>
    /// <param name="ZipCode">郵遞區號 (3 碼)</param>
    public record BaseAddressArea(string District, string ZipCode);

    /// <summary>
    /// 上傳的檔案
    /// </summary>
    /// <param name="Name">檔名 (含副檔名)</param>
    /// <param name="Size">大小 (bytes)</param>
    /// <param name="Content">檔案內容</param>
    public record BaseUploadFile(string Name, long Size, byte[] Content);

    /// <summary>
    /// 上傳處理結果
    /// </summary>
    /// <param name="Success">是否成功</param>
    /// <param name="Messages">結果訊息 (逐行顯示)</param>
    public record BaseUploadResult(bool Success, IReadOnlyList<string> Messages)
    {
        public static BaseUploadResult Ok(params string[] messages) => new(true, messages);

        public static BaseUploadResult Fail(params string[] messages) => new(false, messages);
    }
}
