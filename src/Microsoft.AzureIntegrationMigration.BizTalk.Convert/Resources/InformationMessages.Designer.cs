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

namespace Microsoft.AzureIntegrationMigration.BizTalk.Convert.Resources {
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
    internal class InformationMessages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal InformationMessages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.AzureIntegrationMigration.BizTalk.Convert.Resources.InformationMessages" +
                            "", typeof(InformationMessages).Assembly);
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
        ///   Looks up a localized string similar to There were {ResourceTemplateFileCount} resource template files generated from the target model..
        /// </summary>
        internal static string GeneratedResourceTemplateFiles {
            get {
                return ResourceManager.GetString("GeneratedResourceTemplateFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There are no target resource templates in the model to convert..
        /// </summary>
        internal static string NoResourceTemplatesToConvert {
            get {
                return ResourceManager.GetString("NoResourceTemplatesToConvert", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There are no template paths provided to the converter..
        /// </summary>
        internal static string NoTemplatePathsFound {
            get {
                return ResourceManager.GetString("NoTemplatePathsFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Running BizTalk converter {ConverterName}..
        /// </summary>
        internal static string RunningBizTalkConverter {
            get {
                return ResourceManager.GetString("RunningBizTalkConverter", resourceCulture);
            }
        }
    }
}
