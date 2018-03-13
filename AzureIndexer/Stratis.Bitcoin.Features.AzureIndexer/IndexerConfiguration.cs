﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using NBitcoin;
using NBitcoin.Protocol;
using Stratis.Bitcoin.P2P.Peer;
using Stratis.Bitcoin.P2P.Protocol.Payloads;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.AzureIndexer
{
    public class IndexerConfiguration
    {
        public IndexerConfiguration()
        {
            Network = Network.Main;
        }

        public IndexerConfiguration(IConfiguration config)
        {
            var account = GetValue(config, "Azure.AccountName", true);
            var key = GetValue(config, "Azure.Key", true);
            this.StorageCredentials = new StorageCredentials(account, key);
            this.StorageNamespace = GetValue(config, "StorageNamespace", false);
            var network = GetValue(config, "Bitcoin.Network", false) ?? "Main";
            this.Network = Network.GetNetwork(network);
            if (this.Network == null)
                throw new IndexerConfigurationErrorsException("Invalid value " + network + " in appsettings (expecting Main, Test or Seg)");
            this.Node = GetValue(config, "Node", false);
            this.CheckpointSetName = GetValue(config, "CheckpointSetName", false);
            if (string.IsNullOrWhiteSpace(this.CheckpointSetName))
                this.CheckpointSetName = "default";

            var emulator = GetValue(config, "AzureStorageEmulatorUsed", false);
            if (!string.IsNullOrWhiteSpace(emulator))
                this.AzureStorageEmulatorUsed = bool.Parse(emulator);
        }

        public Task EnsureSetupAsync()
        {
            var tasks = EnumerateTables()
                .Select(t => t.CreateIfNotExistsAsync())
                .OfType<Task>()
                .ToList();
            tasks.Add(GetBlocksContainer().CreateIfNotExistsAsync());
            return Task.WhenAll(tasks.ToArray());
        }

        public void EnsureSetup()
        {
            try
            {
                EnsureSetupAsync().Wait();
            }
            catch (AggregateException aex)
            {
                ExceptionDispatchInfo.Capture(aex).Throw();
                throw;
            }
        }

        protected static string GetValue(IConfiguration config, string setting, bool required)
        {			
            var result = config[setting];
            result = String.IsNullOrWhiteSpace(result) ? null : result;
            if (result == null && required)
                throw new IndexerConfigurationErrorsException("AppSetting " + setting + " not found");
            return result;
        }

        

        public Network Network
        {
            get;
            set;
        }

        public bool AzureStorageEmulatorUsed
        {
            get;
            set;
        }

        public AzureIndexer CreateIndexer()
        {
            return new AzureIndexer(this);
        }

        public NetworkPeer ConnectToNode(bool isRelay)
        {
            if (String.IsNullOrEmpty(Node))
                throw new IndexerConfigurationErrorsException("Node setting is not configured");

            NetworkPeerFactory networkPeerFactory = new NetworkPeerFactory(Network.StratisTest, DateTimeProvider.Default, new LoggerFactory(), new PayloadProvider().DiscoverPayloads());
            return (NetworkPeer)networkPeerFactory.CreateConnectedNetworkPeerAsync(Node, ProtocolVersion.PROTOCOL_VERSION, isRelay: isRelay).Result;
        }

        public string Node
        {
            get;
            set;
        }

        public string CheckpointSetName
        {
            get;
            set;
        }

        string _Container = "indexer";
        string _TransactionTable = "transactions";
        string _BalanceTable = "balances";
        string _ChainTable = "chain";
        string _WalletTable = "wallets";

        public StorageCredentials StorageCredentials
        {
            get;
            set;
        }

        public CloudBlobClient CreateBlobClient()
        {
            return new CloudBlobClient(MakeUri("blob", AzureStorageEmulatorUsed), StorageCredentials);
        }

        public IndexerClient CreateIndexerClient()
        {
            return new IndexerClient(this);
        }

        public CloudTable GetTransactionTable()
        {
            return CreateTableClient().GetTableReference(GetFullName(_TransactionTable));
        }

        public CloudTable GetWalletRulesTable()
        {
            return CreateTableClient().GetTableReference(GetFullName(_WalletTable));
        }

        public CloudTable GetTable(string tableName)
        {
            return CreateTableClient().GetTableReference(GetFullName(tableName));
        }

        private string GetFullName(string storageObjectName)
        {
            return (StorageNamespace + storageObjectName).ToLowerInvariant();
        }

        public CloudTable GetBalanceTable()
        {
            return CreateTableClient().GetTableReference(GetFullName(_BalanceTable));
        }

        public CloudTable GetChainTable()
        {
            return CreateTableClient().GetTableReference(GetFullName(_ChainTable));
        }

        public CloudBlobContainer GetBlocksContainer()
        {
            return CreateBlobClient().GetContainerReference(GetFullName(_Container));
        }

        private Uri MakeUri(string clientType, bool azureStorageEmulatorUsed = false)
        {
            if (!azureStorageEmulatorUsed)
            {
                return new Uri(String.Format("http://{0}.{1}.core.windows.net/", StorageCredentials.AccountName,
                    clientType), UriKind.Absolute);
            }
            else
            {
                if (clientType.Equals("blob"))
                {
                    return new Uri("http://127.0.0.1:10000/devstoreaccount1");
                }
                else
                {
                    if (clientType.Equals("table"))
                    {
                        return new Uri("http://127.0.0.1:10002/devstoreaccount1");
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public CloudTableClient CreateTableClient()
        {
            return new CloudTableClient(MakeUri("table", AzureStorageEmulatorUsed), StorageCredentials);
        }

        public string StorageNamespace
        {
            get;
            set;
        }

        public IEnumerable<CloudTable> EnumerateTables()
        {
            yield return GetTransactionTable();
            yield return GetBalanceTable();
            yield return GetChainTable();
            yield return GetWalletRulesTable();
        }
    }
}
