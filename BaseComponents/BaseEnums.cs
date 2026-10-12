namespace BaseComponents
{
    /// <summary>
    /// 按鈕類型
    /// </summary>
    public enum BaseButtonKind
    {
        /// <summary>一般按鈕 (外框)</summary>
        Default,

        /// <summary>主要動作 (實心主色)，如查詢、儲存；在欄位中按 Enter 到最後一欄時會移到此按鈕</summary>
        Primary,

        /// <summary>危險動作 (實心紅色)，如刪除</summary>
        Danger,
    }

    /// <summary>
    /// 文字方框類型
    /// </summary>
    public enum BaseTextType
    {
        Text,
        Password,
        Email,
        Tel,
        Url,
    }

    /// <summary>
    /// 按鈕圖示 (以列舉提供，頁面不需直接引用 Fluent UI 圖示)
    /// </summary>
    public enum BaseIcon
    {
        None,
        Search,
        Add,
        Edit,
        Delete,
        Save,
        Reset,
        Cancel,
        Back,
        Print,
        Export,
        Confirm,
        Upload,
        Download,
    }
}
