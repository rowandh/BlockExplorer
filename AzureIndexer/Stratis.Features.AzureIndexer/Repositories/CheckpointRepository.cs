﻿namespace Stratis.Features.AzureIndexer.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage.Blob;
    using NBitcoin;

    public class CheckpointRepository
    {
        private readonly ILoggerFactory loggerFactory;

        public CheckpointRepository(CloudBlobContainer container, Network network, string checkpointSet, ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            this._Network = network;
            this._Container = container;
            this.CheckpointSet = checkpointSet;
        }

        public string CheckpointSet { get; set; }

        readonly CloudBlobContainer _Container;

        readonly Network _Network;

        public Task<Checkpoint> GetCheckpointAsync(string checkpointName)
        {
            CloudBlockBlob blob = this._Container.GetBlockBlobReference("Checkpoints/" + this.GetSetPart(checkpointName));
            return Checkpoint.LoadBlobAsync(blob, this._Network, this.loggerFactory);
        }

        private string GetSetPart(string checkpointName)
        {
            bool isLocal = !checkpointName.Contains('/');
            if (isLocal)
            {
                return this.GetSetPart() + checkpointName;
            }

            if (checkpointName.StartsWith("/"))
            {
                checkpointName = checkpointName.Substring(1);
            }

            return checkpointName;
        }

        private string GetSetPart()
        {
            if (this.CheckpointSet == null)
            {
                return "";
            }

            return this.CheckpointSet + "/";
        }

        public Task<Checkpoint[]> GetCheckpointsAsync()
        {
            var checkpoints = new List<Task<Checkpoint>>();

            foreach (CloudBlockBlob blob in this._Container
                .ListBlobsAsync("Checkpoints/" + this.GetSetPart(), true, BlobListingDetails.None)
                .GetAwaiter()
                .GetResult()
                .OfType<CloudBlockBlob>())
            {
                checkpoints.Add(Checkpoint.LoadBlobAsync(blob, this._Network, this.loggerFactory));
            }

            return Task.WhenAll(checkpoints.ToArray());
        }

        public async Task DeleteCheckpointsAsync()
        {
            var deletions = new List<Task>();
            Checkpoint[] checkpoints = await this.GetCheckpointsAsync().ConfigureAwait(false);
            foreach (Checkpoint checkpoint in checkpoints)
            {
                deletions.Add(checkpoint.DeleteAsync());
            }

            await Task.WhenAll(deletions.ToArray()).ConfigureAwait(false);
        }

        public void DeleteCheckpoints()
        {
            try
            {
                this.DeleteCheckpointsAsync().Wait();
            }
            catch (AggregateException aex)
            {
                ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
            }
        }

        public Checkpoint GetCheckpoint(string checkpointName)
        {
            try
            {
                return this.GetCheckpointAsync(checkpointName).Result;
            }
            catch (AggregateException aex)
            {
                ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
                return null;
            }
        }
    }

    public static class CloudBlobContainerExtensions
    {
        public static async Task<IList<IListBlobItem>> ListBlobsAsync(this CloudBlobContainer blobContainer,
            string prefix, bool useFlatBlobListing, BlobListingDetails blobListingDetails,
            CancellationToken ct = default(CancellationToken), Action<IList<IListBlobItem>> onProgress = null)
        {
            var items = new List<IListBlobItem>();
            BlobContinuationToken token = null;

            do
            {
                BlobResultSegment seg = await blobContainer.ListBlobsSegmentedAsync(prefix, useFlatBlobListing,
                    blobListingDetails, null, token, null, null);
                token = seg.ContinuationToken;
                items.AddRange(seg.Results);
                if (onProgress != null)
                {
                    onProgress(items);
                }
            }
            while (token != null && !ct.IsCancellationRequested);

            return items;
        }
    }
}
