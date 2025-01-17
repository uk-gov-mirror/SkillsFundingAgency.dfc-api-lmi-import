﻿using DFC.Api.Lmi.Import.Contracts;
using DFC.Api.Lmi.Import.Functions;
using DFC.Api.Lmi.Import.Models;
using DFC.Api.Lmi.Import.Models.ClientOptions;
using DFC.Api.Lmi.Import.Models.FunctionRequestModels;
using DFC.Api.Lmi.Import.Models.GraphData;
using DFC.Api.Lmi.Import.Models.LmiApiData;
using DFC.Api.Lmi.Import.Models.SocJobProfileMapping;
using FakeItEasy;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace DFC.Api.Lmi.Import.UnitTests.Functions
{
    [Trait("Category", "LMI import orchestration trigger function Unit Tests")]
    public class LmiImportOrchestrationTriggerTests
    {
        private readonly ILogger<LmiImportOrchestrationTrigger> fakeLogger = A.Fake<ILogger<LmiImportOrchestrationTrigger>>();
        private readonly IJobProfileService fakeJobProfileService = A.Fake<IJobProfileService>();
        private readonly IMapLmiToGraphService fakeMapLmiToGraphService = A.Fake<IMapLmiToGraphService>();
        private readonly ILmiSocImportService fakeLmiSocImportService = A.Fake<ILmiSocImportService>();
        private readonly IGraphService fakeGraphService = A.Fake<IGraphService>();
        private readonly IEventGridService fakeEventGridService = A.Fake<IEventGridService>();
        private readonly EventGridClientOptions dummyEventGridClientOptions = A.Dummy<EventGridClientOptions>();
        private readonly IDurableOrchestrationContext fakeDurableOrchestrationContext = A.Fake<IDurableOrchestrationContext>();
        private readonly SocJobProfilesMappingsCachedModel socJobProfilesMappingsCachedModel = new SocJobProfilesMappingsCachedModel();
        private readonly LmiImportOrchestrationTrigger lmiImportOrchestrationTrigger;

        public LmiImportOrchestrationTriggerTests()
        {
            lmiImportOrchestrationTrigger = new LmiImportOrchestrationTrigger(fakeLogger, fakeJobProfileService, fakeMapLmiToGraphService, fakeLmiSocImportService, fakeGraphService, fakeEventGridService, dummyEventGridClientOptions, socJobProfilesMappingsCachedModel);
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerGraphRefreshSocOrchestratorIsSuccessful()
        {
            // Arrange
            const HttpStatusCode expectedResult = HttpStatusCode.OK;
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<SocRequestModel>()).Returns(new SocRequestModel { Soc = 3435 });
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<Guid?>(nameof(LmiImportOrchestrationTrigger.ImportSocItemActivity), A<SocJobProfileMappingModel>.Ignored)).Returns(Guid.NewGuid());

            // Act
            var result = await lmiImportOrchestrationTrigger.GraphRefreshSocOrchestrator(fakeDurableOrchestrationContext).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<SocRequestModel>()).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<IList<SocJobProfileMappingModel>?>(nameof(LmiImportOrchestrationTrigger.GetJobProfileSocMappingsActivity), null)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.GraphPurgeSocActivity), A<int>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<Guid?>(nameof(LmiImportOrchestrationTrigger.ImportSocItemActivity), A<SocJobProfileMappingModel>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.PostGraphEventActivity), A<EventGridPostRequestModel>.Ignored)).MustHaveHappenedTwiceExactly();
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerGraphRefreshSocOrchestratorReturnsNullItemId()
        {
            // Arrange
            const HttpStatusCode expectedResult = HttpStatusCode.NoContent;
            Guid? nullGuid = null;
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<SocRequestModel>()).Returns(new SocRequestModel { Soc = 3435 });
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<Guid?>(nameof(LmiImportOrchestrationTrigger.ImportSocItemActivity), A<SocJobProfileMappingModel>.Ignored)).Returns(nullGuid);

            // Act
            var result = await lmiImportOrchestrationTrigger.GraphRefreshSocOrchestrator(fakeDurableOrchestrationContext).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<SocRequestModel>()).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<IList<SocJobProfileMappingModel>?>(nameof(LmiImportOrchestrationTrigger.GetJobProfileSocMappingsActivity), null)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.GraphPurgeSocActivity), A<int>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<Guid?>(nameof(LmiImportOrchestrationTrigger.ImportSocItemActivity), A<SocJobProfileMappingModel>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.PostGraphEventActivity), A<EventGridPostRequestModel>.Ignored)).MustHaveHappenedOnceExactly();
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerGraphPurgeSocOrchestratorIsSuccessful()
        {
            // Arrange
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<SocRequestModel>()).Returns(new SocRequestModel { Soc = 3435 });

            // Act
            await lmiImportOrchestrationTrigger.GraphPurgeSocOrchestrator(fakeDurableOrchestrationContext).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<SocRequestModel>()).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.GraphPurgeSocActivity), A<int>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.PostGraphEventActivity), A<EventGridPostRequestModel>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerGraphPurgeOrchestratorIsSuccessful()
        {
            // Arrange
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<OrchestratorRequestModel>()).Returns(new OrchestratorRequestModel { IsDraftEnvironment = true });

            // Act
            await lmiImportOrchestrationTrigger.GraphPurgeOrchestrator(fakeDurableOrchestrationContext).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<OrchestratorRequestModel>()).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.GraphPurgeActivity), A<object>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.PostGraphEventActivity), A<EventGridPostRequestModel>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerGraphRefreshOrchestratorIsSuccessful()
        {
            // Arrange
            const HttpStatusCode expectedResult = HttpStatusCode.OK;
            const int mappingItemsCount = 2;
            var dummyMappings = A.CollectionOfDummy<SocJobProfileMappingModel>(mappingItemsCount);
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<OrchestratorRequestModel>()).Returns(new OrchestratorRequestModel { IsDraftEnvironment = true });
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<IList<SocJobProfileMappingModel>?>(nameof(LmiImportOrchestrationTrigger.GetJobProfileSocMappingsActivity), A<object>.Ignored)).Returns(dummyMappings);
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<Guid?>(nameof(LmiImportOrchestrationTrigger.ImportSocItemActivity), A<SocJobProfileMappingModel>.Ignored)).Returns(Guid.NewGuid());

            // Act
            var result = await lmiImportOrchestrationTrigger.GraphRefreshOrchestrator(fakeDurableOrchestrationContext).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<OrchestratorRequestModel>()).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<IList<SocJobProfileMappingModel>?>(nameof(LmiImportOrchestrationTrigger.GetJobProfileSocMappingsActivity), A<object>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.GraphPurgeActivity), null)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<Guid?>(nameof(LmiImportOrchestrationTrigger.ImportSocItemActivity), A<SocJobProfileMappingModel>.Ignored)).MustHaveHappened(mappingItemsCount, Times.Exactly);
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.PostGraphEventActivity), A<EventGridPostRequestModel>.Ignored)).MustHaveHappenedOnceExactly();
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerGraphRefreshOrchestratorBadRequestWhenSuccessThresholdNotMet()
        {
            // Arrange
            const HttpStatusCode expectedResult = HttpStatusCode.BadRequest;
            const int mappingItemsCount = 2;
            var dummyMappings = A.CollectionOfDummy<SocJobProfileMappingModel>(mappingItemsCount);
            Guid? nullGuid = null;
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<OrchestratorRequestModel>()).Returns(new OrchestratorRequestModel { IsDraftEnvironment = true, SuccessRelayPercent = 99 });
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<IList<SocJobProfileMappingModel>?>(nameof(LmiImportOrchestrationTrigger.GetJobProfileSocMappingsActivity), A<object>.Ignored)).Returns(dummyMappings);
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<Guid?>(nameof(LmiImportOrchestrationTrigger.ImportSocItemActivity), A<SocJobProfileMappingModel>.Ignored)).Returns(nullGuid);

            // Act
            var result = await lmiImportOrchestrationTrigger.GraphRefreshOrchestrator(fakeDurableOrchestrationContext).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<OrchestratorRequestModel>()).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<IList<SocJobProfileMappingModel>?>(nameof(LmiImportOrchestrationTrigger.GetJobProfileSocMappingsActivity), A<object>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.GraphPurgeActivity), null)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<Guid?>(nameof(LmiImportOrchestrationTrigger.ImportSocItemActivity), A<SocJobProfileMappingModel>.Ignored)).MustHaveHappened(mappingItemsCount, Times.Exactly);
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.PostGraphEventActivity), A<EventGridPostRequestModel>.Ignored)).MustNotHaveHappened();
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerGraphRefreshOrchestratorIsSuccessfulWhenNoMappings()
        {
            // Arrange
            const HttpStatusCode expectedResult = HttpStatusCode.NoContent;
            const int mappingItemsCount = 0;
            var dummyMappings = A.CollectionOfDummy<SocJobProfileMappingModel>(mappingItemsCount);
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<OrchestratorRequestModel>()).Returns(new OrchestratorRequestModel { IsDraftEnvironment = true });
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<IList<SocJobProfileMappingModel>?>(nameof(LmiImportOrchestrationTrigger.GetJobProfileSocMappingsActivity), A<object>.Ignored)).Returns(dummyMappings);

            // Act
            var result = await lmiImportOrchestrationTrigger.GraphRefreshOrchestrator(fakeDurableOrchestrationContext).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<OrchestratorRequestModel>()).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<IList<SocJobProfileMappingModel>?>(nameof(LmiImportOrchestrationTrigger.GetJobProfileSocMappingsActivity), A<object>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.GraphPurgeActivity), null)).MustNotHaveHappened();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<Guid?>(nameof(LmiImportOrchestrationTrigger.ImportSocItemActivity), A<SocJobProfileMappingModel>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.PostGraphEventActivity), A<EventGridPostRequestModel>.Ignored)).MustNotHaveHappened();
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerGraphRefreshOrchestratorIsSuccessfulWhenNullMappings()
        {
            // Arrange
            const HttpStatusCode expectedResult = HttpStatusCode.NoContent;
            List<SocJobProfileMappingModel>? nullMappings = null;
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<OrchestratorRequestModel>()).Returns(new OrchestratorRequestModel { IsDraftEnvironment = true });
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<IList<SocJobProfileMappingModel>?>(nameof(LmiImportOrchestrationTrigger.GetJobProfileSocMappingsActivity), A<object>.Ignored)).Returns(nullMappings);

            // Act
            var result = await lmiImportOrchestrationTrigger.GraphRefreshOrchestrator(fakeDurableOrchestrationContext).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeDurableOrchestrationContext.GetInput<OrchestratorRequestModel>()).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<IList<SocJobProfileMappingModel>?>(nameof(LmiImportOrchestrationTrigger.GetJobProfileSocMappingsActivity), A<object>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.GraphPurgeActivity), null)).MustNotHaveHappened();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync<Guid?>(nameof(LmiImportOrchestrationTrigger.ImportSocItemActivity), A<SocJobProfileMappingModel>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeDurableOrchestrationContext.CallActivityAsync(nameof(LmiImportOrchestrationTrigger.PostGraphEventActivity), A<EventGridPostRequestModel>.Ignored)).MustNotHaveHappened();
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerGetJobProfileSocMappingsActivityIsSuccessful()
        {
            // Arrange
            const int mappingItemsCount = 2;
            var dummyMappings = A.CollectionOfDummy<SocJobProfileMappingModel>(mappingItemsCount);
            A.CallTo(() => fakeJobProfileService.GetMappingsAsync()).Returns(dummyMappings);

            // Act
            var results = await lmiImportOrchestrationTrigger.GetJobProfileSocMappingsActivity(null).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeJobProfileService.GetMappingsAsync()).MustHaveHappenedOnceExactly();
            Assert.Equal(dummyMappings, results);
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerGraphPurgeActivityIsSuccessful()
        {
            // Arrange

            // Act
            await lmiImportOrchestrationTrigger.GraphPurgeActivity(null).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeGraphService.PurgeAsync()).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerGraphPurgeSocActivityIsSuccessful()
        {
            // Arrange

            // Act
            await lmiImportOrchestrationTrigger.GraphPurgeSocActivity(1234).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeGraphService.PurgeSocAsync(A<int>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerImportSocItemActivityIsSuccessful()
        {
            // Arrange
            var dummyLmiSocDatasetModel = A.Dummy<LmiSocDatasetModel>();
            var dummyGraphSocDatasetModel = A.Dummy<GraphSocDatasetModel>();
            var socJobProfileMapping = new SocJobProfileMappingModel { Soc = 1234 };

            A.CallTo(() => fakeLmiSocImportService.ImportAsync(A<int>.Ignored, A<List<SocJobProfileItemModel>>.Ignored)).Returns(dummyLmiSocDatasetModel);
            A.CallTo(() => fakeMapLmiToGraphService.Map(A<LmiSocDatasetModel>.Ignored)).Returns(dummyGraphSocDatasetModel);
            A.CallTo(() => fakeGraphService.ImportAsync(A<GraphSocDatasetModel>.Ignored)).Returns(true);

            // Act
            var result = await lmiImportOrchestrationTrigger.ImportSocItemActivity(socJobProfileMapping).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeLmiSocImportService.ImportAsync(A<int>.Ignored, A<List<SocJobProfileItemModel>>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeMapLmiToGraphService.Map(A<LmiSocDatasetModel>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeGraphService.ImportAsync(A<GraphSocDatasetModel>.Ignored)).MustHaveHappenedOnceExactly();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerImportSocItemActivityReturnsNullItemId()
        {
            // Arrange
            LmiSocDatasetModel? nullLmiSocDatasetModel = null;
            var socJobProfileMapping = new SocJobProfileMappingModel { Soc = 1234 };

            A.CallTo(() => fakeLmiSocImportService.ImportAsync(A<int>.Ignored, A<List<SocJobProfileItemModel>>.Ignored)).Returns(nullLmiSocDatasetModel);

            // Act
            var result = await lmiImportOrchestrationTrigger.ImportSocItemActivity(socJobProfileMapping).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeLmiSocImportService.ImportAsync(A<int>.Ignored, A<List<SocJobProfileItemModel>>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeMapLmiToGraphService.Map(A<LmiSocDatasetModel>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeGraphService.ImportAsync(A<GraphSocDatasetModel>.Ignored)).MustNotHaveHappened();
            Assert.Null(result);
        }

        [Fact]
        public async Task LmiImportOrchestrationTriggerPostGraphEventActivityIsSuccessful()
        {
            // Arrange
            var eventGridPostRequest = new EventGridPostRequestModel
            {
                ItemId = Guid.NewGuid(),
                DisplayText = "Display text",
                EventType = "published",
            };

            // Act
            await lmiImportOrchestrationTrigger.PostGraphEventActivity(eventGridPostRequest).ConfigureAwait(false);

            // Assert
            A.CallTo(() => fakeEventGridService.SendEventAsync(A<EventGridEventData>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }
    }
}
