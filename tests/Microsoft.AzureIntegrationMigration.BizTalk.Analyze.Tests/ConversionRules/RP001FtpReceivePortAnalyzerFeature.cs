// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using System;
using System.Threading;
using FluentAssertions;
using Microsoft.AzureIntegrationMigration.ApplicationModel;
using Microsoft.AzureIntegrationMigration.BizTalk.Analyze.ConversionRules;
using Microsoft.AzureIntegrationMigration.BizTalk.Types.Entities;
using Microsoft.AzureIntegrationMigration.Runner.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Xbehave;
using Xunit;

#pragma warning disable CA1303 // Do not pass literals as localized parameters
namespace Microsoft.AzureIntegrationMigration.BizTalk.Analyze.Tests.ConversionRules
{
    /// <summary>
    /// Tests for the <see cref="RP001FtpReceivePortAnalyzer"/> class.
    /// </summary>
    public class RP001FtpReceivePortAnalyzerFeature
    {
        /// <summary>
        /// Defines a mocked logger.
        /// </summary>
        private Mock<ILogger> _mockLogger;

        #region Before Each Scenario

        /// <summary>
        /// Sets up state before each scenario.
        /// </summary>
        [Background]
        public void Setup()
        {
            "Given a new mock logger"
                .x(() => _mockLogger = new Mock<ILogger>());
        }

        #endregion

        #region Constructor Scenarios

        /// <summary>
        /// Scenario tests that the object construction throws an exception when a null model is passed.
        /// </summary>
        /// <param name="analyzer">The analyzer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="model">The application model.</param>
        /// <param name="context">The migration context.</param>
        /// <param name="e">The thrown exception, if any.</param>
        [Scenario]
        [Trait(TestConstants.TraitCategory, TestConstants.CategoryUnitTest)]
        public void ConstructWithNullModel(RP001FtpReceivePortAnalyzer analyzer, ILogger logger, IApplicationModel model, MigrationContext context, Exception e)
        {
            "Given an analyzer"
                .x(() => analyzer.Should().BeNull());

            "And null model"
                .x(() => model.Should().BeNull());

            "And a context"
                .x(() => context = TestHelper.BuildContext());

            "And a logger"
                .x(() => logger = _mockLogger.Object);

            "When constructing with a null model"
                .x(() => e = Record.Exception(() => new RP001FtpReceivePortAnalyzer(model, context, logger)));

            "Then the constructor should throw an exception"
                .x(() => e.Should().NotBeNull().And.Subject.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("model"));
        }

        /// <summary>
        /// Scenario tests that the object construction throws an exception when a null context is passed.
        /// </summary>
        /// <param name="analyzer">The analyzer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="model">The application model.</param>
        /// <param name="context">The migration context.</param>
        /// <param name="e">The thrown exception, if any.</param>
        [Scenario]
        [Trait(TestConstants.TraitCategory, TestConstants.CategoryUnitTest)]
        public void ConstructWithNullContext(RP001FtpReceivePortAnalyzer analyzer, ILogger logger, IApplicationModel model, MigrationContext context, Exception e)
        {
            "Given an analyzer"
                .x(() => analyzer.Should().BeNull());

            "And a model"
                .x(() => model = new AzureIntegrationServicesModel());

            "And null context"
                .x(() => context.Should().BeNull());

            "And a logger"
                .x(() => logger = _mockLogger.Object);

            "When constructing with a null context"
                .x(() => e = Record.Exception(() => new RP001FtpReceivePortAnalyzer(model, context, logger)));

            "Then the constructor should throw an exception"
                .x(() => e.Should().NotBeNull().And.Subject.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("context"));
        }

        /// <summary>
        /// Scenario tests that the object construction throws an exception when a null logger is passed.
        /// </summary>
        /// <param name="analyzer">The analyzer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="model">The application model.</param>
        /// <param name="context">The migration context.</param>
        /// <param name="e">The thrown exception, if any.</param>
        [Scenario]
        [Trait(TestConstants.TraitCategory, TestConstants.CategoryUnitTest)]
        public void ConstructWithNullLogger(RP001FtpReceivePortAnalyzer analyzer, ILogger logger, IApplicationModel model, MigrationContext context, Exception e)
        {
            "Given an analyzer"
                .x(() => analyzer.Should().BeNull());

            "And a model"
                .x(() => model = new AzureIntegrationServicesModel());

            "And a context"
                .x(() => context = TestHelper.BuildContext());

            "And null logger"
                .x(() => logger.Should().BeNull());

            "When constructing with a null logger"
                .x(() => e = Record.Exception(() => new RP001FtpReceivePortAnalyzer(model, context, logger)));

            "Then the constructor should throw an exception"
                .x(() => e.Should().NotBeNull().And.Subject.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("logger"));
        }

        /// <summary>
        /// Scenario tests that the object construction works when params ok.
        /// </summary>
        /// <param name="analyzer">The analyzer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="model">The application model.</param>
        /// <param name="context">The migration context.</param>
        /// <param name="e">The thrown exception, if any.</param>
        [Scenario]
        [Trait(TestConstants.TraitCategory, TestConstants.CategoryUnitTest)]
        public void ConstructWithSuccess(RP001FtpReceivePortAnalyzer analyzer, ILogger logger, IApplicationModel model, MigrationContext context, Exception e)
        {
            "Given an analyzer"
                .x(() => analyzer.Should().BeNull());

            "And a model"
                .x(() => model = new AzureIntegrationServicesModel());

            "And a context"
                .x(() => context = TestHelper.BuildContext());

            "And a logger"
                .x(() => logger = _mockLogger.Object);

            "When constructing"
                .x(() => e = Record.Exception(() => new RP001FtpReceivePortAnalyzer(model, context, logger)));

            "Then the constructor should NOT throw an exception"
                .x(() => e.Should().BeNull());
        }

        #endregion

        #region RP001 Tests

        /// <summary>
        /// Scenario tests that the endpoint is created on the migration target.
        /// </summary>
        /// <param name="analyzer">The analyzer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="model">The application model.</param>
        /// <param name="context">The migration context.</param>
        /// <param name="e">The thrown exception, if any.</param>
        [Scenario]
        [Trait(TestConstants.TraitCategory, TestConstants.CategoryUnitTest)]
        public void CreateFtpEndpointSuccess(RP001FtpReceivePortAnalyzer analyzer, ILogger logger, AzureIntegrationServicesModel model, MigrationContext context, Exception e)
        {
            "Given an analyzer"
                .x(() => analyzer.Should().BeNull());

            "And a model"
                .x(() => model = new AzureIntegrationServicesModel());

            "And a model"
             .x(() => model = TestHelper.CreateDefaultModelForAnalyzing());

            "And the model has a migration target "
               .x(() =>
               {
                   TestHelper.CopySourceToTarget(model, includeFtpReceive: true);
               });

            "And a context"
                .x(() => context = TestHelper.BuildContext());

            "And a logger"
                .x(() => logger = _mockLogger.Object);

            "And an analyzer"
                .x(() => analyzer = new RP001FtpReceivePortAnalyzer(model, context, logger));

            "When analyzing"
                .x(async () => e = await Record.ExceptionAsync(async () => await analyzer.AnalyzeAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false));

            "Then the constructor should NOT throw an exception"
                .x(() => e.Should().BeNull());

            "And the message bus will have been created"
                .x(() =>
                {
                    model.Should().NotBeNull();
                    model.MigrationTarget.Should().NotBeNull();
                    model.MigrationTarget.MessageBus.Should().NotBeNull();
                    model.MigrationTarget.MessageBus.Applications.Should().HaveCount(3);
                    
                    model.MigrationTarget.MessageBus.Applications[0].Endpoints.Should().HaveCount(1);
                    model.MigrationTarget.MessageBus.Applications[0].Endpoints[0].Name.Should().Be("FTP Receive Adapter");
                    model.MigrationTarget.MessageBus.Applications[0].Endpoints[0].Key.Should().Be("MessageBus:TestApp1:ReceivePort1:ReceiveLocation1:AdapterEndpoint");
                    model.MigrationTarget.MessageBus.Applications[0].Endpoints[0].ResourceMapKey.Should().Be("ftpReceiveAdapterEndpointTestApp1FTPReceiveAdapter");

                    model.MigrationTarget.MessageBus.Applications[0].Endpoints[0].Properties.Should().HaveCount(13);
                    model.MigrationTarget.MessageBus.Applications[0].Endpoints[0].ReportLinks.Should().HaveCount(1);
                    model.MigrationTarget.MessageBus.Applications[0].Endpoints[0].ReportMessages.Should().HaveCount(16);
                });
        }

        /// <summary>
        /// Scenario tests that the endpoint is created on the migration target with no transport data.
        /// </summary>
        /// <param name="analyzer">The analyzer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="model">The application model.</param>
        /// <param name="context">The migration context.</param>
        /// <param name="e">The thrown exception, if any.</param>
        [Scenario]
        [Trait(TestConstants.TraitCategory, TestConstants.CategoryUnitTest)]
        public void FtpEndpointMissingWithError(RP001FtpReceivePortAnalyzer analyzer, ILogger logger, AzureIntegrationServicesModel model, MigrationContext context, Exception e)
        {
            "Given an analyzer"
                .x(() => analyzer.Should().BeNull());

            "And an empty model"
                .x(() => model = new AzureIntegrationServicesModel());

            "And a populated model"
                .x(() => model = TestHelper.CreateDefaultModelForAnalyzing());

            "And the model has a migration target"
                .x(() =>
                {
                    TestHelper.CopySourceToTarget(model, includeFtpReceive: true);
                });

            "And the target model is missing the endpoint"
                .x(() =>
                {
                    model.MigrationTarget.MessageBus.Applications[0].Endpoints.Clear();
                });

            "And a context"
                .x(() => context = TestHelper.BuildContext());

            "And a logger"
                .x(() => logger = _mockLogger.Object);

            "And an analyzer"
                .x(() => analyzer = new RP001FtpReceivePortAnalyzer(model, context, logger));

            "When analyzing"
                .x(async () => e = await Record.ExceptionAsync(async () => await analyzer.AnalyzeAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false));

            "Then the constructor should NOT throw an exception"
                .x(() => e.Should().BeNull());

            "And an error should be raised"
                .x(() =>
                {
                    model.Should().NotBeNull();
                    model.MigrationTarget.Should().NotBeNull();
                    model.MigrationTarget.MessageBus.Should().NotBeNull();
                    model.MigrationTarget.MessageBus.Applications.Should().HaveCount(3);

                    model.MigrationTarget.MessageBus.Applications[0].Endpoints.Should().HaveCount(0);

                    context.Errors.Should().HaveCount(1);
                });
        }

        /// <summary>
        /// Scenario tests that the endpoint is created on the migration target with empty transport data.
        /// </summary>
        /// <param name="analyzer">The analyzer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="model">The application model.</param>
        /// <param name="context">The migration context.</param>
        /// <param name="e">The thrown exception, if any.</param>
        [Scenario]
        [Trait(TestConstants.TraitCategory, TestConstants.CategoryUnitTest)]
        public void CreateFtpEndpointEmptyTransportData(RP001FtpReceivePortAnalyzer analyzer, ILogger logger, AzureIntegrationServicesModel model, MigrationContext context, Exception e)
        {
            "Given an analyzer"
                .x(() => analyzer.Should().BeNull());

            "And a model"
                .x(() => model = new AzureIntegrationServicesModel());

            "And a model"
             .x(() => model = TestHelper.CreateDefaultModelForAnalyzing());

            "And the model receive location has no TransportTypeData"
             .x(() =>
             {
                 model.GetSourceModel<ParsedBizTalkApplicationGroup>().Applications[0].Application.Bindings.BindingInfo.ReceivePortCollection[0].ReceiveLocations[0].ReceiveLocationTransportTypeData = "<CustomProps><AdapterConfig vt=\"8\"><Config xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"></Config></AdapterConfig></CustomProps>";
             });

            "And the model has a migration target "
               .x(() =>
               {
                   TestHelper.CopySourceToTarget(model, includeFtpReceive: true);
               });

            "And a context"
                .x(() => context = TestHelper.BuildContext());

            "And a logger"
                .x(() => logger = _mockLogger.Object);

            "And an analyzer"
                .x(() => analyzer = new RP001FtpReceivePortAnalyzer(model, context, logger));

            "When analyzing"
                .x(async () => e = await Record.ExceptionAsync(async () => await analyzer.AnalyzeAsync(CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false));

            "Then the constructor should NOT throw an exception"
                .x(() => e.Should().BeNull());

            "And the message bus will have been created"
                .x(() =>
                {
                    model.Should().NotBeNull();
                    model.MigrationTarget.Should().NotBeNull();
                    model.MigrationTarget.MessageBus.Should().NotBeNull();
                    model.MigrationTarget.MessageBus.Applications.Should().HaveCount(3);

                    model.MigrationTarget.MessageBus.Applications[0].Endpoints.Should().HaveCount(1);
                    model.MigrationTarget.MessageBus.Applications[0].Endpoints[0].Name.Should().Be("FTP Receive Adapter");
                    model.MigrationTarget.MessageBus.Applications[0].Endpoints[0].Key.Should().Be("MessageBus:TestApp1:ReceivePort1:ReceiveLocation1:AdapterEndpoint");
                    model.MigrationTarget.MessageBus.Applications[0].Endpoints[0].ResourceMapKey.Should().Be("ftpReceiveAdapterEndpointTestApp1FTPReceiveAdapter");

                    model.MigrationTarget.MessageBus.Applications[0].Endpoints[0].Properties.Should().HaveCount(13);
                    model.MigrationTarget.MessageBus.Applications[0].Endpoints[0].ReportLinks.Should().HaveCount(1);
                    model.MigrationTarget.MessageBus.Applications[0].Endpoints[0].ReportMessages.Should().HaveCount(1);
                });
        }
        #endregion
    }
}
#pragma warning restore CA1303 // Do not pass literals as localized parameters
