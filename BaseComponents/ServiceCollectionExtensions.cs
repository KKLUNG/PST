using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace BaseComponents
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 註冊底層元件所需服務 (MudBlazor、BaseDialogService、BaseNotifier)
        /// 用法 (Program.cs)：builder.Services.AddBaseComponents();
        /// </summary>
        public static IServiceCollection AddBaseComponents(this IServiceCollection services)
        {
            services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
                config.SnackbarConfiguration.VisibleStateDuration = 3000;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.PreventDuplicates = false;
            });

            services.AddScoped<BaseDialogService>();
            services.AddScoped<BaseNotifier>();

            return services;
        }
    }
}
