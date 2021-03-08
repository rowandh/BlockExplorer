﻿namespace Stratis.Features.AzureIndexer.Entities
{
    using System.Collections.Generic;
    using Microsoft.WindowsAzure.Storage.Table;
    using NBitcoin;
    using Stratis.Features.AzureIndexer.Helpers;

    public class ChainPartEntry
    {
        public ChainPartEntry()
        {
            this.BlockHeaders = new List<BlockHeader>();
        }

        public ChainPartEntry(DynamicTableEntity entity, Network network)
        {
            this.ChainOffset = Helper.StringToHeight(entity.RowKey);
            this.BlockHeaders = new List<BlockHeader>();
            foreach (KeyValuePair<string, EntityProperty> prop in entity.Properties)
            {
                BlockHeader header = network.Consensus.ConsensusFactory.CreateBlockHeader();
                header.FromBytes(prop.Value.BinaryValue);
                this.BlockHeaders.Add(header);
            }
        }

        public int ChainOffset { get; set; }

        public List<BlockHeader> BlockHeaders { get; private set; }

        public BlockHeader GetHeader(int height)
        {
            if (height < this.ChainOffset)
            {
                return null;
            }

            height = height - this.ChainOffset;
            if (height >= this.BlockHeaders.Count)
            {
                return null;
            }

            return this.BlockHeaders[height];
        }

        public DynamicTableEntity ToEntity()
        {
            DynamicTableEntity entity = new DynamicTableEntity();
            entity.PartitionKey = "a";
            entity.RowKey = Helper.HeightToString(this.ChainOffset);
            var i = 0;
            foreach (BlockHeader header in this.BlockHeaders)
            {
                entity.Properties.Add("a" + i, new EntityProperty(header.ToBytes()));
                i++;
            }

            return entity;
        }
    }
}
