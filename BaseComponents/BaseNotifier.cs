using MudBlazor;

namespace BaseComponents
{
    /// <summary>
    /// 訊息通知服務 - 畫面上方顯示短暫的成功／錯誤訊息
    /// </summary>
    public class BaseNotifier
    {
        private readonly ISnackbar _snackbar;

        public BaseNotifier(ISnackbar snackbar)
        {
            _snackbar = snackbar;
        }

        public void Success(string message) => _snackbar.Add(message, Severity.Success);

        public void Info(string message) => _snackbar.Add(message, Severity.Info);

        public void Warning(string message) => _snackbar.Add(message, Severity.Warning);

        /// <summary>錯誤訊息需手動關閉，避免使用者來不及看到</summary>
        public void Error(string message) =>
            _snackbar.Add(message, Severity.Error, options => options.RequireInteraction = true);
    }
}
