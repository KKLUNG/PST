using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace BaseComponents
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 註冊底層元件所需服務 (Fluent UI、BaseDialogService、BaseNotifier)
        /// 用法 (Program.cs)：builder.Services.AddBaseComponents();
        /// </summary>
        public static IServiceCollection AddBaseComponents(this IServiceCollection services)
        {
            services.AddFluentUIComponents();

            services.AddScoped<BaseDialogService>();
            services.AddScoped<BaseNotifier>();

            return services;
        }
    }
}
