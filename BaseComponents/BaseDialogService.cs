using Microsoft.AspNetCore.Components;
using MudBlazor;

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
        /// <param name="danger">危險操作 (如刪除) 時確認按鈕為紅色</param>
        public async Task<bool> ConfirmAsync(string message, string title = "確認", string confirmText = "確定", bool danger = false)
        {
            var parameters = new DialogParameters
            {
                { nameof(BaseConfirmDialog.Message), message },
                { nameof(BaseConfirmDialog.ConfirmText), confirmText },
                { nameof(BaseConfirmDialog.Danger), danger },
            };
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraSmall,
                FullWidth = true,
            };

            var dialog = await _dialogService.ShowAsync(typeof(BaseConfirmDialog), title, parameters, options);
            var result = await dialog.Result;
            return result is { Canceled: false };
        }

        /// <summary>
        /// 開啟表單對話框 (內含 BaseFormDialog 的元件)，存檔成功回傳 true
        /// </summary>
        /// <typeparam name="TDialog">對話框元件</typeparam>
        /// <param name="title">標題</param>
        /// <param name="parameters">傳給對話框元件的參數</param>
        public async Task<bool> ShowFormAsync<TDialog>(string title, DialogParameters? parameters = null)
            where TDialog : IComponent
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = true,
            };

            var dialog = await _dialogService.ShowAsync(typeof(TDialog), title, parameters ?? new DialogParameters(), options);
            var result = await dialog.Result;
            return result is { Canceled: false };
        }
    }
}
