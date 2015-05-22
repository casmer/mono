//------------------------------------------------------------------------------
// <copyright file="ModulesEntry.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

/*
 * Config related classes for HttpApplication
 * 
 */

namespace System.Web.Configuration.Common {
	using System.Diagnostics;
    using System.Runtime.Serialization.Formatters;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.Collections;
    using System.Reflection;
    using System.Globalization;
    using System.Configuration;
    using System.Web;
    using System.Web.SessionState;
    using System.Web.Security;
    using System.Web.Util;
    using System.Web.Compilation;
	using System.Xml;
    using System.Security;
    using System.Security.Permissions;

    /*
     * Single Entry of request to class
     */
    internal class ModulesEntry {
        private String _name;
        private Type _type;

        internal ModulesEntry(String name, String typeName, string propertyName, ConfigurationElement configElement) {
            _name = (name != null) ? name : String.Empty;

            // Don't check the APTCA bit for modules (VSWhidbey 467768, 550122)
            _type = SecureGetType(typeName, propertyName, configElement);
            if (!typeof(IHttpModule).IsAssignableFrom(_type)) {
                if (configElement == null) {
					throw new ConfigurationErrorsException(SR.GetString("Type {0} is not a Module.", typeName)); 
                }
                else {
					throw new ConfigurationErrorsException(SR.GetString("Type {0} is not a Module.", typeName),
                              configElement.ElementInformation.Properties["type"].Source, configElement.ElementInformation.Properties["type"].LineNumber);
                }
            }
        }

        internal static bool IsTypeMatch(Type type, String typeName) {
            return(type.Name.Equals(typeName) || type.FullName.Equals(typeName));
        }

        internal String ModuleName {
            get { return _name; }
        }

        internal /*public*/ IHttpModule Create() {
            return (IHttpModule)HttpRuntime.CreateNonPublicInstance(_type);
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted=true)]
        private Type SecureGetType(string typeName, string propertyName, ConfigurationElement configElement) {
			return GetType(typeName, propertyName, configElement,null, false /*checkAptcaBit*/, false);
        }

		internal static Type GetType(string typeName, string propertyName, ConfigurationElement configElement,
			XmlNode node, bool checkAptcaBit, bool ignoreCase) {

			// We should get either a propertyName/configElement or node, but not both.
			// They are used only for error reporting.
			Debug.Assert((propertyName != null) != (node != null));

			Type val;
			try {
				val = BuildManager.GetType(typeName, true /*throwOnError*/, ignoreCase);
			}
			catch (Exception e) {
				if (e is ThreadAbortException || e is StackOverflowException || e is OutOfMemoryException) {
					throw;
				}

				if (node != null) {
					throw new ConfigurationErrorsException(e.Message, e, node);
				}
				else {
					if (configElement != null) {
						throw new ConfigurationErrorsException(e.Message, e,
							configElement.ElementInformation.Properties[propertyName].Source,
							configElement.ElementInformation.Properties[propertyName].LineNumber);
					}
					else {
						throw new ConfigurationErrorsException(e.Message, e);
					}
				}
			}

			// If we're not in full trust, only allow types that have the APTCA bit (ASURT 139687),
			// unless the checkAptcaBit flag is false
//			if (checkAptcaBit) {
//				if (node != null) {
//					HttpRuntime.FailIfNoAPTCABit(val, node);
//				}
//				else {
//					HttpRuntime.FailIfNoAPTCABit(val,
//						configElement != null ? configElement.ElementInformation : null,
//						propertyName);
//				}
//			}
			return val;
		}
    }
}
