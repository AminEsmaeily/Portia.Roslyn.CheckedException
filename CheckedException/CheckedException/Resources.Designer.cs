﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CheckedException {
    using System;
    using System.Reflection;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CheckedException.Resources", typeof(Resources).GetTypeInfo().Assembly);
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
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string DuplicateAttributeAnalyzerDescription {
            get {
                return ResourceManager.GetString("DuplicateAttributeAnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Another attribute with the {0} exception type has been declared for this method. Please remove one of them..
        /// </summary>
        internal static string DuplicateAttributeAnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("DuplicateAttributeAnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Another attribute with the same exception type has been declared..
        /// </summary>
        internal static string DuplicateAttributeAnalyzerTitle {
            get {
                return ResourceManager.GetString("DuplicateAttributeAnalyzerTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Fix using Attribute.
        /// </summary>
        internal static string FixByAnnotation {
            get {
                return ResourceManager.GetString("FixByAnnotation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Fix using TryCatch.
        /// </summary>
        internal static string FixByTryCatch {
            get {
                return ResourceManager.GetString("FixByTryCatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This method might have thrown an exception, but it isn&apos;t handled in code. Please use either TryCatch to handle or rethrow it using ThrowsExceptionAttribute to announce your methods&apos; caller about this exception..
        /// </summary>
        internal static string NotHandledAnalyzerDescription {
            get {
                return ResourceManager.GetString("NotHandledAnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The exception of type {0} is not handled in code..
        /// </summary>
        internal static string NotHandledAnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("NotHandledAnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The exception is not handled in code.
        /// </summary>
        internal static string NotHandledAnalyzerTitle {
            get {
                return ResourceManager.GetString("NotHandledAnalyzerTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The container class of this method has a ThrowException attribute of this exception type, then this declaration is redundant here..
        /// </summary>
        internal static string RedundantAttributeAnalyzerDescription {
            get {
                return ResourceManager.GetString("RedundantAttributeAnalyzerDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The exception of type {0} has been declared for the class. This declaration is redundant here..
        /// </summary>
        internal static string RedundantAttributeAnalyzerMessageFormat {
            get {
                return ResourceManager.GetString("RedundantAttributeAnalyzerMessageFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The declared attribute is redundant.
        /// </summary>
        internal static string RedundantAttributeAnalyzerTitle {
            get {
                return ResourceManager.GetString("RedundantAttributeAnalyzerTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Remove duplicate attribute.
        /// </summary>
        internal static string RemoveDuplicateAttribute {
            get {
                return ResourceManager.GetString("RemoveDuplicateAttribute", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Remove redundant attribute.
        /// </summary>
        internal static string RemoveRedundantAttribute {
            get {
                return ResourceManager.GetString("RemoveRedundantAttribute", resourceCulture);
            }
        }
    }
}
