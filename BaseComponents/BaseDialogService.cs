using Microsoft.FluentUI.AspNetCore.Components;

namespace BaseComponents
{
    /// <summary>
    /// 對話框服務 - 統一確認視窗與表單視窗的開啟方式
    /// </summary>
    public class BaseDialogService
    {
        private readonly IDialogService _dialogService;

        public BaseDialogService(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        /// <summary>
        /// 顯示確認視窗，按下確認回傳 true
        /// </summary>
        /// <param name="message">確認訊息</param>
        /// <param name="title">標題</param>
        /// <param name="confirmText">確認按鈕文字</param>
        public async Task<bool> ConfirmAsync(string message, string title = "確認", string confirmText = "確定")
        {
            var dialog = await _dialogService.ShowConfirmationAsync(message, confirmText, "取消", title);
            var result = await dialog.Result;
            return !result.Cancelled;
        }

        /// <summary>
        /// 開啟表單對話框 (內含 BaseFormDialog 的元件)，存檔成功回傳 true
        /// </summary>
        /// <typeparam name="TDialog">對話框元件，需實作 IDialogContentComponent&lt;TData&gt;</typeparam>
        /// <typeparam name="TData">傳給對話框的資料 (元件的 Content 參數)，需為類別，例如 record UserEditArgs(int? UserId)</typeparam>
        /// <param name="title">標題</param>
        /// <param name="data">傳給對話框的資料</param>
        /// <param name="width">寬度，預設 720px</param>
        public async Task<bool> ShowFormAsync<TDialog, TData>(string title, TData data, string width = "720px")
            where TDialog : IDialogContentComponent<TData>
            where TData : class
        {
            var parameters = new DialogParameters
            {
                Title = title,
                Width = width,
                // 按鈕由 BaseFormDialog 提供，不使用對話框預設的頁尾按鈕
                PrimaryAction = null,
                SecondaryAction = null,
                PreventDismissOnOverlayClick = true,
                Modal = true,
                TrapFocus = true,
            };

            var dialog = await _dialogService.ShowDialogAsync(typeof(TDialog), data, parameters);
            var result = await dialog.Result;
            return !result.Cancelled;
        }
    }
}
