namespace BlazorUserManagement.Components.Pages.Users
{
    /// <summary>
    /// 使用者新增／編輯對話框的參數
    /// </summary>
    /// <param name="UserId">要編輯的使用者 ID，null 表示新增</param>
    public record UserEditArgs(int? UserId);
}
