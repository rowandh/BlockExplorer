using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using DBreeze.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stratis.Bitcoin.Base;
using Stratis.Bitcoin.Utilities;
using Stratis.Features.AzureIndexer;

namespace AzureIndexer.Api.Infrastructure
{
    using System;
    using System.IO;
    using System.Threading;
    using NBitcoin;

    public class ChainCacheProvider
    {
        private readonly string cacheFilePath;
        private readonly ChainIndexer chain;
        private readonly IChainRepository repository;
        private readonly IndexerClient client;
        private readonly ILogger logger;

        public ChainCacheProvider(IConfiguration configuration, ChainIndexer chain, IndexerClient client, ILoggerFactory loggerFactory, IChainRepository chainRepository)
        {
            this.cacheFilePath = configuration["LocalChain"];
            this.chain = chain;
            this.client = client;
            this.repository = chainRepository;
            this.logger = loggerFactory.CreateLogger<ChainCacheProvider>();
        }

        public bool IsCacheAvailable =>
            File.Exists(Path.Combine(this.cacheFilePath, "_completed.lock")) &&
            DateTime.UtcNow.Subtract(File.GetLastWriteTimeUtc(Path.Combine(this.cacheFilePath, "_completed.lock"))).TotalHours < 24;

        public async Task BuildCache()
        {
            try
            {
                await this.LoadCache();

                var changes = this.client.GetChainChangesUntilFork(this.chain.Tip, false);
                try
                {
                    changes.UpdateChain(this.chain, this.logger);
                }
                catch (ArgumentException ex)
                {
                    // Happen when chain in table is corrupted
                    this.client.Configuration.GetChainTable().DeleteIfExistsAsync().GetAwaiter().GetResult();
                    for (var i = 0; i < 20; i++)
                    {
                        try
                        {
                            if (this.client.Configuration.GetChainTable().CreateIfNotExistsAsync().GetAwaiter()
                                .GetResult())
                            {
                                break;
                            }
                        }
                        catch
                        {
                            // ignored
                        }

                        Thread.Sleep(10000);
                    }

                    this.client.Configuration.CreateIndexer().IndexChain(this.chain);
                }

                await this.SaveChainCache();
                if (!Directory.Exists(cacheFilePath))
                {
                    Directory.CreateDirectory(cacheFilePath);
                }

                File.Delete(Path.Combine(cacheFilePath, "_completed.lock"));
                File.Create(Path.Combine(cacheFilePath, "_completed.lock"));
            }
            catch (Exception ex)
            {
                // ignore
            }
        }

        private async Task LoadCache()
        {
            if (string.IsNullOrEmpty(this.cacheFilePath))
                return;

            try
            {
                await this.repository.LoadAsync(this.chain.Genesis);
            }
            catch
            {
                // We don't care if it don't succeed
            }
        }

        private async Task SaveChainCache()
        {
            if (string.IsNullOrEmpty(this.cacheFilePath))
                return;

            try
            {
                await this.repository.SaveAsync(this.chain);
            }
            catch
            {
                // ignored
            }
        }
    }

    public class ChainCacheCheckMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ChainCacheProvider provider;

        public ChainCacheCheckMiddleware(RequestDelegate next, ChainCacheProvider provider)
        {
            this.next = next;
            this.provider = provider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (this.provider.IsCacheAvailable == false)
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync("Cache is being built. Try refreshing this page in 15minutes");
                return;
            }

            await this.next(context);
        }
    }
}