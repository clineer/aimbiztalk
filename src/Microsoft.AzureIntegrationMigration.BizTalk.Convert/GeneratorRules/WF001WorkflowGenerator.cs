// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AzureIntegrationMigration.ApplicationModel.Renderer;
using Microsoft.AzureIntegrationMigration.ApplicationModel.Target;
using Microsoft.AzureIntegrationMigration.ApplicationModel.Target.Intermediaries;
using Microsoft.AzureIntegrationMigration.BizTalk.Convert.Repositories;
using Microsoft.AzureIntegrationMigration.BizTalk.Convert.Resources;
using Microsoft.AzureIntegrationMigration.BizTalk.Types;
using Microsoft.AzureIntegrationMigration.BizTalk.Types.Entities;
using Microsoft.AzureIntegrationMigration.Runner.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AzureIntegrationMigration.BizTalk.Convert.GeneratorRules
{
    /// <summary>
    /// Defines a class that implements a converter that generates a workflow from snippets.
    /// </summary>
    public sealed class WF001WorkflowGenerator : BizTalkConverterBase
    {
        /// <summary>
        /// Defines the name of this rule.
        /// </summary>
        private const string RuleName = "WF001";

        /// <summary>
        /// Key for a default converter.
        /// </summary>
        private const string DefaultConverter = "DefaultConverter";

        /// <summary>
        /// Defines a file repository.
        /// </summary>
        private readonly IFileRepository _fileRepository;

        /// <summary>
        /// Defines a template repository.
        /// </summary>
        private readonly ITemplateRepository _repository;

        /// <summary>
        /// Defines a snippet renderer.
        /// </summary>
        private readonly ISnippetRenderer _snippetRenderer;

        /// <summary>
        /// Defines a logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Defines a unique index used to apply to an action name when generating a workflow definition.
        /// </summary>
        private int _index;

        /// <summary>
        /// Defines a mapping between workflow activity container types and handlers used to convert to a Logic App action.
        /// </summary>
        private readonly IDictionary<string, ConvertActivityContainerHandler> _activityContainerConverters = new Dictionary<string, ConvertActivityContainerHandler>();

        /// <summary>
        /// Defines a mapping between workflow activity types and handlers used to convert to a Logic App action or trigger.
        /// </summary>
        private readonly IDictionary<string, ConvertActivityHandler> _activityConverters = new Dictionary<string, ConvertActivityHandler>();

        /// <summary>
        /// Creates a new instance of a <see cref="WF001WorkflowGenerator"/> class.
        /// </summary>
        /// <param name="fileRepository">The file repository.</param>
        /// <param name="repository">The repository.</param>
        /// <param name="snippetRenderer">The snippet renderer.</param>
        /// <param name="model">The model.</param>
        /// <param name="context">The context.</param>
        /// <param name="logger">A logger.</param>
        public WF001WorkflowGenerator(IFileRepository fileRepository, ITemplateRepository repository, ISnippetRenderer snippetRenderer, IApplicationModel model, MigrationContext context, ILogger logger)
            : base(nameof(WF001WorkflowGenerator), model, context, logger)
        {
            // Validate and set the member
            _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _snippetRenderer = snippetRenderer ?? throw new ArgumentNullException(nameof(snippetRenderer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize convert mapping
            InitializeConverters();
        }

        /// <summary>
        /// Initializes the mapping between activity container and activity types to converters.
        /// </summary>
        private void InitializeConverters()
        {
            // Initialize the activity container converters
            _activityContainerConverters.Add(DefaultConverter, ConvertActivityContainer);
            _activityContainerConverters.Add(WorkflowModelConstants.ActivityTypeDecisionBranch, ConvertDecisionBranchActivityContainer);

            // Initialize the activity converters
            _activityConverters.Add(DefaultConverter, ConvertActivity);
            _activityConverters.Add(WorkflowModelConstants.ActivityTypeReceive, ConvertReceiveActivity);
            _activityConverters.Add(WorkflowModelConstants.ActivityTypeSend, ConvertSendActivity);
        }

        /// <summary>
        /// Generate schema files for each application in the target under the resource output path.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Task used to await the operation.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "The folder paths are lowercased, so must use a lowercase function.")]
        protected override async Task ConvertInternalAsync(CancellationToken token)
        {
            if (Model.MigrationTarget.MessageBus?.Applications != null)
            {
                _logger.LogDebug(TraceMessages.RunningGenerator, RuleName, nameof(WF001WorkflowGenerator));

                foreach (var targetApplication in Model.MigrationTarget.MessageBus.Applications)
                {
                    var workflows = targetApplication.Intermediaries.Where(i => i is ProcessManager).Select(i => (ProcessManager)i);
                    if (workflows != null && workflows.Any())
                    {
                        foreach (var workflow in workflows)
                        {
                            await BuildProcessManager(workflow).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        _logger.LogDebug(TraceMessages.NoWorkflowsToGenerate, RuleName, targetApplication.Name);
                    }
                }

                _logger.LogDebug(TraceMessages.GeneratorCompleted, RuleName, nameof(WF001WorkflowGenerator));
            }
            else
            {
                _logger.LogDebug(TraceMessages.SkippingRuleAsMigrationTargetMessageBusMissing, RuleName, nameof(WF001WorkflowGenerator));
            }
        }

        /// <summary>
        /// Builds an instance of a process manager intermediary using snippets.
        /// </summary>
        /// <remarks>
        /// As a process manager is a worklow related intermediary, this rule assumes that the workflow implementation
        /// in AIS will be a Logic App.  This code will therefore expect the snippets to be individual parts of a
        /// Logic App that need to be stitched together to form a single Logic App.
        /// </remarks>
        /// <param name="processManager">The process manager to build.</param>
        private async Task BuildProcessManager(ProcessManager processManager)
        {
            // Check to see if there are any snippets associated with the process manager
            if (processManager.Snippets.Any())
            {
                if (processManager.WorkflowModel != null)
                {
                    // Get snippet paths
                    var snippetPaths = Context.TemplateFolders.Select(p => new DirectoryInfo(p));
                    if (snippetPaths.Any())
                    {
                        // Ensure generation path exists
                        var generationPath = new DirectoryInfo(Context.GenerationFolder);
                        if (!_fileRepository.DoesDirectoryExist(generationPath.FullName))
                        {
                            _logger.LogDebug(TraceMessages.CreatingGenerationPath, generationPath.FullName);

                            _fileRepository.CreateDirectory(generationPath.FullName);
                        }

                        // Find resource template for the process manager
                        var resourceTemplate = processManager.Resources.Where(r => r.ResourceType == ModelConstants.ResourceTypeAzureLogicApp).SingleOrDefault();
                        if (resourceTemplate != null)
                        {
                            // Find snippet for workflow definition
                            var workflowResource = processManager.Snippets.Where(s => s.ResourceType == ModelConstants.ResourceTypeWorkflowDefinition).FirstOrDefault();
                            if (workflowResource != null)
                            {
                                _logger.LogDebug(TraceMessages.GeneratingWorkflow, RuleName, processManager.Name);

                                // Generate skeleton Logic App in which we will add snippets based on the workflow model
                                var workflowDefinition = await LoadSnippet(processManager, processManager.WorkflowModel, resourceTemplate, workflowResource, snippetPaths, generationPath).ConfigureAwait(false);
                                if (workflowDefinition != null)
                                {
                                    _index = 0;

                                    // Add parameters
                                    await AddParameters(processManager, workflowDefinition, resourceTemplate, snippetPaths, generationPath).ConfigureAwait(false);

                                    // Add properties
                                    await AddProperties(processManager, workflowDefinition, resourceTemplate, snippetPaths, generationPath).ConfigureAwait(false);

                                    // Add triggers
                                    await AddTriggers(processManager, workflowDefinition, resourceTemplate, snippetPaths, generationPath).ConfigureAwait(false);

                                    // Add variables
                                    await AddVariables(processManager, workflowDefinition, resourceTemplate, snippetPaths, generationPath).ConfigureAwait(false);

                                    // Add messages
                                    await AddMessages(processManager, workflowDefinition, resourceTemplate, snippetPaths, generationPath).ConfigureAwait(false);

                                    // Add actions
                                    var actionsPath = "$..definition.actions";
                                    var container = (JObject)workflowDefinition.SelectToken(actionsPath);
                                    if (container != null)
                                    {
                                        // Add actions for workflow activities
                                        await AddWorkflowActivities(processManager, workflowDefinition, container, processManager.WorkflowModel, resourceTemplate, snippetPaths, generationPath).ConfigureAwait(false);

                                        // Bind actions
                                        BindActions(processManager, container);

                                        // Save workflow definition to path where process manager is loaded in the resource template
                                        var workflowFilePath = (string)resourceTemplate.Parameters["workflow_definition_file"];
                                        SaveWorkflow(processManager, workflowDefinition, generationPath, workflowFilePath);
                                    }
                                    else
                                    {
                                        _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, actionsPath);
                                        Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, actionsPath)));
                                    }
                                }
                            }
                            else
                            {
                                _logger.LogWarning(WarningMessages.ProcessManagerSnippetNotFound, processManager.Name, ModelConstants.ResourceTypeWorkflowDefinition);
                            }

                            // Find snippet for parameters definition
                            var parametersResource = processManager.Snippets.Where(s => s.ResourceType == ModelConstants.ResourceTypeWorkflowParametersDefinition).FirstOrDefault();
                            if (parametersResource != null)
                            {
                                // Generate skeleton Logic App ARM Parameters file to add parameters
                                var parametersDefinition = await LoadSnippet(processManager, processManager.WorkflowModel, resourceTemplate, parametersResource, snippetPaths, generationPath).ConfigureAwait(false);
                                if (parametersDefinition != null)
                                {
                                    // Add parameters
                                    await AddArmParameters(processManager, parametersDefinition, resourceTemplate, snippetPaths, generationPath).ConfigureAwait(false);
                                }

                                // Save parameter file to the path in the resource template
                                var parametersFilePath = (string)resourceTemplate.Parameters["workflow_parameters_file"];
                                SaveWorkflow(processManager, parametersDefinition, generationPath, parametersFilePath);
                            }
                            else
                            {
                                _logger.LogWarning(WarningMessages.ProcessManagerSnippetNotFound, processManager.Name, ModelConstants.ResourceTypeWorkflowParametersDefinition);
                            }
                        }
                        else
                        {
                            _logger.LogWarning(WarningMessages.ProcessManagerResourceTemplateNotFound, processManager.Name, ModelConstants.ResourceTypeAzureLogicApp);
                        }
                    }
                    else
                    {
                        _logger.LogWarning(WarningMessages.NoTemplatePathsFound);
                    }
                }
                else
                {
                    _logger.LogError(ErrorMessages.ProcessManagerMissingWorkflowModel, processManager.Name);
                    Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.ProcessManagerMissingWorkflowModel, processManager.Name)));
                }
            }
            else
            {
                _logger.LogWarning(WarningMessages.ProcessManagerSnippetsNotFound, processManager.Name);
            }
        }

        /// <summary>
        /// Adds workflow parameters to the workflow definition.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        private async Task AddParameters(ProcessManager processManager, JObject workflowDefinition, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            var parameterSnippets = processManager.Snippets.Where(s => s.ResourceType == ModelConstants.ResourceTypeWorkflowParameter).ToList();
            if (parameterSnippets != null && parameterSnippets.Any())
            {
                foreach (var parameterSnippet in parameterSnippets)
                {
                    var snippet = await LoadSnippet(processManager, processManager.WorkflowModel, resourceTemplate, parameterSnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
                    if (snippet != null && snippet.HasValues)
                    {
                        // Workflow definition parameter (parameter inside the Logic App definition)
                        var definitionParametersPath = "$..definition.parameters";
                        var definitionParameters = (JObject)workflowDefinition.SelectToken(definitionParametersPath);
                        if (definitionParameters != null)
                        {
                            if (snippet.ContainsKey("workflowDefinitionParameter"))
                            {
                                var parameter = (JProperty)snippet["workflowDefinitionParameter"].First();

                                // Does it already exist?
                                if (!definitionParameters.ContainsKey(parameter.Name))
                                {
                                    // Doesn't exist, add to the parameters
                                    definitionParameters.Add(parameter.Name, parameter.Value);
                                }

                                _logger.LogDebug(TraceMessages.AddedWorkflowParameterToWorkflowDefinition, RuleName, parameter.Name);
                            }
                        }
                        else
                        {
                            _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, definitionParametersPath);
                            Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, definitionParametersPath)));
                        }

                        // Workflow parameter (parameter inside the Logic App workflow resource)
                        var workflowParametersPath = "$.resources[?(@.type == 'Microsoft.Logic/workflows')].properties.parameters";
                        var workflowParameters = (JObject)workflowDefinition.SelectToken(workflowParametersPath);
                        if (workflowParameters != null)
                        {
                            if (snippet.ContainsKey("workflowParameter"))
                            {
                                var parameter = (JProperty)snippet["workflowParameter"].First();

                                // Does it already exist?
                                if (workflowParameters.ContainsKey(parameter.Name))
                                {
                                    // Get value object of existing parameter
                                    var existingValue = workflowParameters[parameter.Name].SelectToken("$..value");
                                    if (existingValue is JObject)
                                    {
                                        var existingValueObject = (JObject)existingValue;

                                        // Incoming parameter already exists, add all the value properties to the existing parameter, if each property
                                        // doesn't exist and incoming value is also an object (to allow multiple properties).
                                        var value = parameter.Value.SelectToken("$..value");
                                        if (value is JObject)
                                        {
                                            // Add value object properties to existing parameter
                                            var valueObject = (JObject)value;
                                            foreach (var valueProperty in valueObject.Properties())
                                            {
                                                if (!existingValueObject.ContainsKey(valueProperty.Name))
                                                {
                                                    existingValueObject.Add(valueProperty.Name, valueProperty.Value);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Doesn't exist, add to the parameters
                                    workflowParameters.Add(parameter.Name, parameter.Value);
                                }

                                _logger.LogDebug(TraceMessages.AddedWorkflowParameterToResourceParameters, RuleName, parameter.Name);
                            }
                        }
                        else
                        {
                            _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, workflowParametersPath);
                            Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, workflowParametersPath)));
                        }

                        // ARM template parameter (parameter inside the ARM template)
                        var armTemplateParametersPath = "$.parameters";
                        var armTemplateParameters = (JObject)workflowDefinition.SelectToken(armTemplateParametersPath);
                        if (armTemplateParameters != null)
                        {
                            if (snippet.ContainsKey("armTemplateParameter"))
                            {
                                var parameter = (JProperty)snippet["armTemplateParameter"].First();
                                armTemplateParameters.Add(parameter.Name, parameter.Value);

                                _logger.LogDebug(TraceMessages.AddedWorkflowParameterToArmTemplate, RuleName, parameter.Name);
                            }
                        }
                        else
                        {
                            _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, armTemplateParametersPath);
                            Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, armTemplateParametersPath)));
                        }
                    }
                }
            }
            else
            {
                _logger.LogDebug(TraceMessages.WorkflowParameterSnippetsNotFound, RuleName, processManager.Name);
            }
        }

        /// <summary>
        /// Adds ARM parameters to the parameters definition.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="parametersDefinition">The workflow parameters definition.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        private async Task AddArmParameters(ProcessManager processManager, JObject parametersDefinition, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            var parameterSnippets = processManager.Snippets.Where(s => s.ResourceType == ModelConstants.ResourceTypeWorkflowParameter).ToList();
            if (parameterSnippets != null && parameterSnippets.Any())
            {
                var parametersPath = "$.parameters";
                var parameters = (JObject)parametersDefinition.SelectToken(parametersPath);
                if (parameters != null)
                {
                    foreach (var parameterSnippet in parameterSnippets)
                    {
                        var snippet = await LoadSnippet(processManager, processManager.WorkflowModel, resourceTemplate, parameterSnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
                        if (snippet != null && snippet.HasValues)
                        {
                            if (snippet.ContainsKey("armParameter"))
                            {
                                var parameter = (JProperty)snippet["armParameter"].First();
                                parameters.Add(parameter.Name, parameter.Value);

                                _logger.LogDebug(TraceMessages.AddedWorkflowParameterToArmParameters, RuleName, parameter.Name);
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogError(ErrorMessages.UnableToFindNodeInParametersDefinition, processManager.Name, parametersPath);
                    Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInParametersDefinition, processManager.Name, parametersPath)));
                }
            }
            else
            {
                _logger.LogDebug(TraceMessages.WorkflowParameterSnippetsNotFound, RuleName, processManager.Name);
            }
        }

        /// <summary>
        /// Adds workflow properties to the workflow definition.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        private async Task AddProperties(ProcessManager processManager, JObject workflowDefinition, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            var propertySnippets = processManager.Snippets.Where(s => s.ResourceType == ModelConstants.ResourceTypeWorkflowProperty).ToList();
            if (propertySnippets != null && propertySnippets.Any())
            {
                foreach (var propertySnippet in propertySnippets)
                {
                    var snippet = await LoadSnippet(processManager, processManager.WorkflowModel, resourceTemplate, propertySnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
                    if (snippet != null && snippet.HasValues)
                    {
                        // Workflow resource property (property inside the Logic App workflow ARM resource type)
                        var workflowResourcePropertiesPath = "$.resources[?(@.type == 'Microsoft.Logic/workflows')]";
                        var workflowResourceProperties = (JObject)workflowDefinition.SelectToken(workflowResourcePropertiesPath);
                        if (workflowResourceProperties != null)
                        {
                            if (snippet.ContainsKey("workflowResourceProperty"))
                            {
                                var property = (JProperty)snippet["workflowResourceProperty"].First();
                                workflowResourceProperties.Add(property.Name, property.Value);

                                _logger.LogDebug(TraceMessages.AddedWorkflowPropertyToResource, RuleName, property.Name);
                            }
                        }
                        else
                        {
                            _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, workflowResourcePropertiesPath);
                            Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, workflowResourcePropertiesPath)));
                        }

                        // Workflow property (property inside the Logic App workflow resource properties object)
                        var workflowPropertiesPath = "$.resources[?(@.type == 'Microsoft.Logic/workflows')].properties";
                        var workflowProperties = (JObject)workflowDefinition.SelectToken(workflowPropertiesPath);
                        if (workflowProperties != null)
                        {
                            if (snippet.ContainsKey("workflowProperty"))
                            {
                                var property = (JProperty)snippet["workflowProperty"].First();
                                workflowProperties.Add(property.Name, property.Value);

                                _logger.LogDebug(TraceMessages.AddedWorkflowPropertyToResourceProperties, RuleName, property.Name);
                            }
                        }
                        else
                        {
                            _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, workflowPropertiesPath);
                            Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, workflowPropertiesPath)));
                        }
                    }
                }
            }
            else
            {
                _logger.LogDebug(TraceMessages.WorkflowPropertySnippetsNotFound, RuleName, processManager.Name);
            }
        }

        /// <summary>
        /// Adds workflow triggers.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition in which to put new messages.</param>
        /// <param name="parentContainer">The parent container in which to put new activity.</param>
        /// <param name="parentActivityContainer">The workflow activity container that is the parent of the activity.</param>
        /// <param name="activity">The activity to convert.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        private async Task AddTriggers(ProcessManager processManager, JObject workflowDefinition, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            // Find all channels that are activating channels (to trigger)
            var channels = processManager.WorkflowModel?.Channels?.Where(c => c.Activator);
            if (channels != null && channels.Any())
            {
                foreach (var channel in channels)
                {
                    var channelSnippets = processManager.Snippets.Where(s => s.ResourceType.StartsWith($"{ModelConstants.ResourceTypeWorkflowChannel}.", StringComparison.CurrentCultureIgnoreCase)).ToList();
                    if (channelSnippets != null && channelSnippets.Any())
                    {
                        // For triggers, messages are received into a new activate instance, whether that is via
                        // a topic channel (message subscription) or a trigger channel (invoked workflow).
                        var triggerSnippets = channelSnippets.Where(s => s.ResourceType.StartsWith($"{ModelConstants.ResourceTypeWorkflowChannelTrigger}.", StringComparison.CurrentCultureIgnoreCase));
                        if (triggerSnippets != null && triggerSnippets.Any())
                        {
                            foreach (var triggerSnippet in triggerSnippets)
                            {
                                var snippet = await LoadSnippet(processManager, processManager.WorkflowModel, resourceTemplate, triggerSnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
                                if (snippet != null && snippet.HasValues)
                                {
                                    // Workflow definition parameter (parameter inside the Logic App definition)
                                    var definitionTriggersPath = "$..definition.triggers";
                                    var definitionTriggers = (JObject)workflowDefinition.SelectToken(definitionTriggersPath);
                                    if (definitionTriggers != null)
                                    {
                                        if (snippet.ContainsKey("workflowTrigger"))
                                        {
                                            var trigger = (JProperty)snippet["workflowTrigger"].First();

                                            // For a correlating receive that is related to the same port (workflow channel) as an
                                            // activating (initializing) receive, the trigger will already be added, so check first
                                            // to see if it already exists.
                                            if (!definitionTriggers.ContainsKey(trigger.Name))
                                            {
                                                definitionTriggers.Add(trigger.Name, trigger.Value);

                                                _logger.LogDebug(TraceMessages.AddedWorkflowChannelToWorkflowDefinitionTriggers, RuleName, trigger.Name);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, definitionTriggersPath);
                                        Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, definitionTriggersPath)));
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogDebug(TraceMessages.WorkflowChannelSnippetsNotFound, RuleName, processManager.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Adds statically declared and workflow model variables to the workflow definition.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        private async Task AddVariables(ProcessManager processManager, JObject workflowDefinition, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            // Find any statically declared variables defined as snippets against the process manager
            var variableSnippets = processManager.Snippets.Where(s => s.ResourceType == ModelConstants.ResourceTypeWorkflowVariable && s.ResourceType != ModelConstants.ResourceTypeWorkflowVariablePlaceHolder).ToList();
            if (variableSnippets != null && variableSnippets.Any())
            {
                foreach (var variableSnippet in variableSnippets)
                {
                    var snippet = await LoadSnippet(processManager, processManager.WorkflowModel, resourceTemplate, variableSnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
                    if (snippet != null && snippet.HasValues)
                    {
                        // Workflow definition variable (an action inside the Logic App definition)
                        var definitionVariablesPath = "$..definition.actions";
                        var definitionVariables = (JObject)workflowDefinition.SelectToken(definitionVariablesPath);
                        if (definitionVariables != null)
                        {
                            if (snippet.ContainsKey("workflowDefinitionVariable"))
                            {
                                var variable = (JProperty)snippet["workflowDefinitionVariable"].First();
                                definitionVariables.Add(variable.Name, variable.Value);

                                _logger.LogDebug(TraceMessages.AddedWorkflowVariableToWorkflowDefinition, RuleName, variable.Name);
                            }
                        }
                        else
                        {
                            _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, definitionVariablesPath);
                            Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, definitionVariablesPath)));
                        }

                        // ARM template variable (variable inside the ARM template)
                        var armTemplateVariablesPath = "$.variables";
                        var armTemplateVariables = (JObject)workflowDefinition.SelectToken(armTemplateVariablesPath);
                        if (armTemplateVariables != null)
                        {
                            if (snippet.ContainsKey("armTemplateVariable"))
                            {
                                var variable = (JProperty)snippet["armTemplateVariable"].First();
                                armTemplateVariables.Add(variable.Name, variable.Value);

                                _logger.LogDebug(TraceMessages.AddedWorkflowVariableToArmTemplate, RuleName, variable.Name);
                            }
                        }
                        else
                        {
                            _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, armTemplateVariablesPath);
                            Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, armTemplateVariablesPath)));
                        }
                    }
                }
            }
            else
            {
                _logger.LogDebug(TraceMessages.WorkflowVariableSnippetsNotFound, RuleName, processManager.Name);
            }

            // Add variables in the workflow model to the workflow definition (Logic App InitializeVariable can only exist at root scope)
            await AddWorkflowVariables(processManager, workflowDefinition, processManager.WorkflowModel, resourceTemplate, snippetPaths, generationPathRoot).ConfigureAwait(false);
        }

        /// <summary>
        /// Recurses through the workflow model adding variables to workflow definition.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition in which to put new variables.</param>
        /// <param name="activityContainer">The workflow activity container in which to find variables.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        private async Task AddWorkflowVariables(ProcessManager processManager, JObject workflowDefinition, WorkflowActivityContainer activityContainer, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            // Get top level actions object (can only put variables and messages here)
            var actionsPath = "$..definition.actions";
            var rootContainer = (JObject)workflowDefinition.SelectToken(actionsPath);
            if (rootContainer != null)
            {
                // Does the container have variables?
                if (activityContainer.Variables.Any())
                {
                    foreach (var variable in activityContainer.Variables)
                    {
                        var variableSnippet = processManager.Snippets.Where(s => s.ResourceType == ModelConstants.ResourceTypeWorkflowVariablePlaceHolder).SingleOrDefault();
                        if (variableSnippet != null)
                        {
                            var renderedVariableSnippet = await LoadSnippet(processManager, variable, resourceTemplate, variableSnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
                            if (renderedVariableSnippet != null && renderedVariableSnippet.HasValues)
                            {
                                if (renderedVariableSnippet.ContainsKey("workflowDefinitionVariable"))
                                {
                                    var variableProperty = (JProperty)renderedVariableSnippet["workflowDefinitionVariable"].First();
                                    rootContainer.Add(variableProperty.Name, variableProperty.Value);

                                    _logger.LogDebug(TraceMessages.AddedWorkflowVariableToWorkflowDefinition, RuleName, variableProperty.Name);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogDebug(TraceMessages.WorkflowVariablePlaceHolderSnippetNotFound, RuleName, ModelConstants.ResourceTypeWorkflowVariablePlaceHolder);
                        }
                    }
                }

                // Recurse
                var childContainers = activityContainer.Activities.Where(a => a is WorkflowActivityContainer).Select(a => (WorkflowActivityContainer)a);
                if (childContainers.Any())
                {
                    foreach (var childContainer in childContainers)
                    {
                        await AddWorkflowVariables(processManager, workflowDefinition, childContainer, resourceTemplate, snippetPaths, generationPathRoot).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, actionsPath);
                Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, actionsPath)));
            }
        }

        /// <summary>
        /// Adds statically declared and workflow model messages to the workflow definition.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        private async Task AddMessages(ProcessManager processManager, JObject workflowDefinition, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            // Find any statically declared messages defined as snippets against the process manager
            var messageSnippets = processManager.Snippets.Where(s => s.ResourceType == ModelConstants.ResourceTypeWorkflowMessage && s.ResourceType != ModelConstants.ResourceTypeWorkflowMessagePlaceHolder).ToList();
            if (messageSnippets != null && messageSnippets.Any())
            {
                foreach (var messageSnippet in messageSnippets)
                {
                    var snippet = await LoadSnippet(processManager, processManager.WorkflowModel, resourceTemplate, messageSnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
                    if (snippet != null && snippet.HasValues)
                    {
                        // Workflow definition message (an action inside the Logic App definition)
                        var definitionMessagesPath = "$..definition.actions";
                        var definitionMessages = (JObject)workflowDefinition.SelectToken(definitionMessagesPath);
                        if (definitionMessages != null)
                        {
                            if (snippet.ContainsKey("workflowDefinitionMessage"))
                            {
                                var message = (JProperty)snippet["workflowDefinitionMessage"].First();
                                definitionMessages.Add(message.Name, message.Value);

                                _logger.LogDebug(TraceMessages.AddedWorkflowMessageToWorkflowDefinition, RuleName, message.Name);
                            }
                        }
                        else
                        {
                            _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, definitionMessagesPath);
                            Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, definitionMessagesPath)));
                        }
                    }
                }
            }
            else
            {
                _logger.LogDebug(TraceMessages.WorkflowMessageSnippetsNotFound, RuleName, processManager.Name);
            }

            // Add messages in the workflow model to the workflow definition (Logic App InitializeVariable can only exist at root scope)
            await AddWorkflowMessages(processManager, workflowDefinition, processManager.WorkflowModel, resourceTemplate, snippetPaths, generationPathRoot).ConfigureAwait(false);
        }

        /// <summary>
        /// Recurses through the workflow model adding messages to workflow definition.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition in which to put new messages.</param>
        /// <param name="activityContainer">The workflow activity container in which to find messages.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        private async Task AddWorkflowMessages(ProcessManager processManager, JObject workflowDefinition, WorkflowActivityContainer activityContainer, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            // Get top level actions object (can only put variables and messages here)
            var actionsPath = "$..definition.actions";
            var rootContainer = (JObject)workflowDefinition.SelectToken(actionsPath);
            if (rootContainer != null)
            {
                // Does the container have messages?
                if (activityContainer.Messages.Any())
                {
                    foreach (var message in activityContainer.Messages)
                    {
                        var messageSnippet = processManager.Snippets.Where(s => s.ResourceType == ModelConstants.ResourceTypeWorkflowMessagePlaceHolder).SingleOrDefault();
                        if (messageSnippet != null)
                        {
                            var renderedMessageSnippet = await LoadSnippet(processManager, message, resourceTemplate, messageSnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
                            if (renderedMessageSnippet != null && renderedMessageSnippet.HasValues)
                            {
                                if (renderedMessageSnippet.ContainsKey("workflowDefinitionMessage"))
                                {
                                    var messageProperty = (JProperty)renderedMessageSnippet["workflowDefinitionMessage"].First();
                                    rootContainer.Add(messageProperty.Name, messageProperty.Value);

                                    _logger.LogDebug(TraceMessages.AddedWorkflowMessageToWorkflowDefinition, RuleName, messageProperty.Name);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogDebug(TraceMessages.WorkflowMessagePlaceHolderSnippetNotFound, RuleName, ModelConstants.ResourceTypeWorkflowMessagePlaceHolder);
                        }
                    }
                }

                // Recurse
                var childContainers = activityContainer.Activities.Where(a => a is WorkflowActivityContainer).Select(a => (WorkflowActivityContainer)a);
                if (childContainers.Any())
                {
                    foreach (var childContainer in childContainers)
                    {
                        await AddWorkflowMessages(processManager, workflowDefinition, childContainer, resourceTemplate, snippetPaths, generationPathRoot).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, actionsPath);
                Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, actionsPath)));
            }
        }

        /// <summary>
        /// Recurses through the workflow model adding activity containers and activities to workflow definition.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition in which to put new messages.</param>
        /// <param name="parentContainer">The parent container in which to put new container and activities.</param>
        /// <param name="activityContainer">The workflow activity container in which to find activities.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        private async Task AddWorkflowActivities(ProcessManager processManager, JObject workflowDefinition, JObject parentContainer, WorkflowActivityContainer activityContainer, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            // Can we handle this activity container or should we use default processing?
            var handler = _activityContainerConverters.ContainsKey(activityContainer.Type) ?
                _activityContainerConverters[activityContainer.Type] :
                _activityContainerConverters[DefaultConverter];

            var activityContainerHandled = await handler(processManager, workflowDefinition, parentContainer, activityContainer, resourceTemplate, snippetPaths, generationPathRoot).ConfigureAwait(false);
            if (activityContainerHandled.isConverted)
            {
                // Created container acts as the new parent for any child activities and activity containers
                var container = activityContainerHandled.container;

                // Add any pre-built actions related to the container type to the container
                var count = await AddPreBuiltActions(processManager, container, activityContainer, resourceTemplate, snippetPaths, generationPathRoot).ConfigureAwait(false);
                if (count > 0)
                {
                    _logger.LogDebug(TraceMessages.AddedPreBuiltActionsToWorkflowActivityContainer, RuleName, count, activityContainer.Name);
                }

                // Does the container have activities?
                if (activityContainer.Activities.Any())
                {
                    foreach (var activity in activityContainer.Activities)
                    {
                        // What type of activity is it?
                        if (activity is WorkflowActivityContainer)
                        {
                            // Recurse
                            await AddWorkflowActivities(processManager, workflowDefinition, container, (WorkflowActivityContainer)activity, resourceTemplate, snippetPaths, generationPathRoot).ConfigureAwait(false);
                        }
                        else
                        {
                            // Can we handle this activity or should we use default processing?
                            var activityHandler = _activityConverters.ContainsKey(activity.Type) ?
                                _activityConverters[activity.Type] :
                                _activityConverters[DefaultConverter];

                            await activityHandler(processManager, workflowDefinition, container, activityContainer, activity, resourceTemplate, snippetPaths, generationPathRoot).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds pre-built actions to the parent container for the activity container.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="parentContainer">The parent container in which to put new container and activities.</param>
        /// <param name="activityContainer">The workflow activity container in which to find activities.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        /// <returns>The count of pre-built actions added to the container.</returns>
        private async Task<int> AddPreBuiltActions(ProcessManager processManager, JObject parentContainer, WorkflowActivityContainer activityContainer, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            var count = 0;

            // Build key to find snippet based on the activity container type
            var containerSnippetResourceType = $"{ModelConstants.ResourceTypeWorkflowActivityContainer}.{activityContainer.Type.ToLower(CultureInfo.CurrentCulture)}";
            var containerSnippets = processManager.Snippets.Where(s => s.ResourceType.StartsWith($"{containerSnippetResourceType}.", StringComparison.CurrentCultureIgnoreCase) && s.ResourceType != containerSnippetResourceType);
            if (containerSnippets != null && containerSnippets.Any())
            {
                foreach (var containerSnippet in containerSnippets)
                {
                    var renderedContainerSnippet = await LoadSnippet(processManager, activityContainer, resourceTemplate, containerSnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
                    if (renderedContainerSnippet != null && renderedContainerSnippet.HasValues)
                    {
                        // Workflow definition action (an action inside the Logic App definition)
                        if (renderedContainerSnippet.ContainsKey("workflowDefinitionAction"))
                        {
                            // Add container to parent
                            var action = (JProperty)renderedContainerSnippet["workflowDefinitionAction"].First();
                            parentContainer.Add(action.Name, action.Value);
                            count++;

                            _logger.LogTrace(TraceMessages.AddedPreBuiltActionToWorkflowActivityContainer, RuleName, action.Name, containerSnippet.ResourceType, activityContainer.Name);
                        }
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Converts an activity container into a Logic App action.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition in which to put new messages.</param>
        /// <param name="parentContainer">The parent container in which to put new container.</param>
        /// <param name="activityContainer">The activity container to convert.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        /// <returns>True if the activity container was converted with the created container, False if not with null.</returns>
        private delegate Task<(bool isConverted, JObject container)> ConvertActivityContainerHandler(ProcessManager processManager, JObject workflowDefinition, JObject parentContainer, WorkflowActivityContainer activityContainer, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot);

        /// <summary>
        /// Converts an activity into a Logic App action or trigger.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition in which to put new messages.</param>
        /// <param name="parentContainer">The parent container in which to put new activity.</param>
        /// <param name="parentActivityContainer">The workflow activity container that is the parent of the activity.</param>
        /// <param name="activity">The activity to convert.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        /// <returns>True if the activity was converted, False if not.</returns>
        private delegate Task<bool> ConvertActivityHandler(ProcessManager processManager, JObject workflowDefinition, JObject parentContainer, WorkflowActivityContainer parentActivityContainer, WorkflowActivity activity, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot);

        /// <summary>
        /// Converts an activity container.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition in which to put new messages.</param>
        /// <param name="parentContainer">The parent container in which to put new activity.</param>
        /// <param name="activityContainer">The activity container to convert.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        /// <returns>True if the activity container was converted with the created container, False if not with null.</returns>
        private async Task<(bool isConverted, JObject container)> ConvertActivityContainer(ProcessManager processManager, JObject workflowDefinition, JObject parentContainer, WorkflowActivityContainer activityContainer, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            _logger.LogTrace(TraceMessages.ConvertingActivityContainer, RuleName, activityContainer.Name, activityContainer.Type);

            // Get container actions
            var (actions, actionPath) = await FindActivityContainerActions(processManager, activityContainer, resourceTemplate, snippetPaths, generationPathRoot).ConfigureAwait(false);
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    // Add actions to parent
                    parentContainer.Add(action.Name, action.Value);

                    _logger.LogDebug(TraceMessages.AddedActionForWorkflowActivityContainerToWorkflowDefinition, RuleName, action.Name, activityContainer.Name);
                }

                // Default action path
                var containerPath = actionPath ?? $"$.['{actions.First().Name}'].actions";

                // Get actions object for the new container to act as the new parent for child activities
                var container = (JObject)parentContainer.SelectToken(containerPath);
                if (container != null)
                {
                    return (true, container);
                }
                else
                {
                    _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, containerPath);
                    Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, containerPath)));
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Converts a decision branch activity container.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition in which to put new messages.</param>
        /// <param name="parentContainer">The parent container in which to put new activity.</param>
        /// <param name="activityContainer">The activity container to convert.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        /// <returns>True if the activity container was converted with the created container, False if not with null.</returns>
        private async Task<(bool isConverted, JObject container)> ConvertDecisionBranchActivityContainer(ProcessManager processManager, JObject workflowDefinition, JObject parentContainer, WorkflowActivityContainer activityContainer, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            _logger.LogTrace(TraceMessages.ConvertingActivityContainer, RuleName, activityContainer.Name, activityContainer.Type);

            // Get container action
            var (actions, actionPath) = await FindActivityContainerActions(processManager, activityContainer, resourceTemplate, snippetPaths, generationPathRoot).ConfigureAwait(false);
            if (actions != null)
            {
                string containerPath = null;
                JObject container = null;

                // Get switch action in the parent (this assumes the snippet uses a switch for a decision shape)
                var decision = (from p in parentContainer.Properties()
                                where p.Value is JObject a && a.ContainsKey("type") && a["type"].Value<string>() == "Switch"
                                select p).SingleOrDefault();

                if (decision != null && decision.Value is JObject decisionObject)
                {
                    // Only add the case if it's not the Else branch (which is defined as the default node in the Switch action)
                    if (activityContainer.Name != WorkflowModelConstants.PropertyValueElse)
                    {
                        // Get cases object in parent to add this decision branch case
                        var casesContainerPath = $"$.cases";
                        var casesContainer = (JObject)decisionObject.SelectToken(casesContainerPath);
                        if (casesContainer != null)
                        {
                            // Add container to parent
                            casesContainer.Add(actions.First().Name, actions.First().Value);

                            // Get actions object for the new container to act as the new parent for child activities
                            containerPath = actionPath ?? $"$.['{actions.First().Name}'].actions";
                            container = (JObject)casesContainer.SelectToken(containerPath);

                            _logger.LogDebug(TraceMessages.AddedActionForWorkflowActivityContainerToWorkflowDefinition, RuleName, actions.First().Name, activityContainer.Name);
                        }
                        else
                        {
                            _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, casesContainerPath);
                            Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, casesContainerPath)));
                        }
                    }
                    else
                    {
                        // Get actions object for the existing default container to act as the new parent for child activities
                        containerPath = $"$.default.actions";
                        container = (JObject)decisionObject.SelectToken(containerPath);
                    }
                }

                if (container != null)
                {
                    return (true, container);
                }
                else
                {
                    _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, containerPath);
                    Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, containerPath)));
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Converts an activity.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition in which to put new messages.</param>
        /// <param name="parentContainer">The parent container in which to put new activity.</param>
        /// <param name="parentActivityContainer">The workflow activity container that is the parent of the activity.</param>
        /// <param name="activity">The activity to convert.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        /// <returns>True if the activity was converted, False if not.</returns>
        private async Task<bool> ConvertActivity(ProcessManager processManager, JObject workflowDefinition, JObject parentContainer, WorkflowActivityContainer parentActivityContainer, WorkflowActivity activity, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            _logger.LogTrace(TraceMessages.ConvertingActivity, RuleName, activity.Name, activity.Type);

            // Get activity actions
            var actions = await FindActivityAction(processManager, activity, resourceTemplate, snippetPaths, generationPathRoot).ConfigureAwait(false);
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    // Add actions to parent
                    parentContainer.Add(action.Name, action.Value);

                    _logger.LogDebug(TraceMessages.AddedActionForWorkflowActivityToWorkflowDefinition, RuleName, action.Name, activity.Name);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts a receive activity.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition in which to put new messages.</param>
        /// <param name="parentContainer">The parent container in which to put new activity.</param>
        /// <param name="parentActivityContainer">The workflow activity container that is the parent of the activity.</param>
        /// <param name="activity">The activity to convert.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        /// <returns>True if the activity was converted, False if not.</returns>
        private async Task<bool> ConvertReceiveActivity(ProcessManager processManager, JObject workflowDefinition, JObject parentContainer, WorkflowActivityContainer parentActivityContainer, WorkflowActivity activity, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            _logger.LogTrace(TraceMessages.ConvertingActivity, RuleName, activity.Name, activity.Type);

            var converted = false;

            var actionSnippets = processManager.Snippets.Where(s => s.ResourceType.StartsWith($"{ModelConstants.ResourceTypeWorkflowChannelReceive}.", StringComparison.CurrentCultureIgnoreCase));
            if (actionSnippets != null && actionSnippets.Any())
            {
                foreach (var actionSnippet in actionSnippets)
                {
                    var snippet = await LoadSnippet(processManager, activity, resourceTemplate, actionSnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
                    if (snippet != null && snippet.HasValues)
                    {
                        if (snippet.ContainsKey("workflowDefinitionAction"))
                        {
                            var action = (JProperty)snippet["workflowDefinitionAction"].First();

                            // Add activity to parent
                            parentContainer.Add(action.Name, action.Value);

                            _logger.LogDebug(TraceMessages.AddedActionForWorkflowActivityToWorkflowDefinition, RuleName, action.Name, activity.Name);

                            converted = true;
                        }
                    }
                }
            }

            return converted;
        }

        /// <summary>
        /// Converts a send activity.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow definition in which to put new messages.</param>
        /// <param name="parentContainer">The parent container in which to put new activity.</param>
        /// <param name="parentActivityContainer">The workflow activity container that is the parent of the activity.</param>
        /// <param name="activity">The activity to convert.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        /// <returns>True if the activity was converted, False if not.</returns>
        private async Task<bool> ConvertSendActivity(ProcessManager processManager, JObject workflowDefinition, JObject parentContainer, WorkflowActivityContainer parentActivityContainer, WorkflowActivity activity, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            _logger.LogTrace(TraceMessages.ConvertingActivity, RuleName, activity.Name, activity.Type);

            var converted = false;

            var actionSnippets = processManager.Snippets.Where(s => s.ResourceType.StartsWith($"{ModelConstants.ResourceTypeWorkflowChannelSend}.", StringComparison.CurrentCultureIgnoreCase));
            if (actionSnippets != null && actionSnippets.Any())
            {
                foreach (var actionSnippet in actionSnippets)
                {
                    var snippet = await LoadSnippet(processManager, activity, resourceTemplate, actionSnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
                    if (snippet != null && snippet.HasValues)
                    {
                        if (snippet.ContainsKey("workflowDefinitionAction"))
                        {
                            var action = (JProperty)snippet["workflowDefinitionAction"].First();

                            // Add activity to parent
                            parentContainer.Add(action.Name, action.Value);

                            _logger.LogDebug(TraceMessages.AddedActionForWorkflowActivityToWorkflowDefinition, RuleName, action.Name, activity.Name);

                            converted = true;
                        }
                    }
                }
            }

            return converted;
        }

        /// <summary>
        /// Gets a rendered action(s) for an activity container.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="activityContainer">The activity container.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        /// <returns>
        /// An array of JSON properties representing the rendered activity container, or null
        /// if one doesn't exist and the path to the actions container where child actions will be added.
        /// </returns>
        private async Task<(IEnumerable<JProperty> actions, string actionPath)> FindActivityContainerActions(ProcessManager processManager, WorkflowActivityContainer activityContainer, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            IEnumerable<JProperty> actions = null;
            string actionPath = null;

            // Build key to find snippet based on the activity container type
            var containerSnippetResourceType = $"{ModelConstants.ResourceTypeWorkflowActivityContainer}.{activityContainer.Type.ToLower(CultureInfo.CurrentCulture)}";
            var containerSnippet = processManager.Snippets.Where(s => s.ResourceType == containerSnippetResourceType).SingleOrDefault();

            // If not found, try getting the placeholder container
            if (containerSnippet == null)
            {
                _logger.LogTrace(TraceMessages.WorkflowActivityContainerSnippetNotFound, RuleName, containerSnippetResourceType);

                containerSnippet = processManager.Snippets.Where(s => s.ResourceType == ModelConstants.ResourceTypeWorkflowActivityContainerPlaceHolder).SingleOrDefault();
            }

            if (containerSnippet != null)
            {
                var renderedContainerSnippet = await LoadSnippet(processManager, activityContainer, resourceTemplate, containerSnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
                if (renderedContainerSnippet != null && renderedContainerSnippet.HasValues)
                {
                    // Workflow definition action (an action inside the Logic App definition)
                    if (renderedContainerSnippet.ContainsKey("workflowDefinitionAction"))
                    {
                        actions = ((JObject)renderedContainerSnippet["workflowDefinitionAction"]).Properties();
                    }

                    // Workflow definition action path
                    if (renderedContainerSnippet.ContainsKey("workflowDefinitionActionPath"))
                    {
                        actionPath = renderedContainerSnippet["workflowDefinitionActionPath"].Value<string>();
                    }
                }
            }
            else
            {
                _logger.LogDebug(TraceMessages.WorkflowActivityContainerPlaceHolderSnippetNotFound, RuleName, ModelConstants.ResourceTypeWorkflowActivityContainerPlaceHolder);
            }

            return (actions, actionPath);
        }

        /// <summary>
        /// Gets a rendered action for an activity.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="activity">The activity.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        /// <returns>An array of JSON properties representing the rendered activity, or null if one doesn't exist.</returns>
        private async Task<IEnumerable<JProperty>> FindActivityAction(ProcessManager processManager, WorkflowActivity activity, TargetResourceTemplate resourceTemplate, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            IEnumerable<JProperty> actions = null;

            // Build key to find snippet based on the activity type
            var activitySnippetResourceType = $"{ModelConstants.ResourceTypeWorkflowActivity}.{activity.Type.ToLower(CultureInfo.CurrentCulture)}";
            var activitySnippet = processManager.Snippets.Where(s => s.ResourceType == activitySnippetResourceType).SingleOrDefault();

            // If not found, try getting the placeholder activity
            if (activitySnippet == null)
            {
                _logger.LogTrace(TraceMessages.WorkflowActivitySnippetNotFound, RuleName, activitySnippetResourceType);

                activitySnippet = processManager.Snippets.Where(s => s.ResourceType == ModelConstants.ResourceTypeWorkflowActivityPlaceHolder).SingleOrDefault();
            }

            if (activitySnippet != null)
            {
                var renderedActivitySnippet = await LoadSnippet(processManager, activity, resourceTemplate, activitySnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
                if (renderedActivitySnippet != null && renderedActivitySnippet.HasValues)
                {
                    if (renderedActivitySnippet.ContainsKey("workflowDefinitionAction"))
                    {
                        actions = ((JObject)renderedActivitySnippet["workflowDefinitionAction"]).Properties();
                    }
                }
            }
            else
            {
                _logger.LogDebug(TraceMessages.WorkflowActivityPlaceHolderSnippetNotFound, RuleName, ModelConstants.ResourceTypeWorkflowActivityPlaceHolder);
            }

            return actions;
        }

        /// <summary>
        /// Binds actions to each other so one runs after the other.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="parentContainer">The actions container in which to bind.</param>
        private void BindActions(ProcessManager processManager, JObject parentContainer)
        {
            _logger.LogDebug(TraceMessages.BindingActions, RuleName, processManager.Name);

            // Set the runAfter for each action
            var properties = parentContainer.Properties().ToList();
            for (var i = 1; i < properties.Count; i++)
            {
                _logger.LogTrace(TraceMessages.BindingAction, RuleName, properties[i].Name, properties[i - 1].Name);

                var runAfter = (JObject)((JObject)properties[i].Value)["runAfter"];
                if (runAfter != null && !runAfter.ContainsKey(properties[i - 1].Name))
                {
                    runAfter.Add(properties[i - 1].Name, new JArray(new object[] { "Succeeded" }));
                }
            }

            // Recurse into properties that themselves have actions
            foreach (var property in properties)
            {
                // TODO: Needs to handle different paths to the actions 'container', for example, a
                // switch has actions in each case switch and the default switch.
                var action = (JObject)property.Value;
                if (action.ContainsKey("actions"))
                {
                    var actionsPath = "$.actions";
                    var container = (JObject)action.SelectToken(actionsPath);
                    if (container != null)
                    {
                        BindActions(processManager, container);
                    }
                    else
                    {
                        _logger.LogError(ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, actionsPath);
                        Context.Errors.Add(new ErrorMessage(string.Format(CultureInfo.CurrentCulture, ErrorMessages.UnableToFindNodeInWorkflowDefinition, processManager.Name, actionsPath)));
                    }
                }
            }
        }

        /// <summary>
        /// Loads a snippet.
        /// </summary>
        /// <param name="processManager">The process manager associated with the resource snippet.</param>
        /// <param name="workflowObject">The workflow object associated with the snippet.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="resourceSnippet">The resource snippet for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        /// <returns>A <see cref="JObject"/> representing the snippet.</returns>
        private async Task<JObject> LoadSnippet(ProcessManager processManager, WorkflowObject workflowObject, TargetResourceTemplate resourceTemplate, TargetResourceSnippet resourceSnippet, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            // Increment unique index and assign to workflow object
            var index = Interlocked.Increment(ref _index);
            if (workflowObject.Properties.ContainsKey(WorkflowModelConstants.PropertyUniqueId))
            {
                workflowObject.Properties[WorkflowModelConstants.PropertyUniqueId] = index;
            }
            else
            {
                workflowObject.Properties.Add(WorkflowModelConstants.PropertyUniqueId, index);
            }

            var snippet = await GenerateFileAsync(processManager, workflowObject, resourceTemplate, resourceSnippet, snippetPaths, generationPathRoot).ConfigureAwait(false);
            if (snippet != null)
            {
                // Load workflow definition as a JSON object
                using (var reader = new StringReader(snippet))
                {
                    using (var jsonReader = new JsonTextReader(reader))
                    {
                        return (JObject)JToken.ReadFrom(jsonReader);
                    }
                }
            }
            else
            {
                _logger.LogWarning(WarningMessages.SnippetFileNotFound, resourceSnippet.ResourceSnippetFile);
            }

            return null;
        }

        /// <summary>
        /// Generates a template file by loading, rendering and saving the file to the conversion path
        /// if it is a .liquid file, otherwise just copy the file.
        /// </summary>
        /// <param name="processManager">The process manager associated with the resource snippet.</param>
        /// <param name="workflowObject">The workflow object associated with the snippet.</param>
        /// <param name="resourceTemplate">The resource template for this render.</param>
        /// <param name="resourceSnippet">The resource snippet for this render.</param>
        /// <param name="snippetPaths">The list of source template paths.</param>
        /// <param name="generationPathRoot">The root path for all generated files.</param>
        /// <returns>Returns a snippet or null if not found.</returns>
        private async Task<string> GenerateFileAsync(ProcessManager processManager, WorkflowObject workflowObject, TargetResourceTemplate resourceTemplate, TargetResourceSnippet resourceSnippet, IEnumerable<DirectoryInfo> snippetPaths, DirectoryInfo generationPathRoot)
        {
            string snippetContent = null;

            foreach (var snippetPath in snippetPaths)
            {
                var snippetFilePath = new FileInfo(Path.Combine(snippetPath.FullName, resourceSnippet.ResourceSnippetFile));
                if (_fileRepository.DoesFileExist(snippetFilePath.FullName))
                {
                    _logger.LogTrace(TraceMessages.LoadingSnippet, RuleName, snippetFilePath.FullName);

                    var workflowObjectName = $"{workflowObject.Properties[WorkflowModelConstants.PropertyUniqueId]}.{workflowObject.Name}".ToSafeFilePath();
                    
                    // Load snippet
                    snippetContent = await _repository.LoadTemplateAsync(snippetFilePath.FullName).ConfigureAwait(false);

                    // Check extension
                    if (snippetFilePath.Extension.ToUpperInvariant() == ".liquid".ToUpperInvariant())
                    {
                        _logger.LogTrace(TraceMessages.RenderingSnippet, RuleName, snippetFilePath.FullName, workflowObject.Name, workflowObject.Type);

                        // Render snippet
                        snippetContent = await _snippetRenderer.RenderSnippetAsync(snippetContent, Model, processManager, resourceTemplate, resourceSnippet, workflowObject).ConfigureAwait(false);

                        // Only output if there is an output path (useful for debugging)
                        if (!string.IsNullOrWhiteSpace(resourceSnippet.OutputPath))
                        {
                            // Set output file path
                            var outputFilePath = new FileInfo(Path.Combine(generationPathRoot.FullName, resourceSnippet.OutputPath, string.Concat(workflowObjectName, ".", Path.GetFileNameWithoutExtension(snippetFilePath.Name))));

                            _logger.LogTrace(TraceMessages.SavingSnippet, RuleName, outputFilePath.FullName);

                            // Save rendered or original (if not a liquid file) snippet
                            await _repository.SaveTemplateAsync(outputFilePath.FullName, snippetContent).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // Only output if there is an output path (useful for debugging)
                        if (!string.IsNullOrWhiteSpace(resourceSnippet.OutputPath))
                        {
                            // Set output file path
                            var outputFilePath = new FileInfo(Path.Combine(generationPathRoot.FullName, resourceSnippet.OutputPath, string.Concat(workflowObjectName, ".", snippetFilePath.Name)));

                            _logger.LogTrace(TraceMessages.CopyingSnippet, RuleName, snippetFilePath.FullName, outputFilePath.FullName);

                            // Create output path if some directories don't exist
                            if (!_fileRepository.DoesDirectoryExist(outputFilePath.FullName))
                            {
                                _fileRepository.CreateDirectory(outputFilePath.DirectoryName);
                            }

                            // Just a normal file, copy it to output path
                            _fileRepository.CopyFile(snippetFilePath.FullName, outputFilePath.FullName);
                        }
                    }
                }
            }

            return snippetContent;
        }

        /// <summary>
        /// Saves the workflow to the specified path.
        /// </summary>
        /// <param name="processManager">The process manager.</param>
        /// <param name="workflowDefinition">The workflow.</param>
        /// <param name="generationPathRoot">The root of the generation path for output.</param>
        /// <param name="filePath">The file path for the saved workflow.</param>
        private void SaveWorkflow(ProcessManager processManager, JObject workflowDefinition, DirectoryInfo generationPathRoot, string filePath)
        {
            // Build path
            var outputFilePath = new FileInfo(Path.Combine(generationPathRoot.FullName, filePath));

            // Create directory, if it doesn't exist
            if (!_fileRepository.DoesDirectoryExist(Path.GetDirectoryName(outputFilePath.FullName)))
            {
                _fileRepository.CreateDirectory(Path.GetDirectoryName(outputFilePath.FullName));
            }

            // Save workflow
            _fileRepository.WriteJsonFile(outputFilePath.FullName, workflowDefinition);

            _logger.LogDebug(TraceMessages.SavedWorkflow, RuleName, outputFilePath.FullName, processManager.Name);
        }
    }
}
