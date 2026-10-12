using System.ComponentModel.DataAnnotations;

namespace BaseComponents
{
    /// <summary>
    /// 手機號碼格式驗證：09XX-XXXXXX (搭配 BaseMobile 使用，空值視為通過，必填請另加 [Required])
    /// 用法：[BaseMobile] public string? Mobile { get; set; }
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class BaseMobileAttribute : RegularExpressionAttribute
    {
        public BaseMobileAttribute() : base(@"^09\d{2}-\d{6}$")
        {
            ErrorMessage = "手機號碼格式應為 09XX-XXXXXX";
        }
    }

    /// <summary>
    /// 市話格式驗證：區碼(2-4 碼)-號碼(5-8 碼)，可加 #分機(1-6 碼)
    /// (搭配 BaseTelephone 使用，空值視為通過，必填請另加 [Required])
    /// 用法：[BaseTelephone] public string? Tel { get; set; }
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class BaseTelephoneAttribute : RegularExpressionAttribute
    {
        public BaseTelephoneAttribute() : base(@"^0\d{1,3}-\d{5,8}(#\d{1,6})?$")
        {
            ErrorMessage = "電話格式應為 區碼-號碼，分機以 # 分隔，例如 02-23456789#123";
        }
    }
}
