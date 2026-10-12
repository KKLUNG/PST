using Microsoft.FluentUI.AspNetCore.Components;

namespace BaseComponents
{
    /// <summary>
    /// 訊息通知服務 - 畫面上方顯示短暫的成功／錯誤訊息
    /// </summary>
    public class BaseNotifier
    {
        private readonly IToastService _toastService;

        public BaseNotifier(IToastService toastService)
        {
            _toastService = toastService;
        }

        public void Success(string message) => _toastService.ShowSuccess(message);

        public void Info(string message) => _toastService.ShowInfo(message);

        public void Warning(string message) => _toastService.ShowWarning(message, 5000);

        /// <summary>錯誤訊息顯示較久 (8 秒)，避免使用者來不及看到</summary>
        public void Error(string message) => _toastService.ShowError(message, 8000);
    }
}
