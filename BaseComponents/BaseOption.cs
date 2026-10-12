namespace BaseComponents
{
    /// <summary>
    /// 下拉選單選項
    /// </summary>
    /// <param name="Value">選項值</param>
    /// <param name="Text">顯示文字</param>
    public record BaseOption<TValue>(TValue Value, string Text);
}
