﻿using DFC.Api.Lmi.Import.Connectors;
using DFC.Api.Lmi.Import.Contracts;
using DFC.Api.Lmi.Import.Extensions;
using DFC.Api.Lmi.Import.HttpClientPolicies;
using DFC.Api.Lmi.Import.Models;
using DFC.Api.Lmi.Import.Models.ClientOptions;
using DFC.Api.Lmi.Import.Services;
using DFC.Api.Lmi.Import.Startup;
using DFC.ServiceTaxonomy.Neo4j.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;

[assembly: WebJobsStartup(typeof(WebJobsExtensionStartup), "Web Jobs Extension Startup")]

namespace DFC.Api.Lmi.Import.Startup
{
    [ExcludeFromCodeCoverage]
    public class WebJobsExtensionStartup : IWebJobsStartup
    {
        private const string AppSettingsPolicies = "Policies";

        public void Configure(IWebJobsBuilder builder)
        {
            _ = builder ?? throw new ArgumentNullException(nameof(builder));

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            builder.Services.AddHttpClient();
            builder.Services.AddApplicationInsightsTelemetry();
            builder.Services.AddAutoMapper(typeof(WebJobsExtensionStartup).Assembly);
            builder.Services.AddSingleton(configuration.GetSection(nameof(LmiApiClientOptions)).Get<LmiApiClientOptions>() ?? new LmiApiClientOptions());
            builder.Services.AddSingleton(configuration.GetSection(nameof(JobProfileApiClientOptions)).Get<JobProfileApiClientOptions>() ?? new JobProfileApiClientOptions());
            builder.Services.AddSingleton(configuration.GetSection(nameof(GraphOptions)).Get<GraphOptions>() ?? new GraphOptions());
            builder.Services.AddGraphCluster(options => configuration.GetSection(Neo4jOptions.Neo4j).Bind(options));
            builder.Services.AddTransient<IApiConnector, ApiConnector>();
            builder.Services.AddTransient<IApiDataConnector, ApiDataConnector>();
            builder.Services.AddTransient<IGraphConnector, GraphConnector>();
            builder.Services.AddTransient<ICypherQueryBuilderService, CypherQueryBuilderService>();
            builder.Services.AddTransient<ILmiImportService, LmiImportService>();
            builder.Services.AddTransient<ILmiSocImportService, LmiSocImportService>();
            builder.Services.AddTransient<IJobProfileService, JobProfileService>();
            builder.Services.AddTransient<IJobProfilesToSocMappingService, JobProfilesToSocMappingService>();
            builder.Services.AddTransient<IGraphService, GraphService>();
            builder.Services.AddTransient<IMapLmiToGraphService, MapLmiToGraphService>();

            var policyOptions = configuration.GetSection(AppSettingsPolicies).Get<PolicyOptions>() ?? new PolicyOptions();
            var policyRegistry = builder.Services.AddPolicyRegistry();

            builder.Services
                .AddPolicies(policyRegistry, nameof(LmiApiClientOptions), policyOptions)
                .AddHttpClient<ILmiApiConnector, LmiApiConnector, LmiApiClientOptions>(configuration, nameof(LmiApiClientOptions), nameof(PolicyOptions.HttpRetry), nameof(PolicyOptions.HttpCircuitBreaker));

            builder.Services
                .AddPolicies(policyRegistry, nameof(JobProfileApiClientOptions), policyOptions)
                .AddHttpClient<IJobProfileApiConnector, JobProfileApiConnector, JobProfileApiClientOptions>(configuration, nameof(JobProfileApiClientOptions), nameof(PolicyOptions.HttpRetry), nameof(PolicyOptions.HttpCircuitBreaker));
        }
    }
}