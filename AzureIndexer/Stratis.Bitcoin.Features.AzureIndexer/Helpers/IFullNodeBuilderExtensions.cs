﻿namespace Stratis.Bitcoin.Features.AzureIndexer.Helpers
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Stratis.Bitcoin.Builder;
    using Stratis.Bitcoin.Configuration.Logging;

    /// <summary>
    /// A class providing extension methods for <see cref="IFullNodeBuilder"/>.
    /// </summary>
    public static class IFullNodeBuilderExtensions
    {
        public static IFullNodeBuilder UseAzureIndexer(this IFullNodeBuilder fullNodeBuilder, Action<AzureIndexerSettings> setup = null)
        {
            LoggingConfiguration.RegisterFeatureNamespace<AzureIndexerFeature>("azindex");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                var settings = new AzureIndexerSettings(setup);

                features
                    .AddFeature<AzureIndexerFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<AzureIndexerLoop>();
                        services.AddSingleton<AzureIndexerSettings>(settings);
                        if (settings.IsSidechain)
                        {
                            services.AddSingleton<ISmartContractOperations, SmartContractOperations>();
                        }
                    });
            });

            return fullNodeBuilder;
        }
    }
}