using MudBlazor;

namespace BaseComponents
{
    /// <summary>
    /// 底層元件的預設主題 (綠色系，傳統後台風格)
    /// 各系統若需不同配色，可在 BaseProviders 傳入自訂的 MudTheme
    /// </summary>
    public static class BaseTheme
    {
        public static MudTheme Default { get; } = new()
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#4f8f26",
                PrimaryContrastText = "#ffffff",
                Secondary = "#6b7c93",
                Error = "#c0392b",
                Success = "#3f7a1f",
                AppbarBackground = "#6fae3f",
                Background = "#ffffff",
                TextPrimary = "#222222",
                TextSecondary = "#666666",
                LinesDefault = "#a8c88b",
                LinesInputs = "#7f9db9",
                TableLines = "#a8c88b",
                TableStriped = "#f4f9ee",
                TableHover = "#fff8d0",
                Divider = "#a8c88b",
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = ["Microsoft JhengHei", "微軟正黑體", "PMingLiU", "Arial", "sans-serif"],
                    FontSize = "0.8125rem",
                },
                Button = new ButtonTypography
                {
                    FontFamily = ["Microsoft JhengHei", "微軟正黑體", "PMingLiU", "Arial", "sans-serif"],
                    FontSize = "0.8125rem",
                    TextTransform = "none",
                },
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "2px",
            },
        };
    }
}
