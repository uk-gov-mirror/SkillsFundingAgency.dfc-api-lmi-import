﻿using DFC.Api.Lmi.Import.Attributes;
using DFC.Api.Lmi.Import.Contracts;
using DFC.Api.Lmi.Import.Models;
using DFC.Api.Lmi.Import.Models.GraphData;
using DFC.ServiceTaxonomy.Neo4j.Commands.Interfaces;
using DFC.ServiceTaxonomy.Neo4j.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DFC.Api.Lmi.Import.Connectors
{
    public class GraphConnector : IGraphConnector
    {
        private readonly IGraphCluster graphCluster;
        private readonly IServiceProvider serviceProvider;
        private readonly GraphOptions graphOptions;
        private readonly ICypherQueryBuilderService cypherQueryBuilderService;

        public GraphConnector(
            IGraphCluster graphCluster,
            IServiceProvider serviceProvider,
            GraphOptions graphOptions,
            ICypherQueryBuilderService cypherQueryBuilderService)
        {
            this.graphCluster = graphCluster;
            this.serviceProvider = serviceProvider;
            this.graphOptions = graphOptions;
            this.cypherQueryBuilderService = cypherQueryBuilderService;
        }

        public IList<string> BuildPurgeCommands()
        {
            return cypherQueryBuilderService.BuildPurgeCommands();
        }

        public IList<string> BuildPurgeCommandsForInitialKey(string key)
        {
            return cypherQueryBuilderService.BuildPurgeCommandsForInitialKey(key);
        }

        public IList<string> BuildImportCommands(GraphBaseSocModel? parent)
        {
            _ = parent ?? throw new ArgumentNullException(nameof(parent));

            var commands = new List<string>();

            var parentGraphNodeAttribute = Utilities.AttributeUtilies.GetAttribute<GraphNodeAttribute>(parent.GetType());
            if (parentGraphNodeAttribute != null)
            {
                commands.Add(cypherQueryBuilderService.BuildMerge(parent, parentGraphNodeAttribute.Name));
                commands.AddRange(cypherQueryBuilderService.BuildRelationships(parent, parentGraphNodeAttribute.Name));
            }

            return commands;
        }

        public async Task RunAsync(IList<string>? commands)
        {
            _ = commands ?? throw new ArgumentNullException(nameof(commands));

            var customCommands = new List<ICustomCommand>();
            foreach (var command in commands)
            {
                var customCommand = serviceProvider.GetRequiredService<ICustomCommand>();
                customCommand.Command = command;
                customCommands.Add(customCommand);
            }

            await graphCluster.Run(graphOptions.ReplicaSetName, customCommands.ToArray()).ConfigureAwait(false);
        }
    }
}
