﻿using Microsoft.Extensions.DependencyInjection;

namespace Application.Options
{
    public static class OperationSettingsExtensions
    {
        public static void AddOperationSettings(this IServiceCollection services, Action<OperationSettings> configure)
        {
            services.AddOptions<OperationSettings>()
                .Configure(configure)
                .Validate(option =>
                    (option.Backtest.Enabled && !option.Order.ExecOrder || !option.Backtest.Enabled)
                );
        }
    }
}
