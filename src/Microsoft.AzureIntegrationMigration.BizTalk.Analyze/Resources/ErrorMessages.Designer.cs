// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.AzureIntegrationMigration.BizTalk.Analyze.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class ErrorMessages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ErrorMessages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.AzureIntegrationMigration.BizTalk.Analyze.Resources.ErrorMessages", typeof(ErrorMessages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The correlation type &apos;{0}&apos; must have properties to be valid, but the metamodel definition doesn&apos;t contain any..
        /// </summary>
        internal static string CorrelationTypeMustHaveProperties {
            get {
                return ResourceManager.GetString("CorrelationTypeMustHaveProperties", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The map resource with the key &apos;{0}&apos; has multiple source schemas.  Multi-source schemas are not supported for the message translator intermediary..
        /// </summary>
        internal static string MapHasTooManySourceSchemas {
            get {
                return ResourceManager.GetString("MapHasTooManySourceSchemas", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The multipart message type &apos;{0}&apos; must have parts to be valid, but the metamodel definition doesn&apos;t contain any..
        /// </summary>
        internal static string MultipartMessageTypeMustHaveParts {
            get {
                return ResourceManager.GetString("MultipartMessageTypeMustHaveParts", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The ApplicationDefinition was not set for &apos;{0}&apos;.
        /// </summary>
        internal static string NoApplicationDefinition {
            get {
                return ResourceManager.GetString("NoApplicationDefinition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Pipeline &apos;{0}&apos; has no stages which is unexpected..
        /// </summary>
        internal static string PipelineHasNoStages {
            get {
                return ResourceManager.GetString("PipelineHasNoStages", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The port type &apos;{0}&apos; must have operations to be valid, but the metamodel definition doesn&apos;t contain any..
        /// </summary>
        internal static string PortTypeMustHaveOperations {
            get {
                return ResourceManager.GetString("PortTypeMustHaveOperations", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The port type &apos;{0}&apos; and operation &apos;{1}&apos; must have a message that matches &apos;{2}&apos; from the activity &apos;{3}&apos;, but the metamodel definition doesn&apos;t contain a matching message..
        /// </summary>
        internal static string PortTypeOperationMustHaveMatchingMessage {
            get {
                return ResourceManager.GetString("PortTypeOperationMustHaveMatchingMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The port type &apos;{0}&apos; with operation &apos;{1}&apos; must have message references to be valid, but the metamodel definition doesn&apos;t contain any..
        /// </summary>
        internal static string PortTypeOperationMustHaveMessageReferences {
            get {
                return ResourceManager.GetString("PortTypeOperationMustHaveMessageReferences", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The receive pipeline does not exist in receive location &apos;{0}&apos;..
        /// </summary>
        internal static string ReceivePipelineNotSetInReceiveLocation {
            get {
                return ResourceManager.GetString("ReceivePipelineNotSetInReceiveLocation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The intermediary &apos;{0}&apos; at the start of the route is not configured as an activator intermediary..
        /// </summary>
        internal static string SendRouteStartNotAnActivator {
            get {
                return ResourceManager.GetString("SendRouteStartNotAnActivator", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The service link type &apos;{0}&apos; must have roles to be valid, but the metamodel definition doesn&apos;t contain any..
        /// </summary>
        internal static string ServiceLinkTypeMustHaveRoles {
            get {
                return ResourceManager.GetString("ServiceLinkTypeMustHaveRoles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The service link type &apos;{0}&apos; with role &apos;{1}&apos; must have a port reference to be valid, but the metamodel definition doesn&apos;t contain one..
        /// </summary>
        internal static string ServiceLinkTypeRoleMustHavePortTypeReference {
            get {
                return ResourceManager.GetString("ServiceLinkTypeRoleMustHavePortTypeReference", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The service &apos;{0}&apos; is missing in the application binding file..
        /// </summary>
        internal static string ServiceMissingInBindingFile {
            get {
                return ResourceManager.GetString("ServiceMissingInBindingFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The source object associated with resource &apos;{0}&apos; of type &apos;{1}&apos; was not found..
        /// </summary>
        internal static string SourceObjectNotFound {
            get {
                return ResourceManager.GetString("SourceObjectNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The subscription filter statement operator &apos;{0}&apos; is not supported..
        /// </summary>
        internal static string SubscriptionFilterOperatorNotSupported {
            get {
                return ResourceManager.GetString("SubscriptionFilterOperatorNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {RuleId}: Message Schema is missing for {MessageName} for {TransformType}..
        /// </summary>
        internal static string TransformMessageSchemaMissing {
            get {
                return ResourceManager.GetString("TransformMessageSchemaMissing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The transmit pipeline does not exist in send port &apos;{0}&apos;..
        /// </summary>
        internal static string TransmitPipelineNotSetInSendPort {
            get {
                return ResourceManager.GetString("TransmitPipelineNotSetInSendPort", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The resource associated with object type &apos;{0}&apos;, name &apos;{1}&apos;, key &apos;{2}&apos; could not be found..
        /// </summary>
        internal static string UnableToFindAssociatedResource {
            get {
                return ResourceManager.GetString("UnableToFindAssociatedResource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to find any related resources of type &apos;{0}&apos; with relationship &apos;{1}&apos; for the resource with key &apos;{2}&apos;..
        /// </summary>
        internal static string UnableToFindFindRelatedResourceByType {
            get {
                return ResourceManager.GetString("UnableToFindFindRelatedResourceByType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to find the messaging object of type &apos;{0}&apos; with the key &apos;{1}&apos; in the target model..
        /// </summary>
        internal static string UnableToFindMessagingObjectWithKeyInTargetModel {
            get {
                return ResourceManager.GetString("UnableToFindMessagingObjectWithKeyInTargetModel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to find the messaging object of type &apos;{0}&apos; with the name &apos;{1}&apos; in the target model..
        /// </summary>
        internal static string UnableToFindMessagingObjectWithNameInTargetModel {
            get {
                return ResourceManager.GetString("UnableToFindMessagingObjectWithNameInTargetModel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to find any related resources of type &apos;{0}&apos; with name &apos;{1}&apos; and relationship &apos;{2}&apos; for the resource with key &apos;{3}&apos;..
        /// </summary>
        internal static string UnableToFindRelatedResourceByTypeAndName {
            get {
                return ResourceManager.GetString("UnableToFindRelatedResourceByTypeAndName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to find a resource of type &apos;{0}&apos; in resource definition &apos;{1}&apos;..
        /// </summary>
        internal static string UnableToFindResource {
            get {
                return ResourceManager.GetString("UnableToFindResource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An expected resource definition of type &apos;{0}&apos; with key &apos;{1}&apos; could not be found..
        /// </summary>
        internal static string UnableToFindResourceDefinition {
            get {
                return ResourceManager.GetString("UnableToFindResourceDefinition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The resource associated with object type &apos;{0}&apos;, keyRef &apos;{1}&apos; could not be found..
        /// </summary>
        internal static string UnableToFindResourceReference {
            get {
                return ResourceManager.GetString("UnableToFindResourceReference", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The workflow &apos;{0}&apos; which is called by &apos;{1}&apos; could not be found in any target application..
        /// </summary>
        internal static string UnableToFindWorkflow {
            get {
                return ResourceManager.GetString("UnableToFindWorkflow", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The workflow channel &apos;{0}&apos; associated with activity &apos;{1}&apos; could not be found in the workflow model..
        /// </summary>
        internal static string UnableToFindWorkflowChannel {
            get {
                return ResourceManager.GetString("UnableToFindWorkflowChannel", resourceCulture);
            }
        }
    }
}
