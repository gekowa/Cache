using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PostSharp.Aspects;
using System.Collections;

namespace CacheAspect {
    [Serializable]
    public class KeyBuilder {
        public static readonly char SPLITTER = '%';

        public string MethodName { get; set; }
        public CacheSettings Settings { get; set; }
        public string GroupName { get; set; }
        public string[] SelectedParameters { get; set; }

        public ParameterInfo[] MethodParameters { get; set; }

        public string BuildCacheKey(object instance, Arguments arguments) {
            StringBuilder cacheKeyBuilder = new StringBuilder();

            // start building a key based on the method name if a group name not set
            if (string.IsNullOrWhiteSpace(GroupName)) {
                cacheKeyBuilder.Append(MethodName).Append(SPLITTER);
            } else {
                cacheKeyBuilder.Append(GroupName).Append(SPLITTER);
            }

            if (instance != null) {
                cacheKeyBuilder.Append(instance);
                cacheKeyBuilder.Append(";");
            }

            // Ignore all paramters
            if (Settings ==  CacheSettings.IgnoreParameters) {
                return cacheKeyBuilder.ToString();
            }

            // UseId
            if (Settings == CacheSettings.UseId) {
                var argIndex = GetArgumentIndexByName("id");
                if (argIndex >= 0 && argIndex < arguments.Count) {
                    cacheKeyBuilder.Append(arguments.GetArgument(argIndex) ?? "Null");
                }
                return cacheKeyBuilder.ToString();
            }

            for (int i = 0; i < MethodParameters.Length; i++) {
                var parameter = MethodParameters[i];
                var attributes = parameter.GetCustomAttributes(true);
                bool hasIgnoreAttribute = false;
                object argument = arguments.GetArgument(i);

                // check if this parameter is ignored
                foreach (var attr in attributes) {
                    if (attr is Attributes.IgnoreAttribute) {
                        hasIgnoreAttribute = true;
                        continue;
                    }

                    if (attr is Attributes.OverrideValueAttribute) {
                        argument = (attr as Attributes.OverrideValueAttribute).Value;
                        continue;
                    }

                    if (attr is Attributes.ExtractPropertyAttribute) {
                        argument = ExtractPropertyValueByAccessor(argument,
                            (attr as Attributes.ExtractPropertyAttribute).Accessor);
                    }
                }

                if (hasIgnoreAttribute) {
                    continue;
                }



                switch (Settings) {
                    case CacheSettings.UseSelectedParameters: {
                        var parameterName = parameter.Name;
                        if (SelectedParameters.Contains(parameterName)) {
                            BuildKeyWithArgument(argument, cacheKeyBuilder);
                        }
                    } break;
                    case CacheSettings.Default:
                        BuildKeyWithArgument(argument, cacheKeyBuilder);
                        break;
                }
            }

            return cacheKeyBuilder.ToString().TrimEnd(SPLITTER);
        }

        private static void BuildKeyWithArgument(object argument, StringBuilder cacheKeyBuilder) {
            if (argument != null && typeof(ICollection).IsAssignableFrom(argument.GetType())) {
                cacheKeyBuilder.Append("{");
                foreach (object o in (ICollection)argument) {
                    cacheKeyBuilder.Append(o ?? "Null").Append(SPLITTER);
                }
                cacheKeyBuilder.Append("}");
            } else {
                cacheKeyBuilder.Append(argument ?? "Null").Append(SPLITTER);
            }
        }

        //private int GetArgumentIndexByName(string paramName) {
        //    var paramKeyValue = parametersNameValueMapper.SingleOrDefault(arg => string.Compare(arg.Value, paramName, CultureInfo.InvariantCulture,
        //        CompareOptions.IgnoreCase) == 0);

        //    return paramKeyValue.Key;
        //}

        private int GetArgumentIndexByName(string parameterName) {
            for (int i = 0; i < MethodParameters.Length; i++) {
                if (parameterName == MethodParameters[i].Name) {
                    return i;
                }
            }

            return -1;
        }

        object ExtractPropertyValueByAccessor(object argument, string accessor) {
            // TODO: allow accessor to be "Property1.Property2", can up to 3 level
            return argument.GetType().GetProperty(accessor).GetValue(argument, null);
        }
    }

    public enum CacheSettings {
        Default,
        UseId,
        IgnoreParameters,
        UseSelectedParameters,
        UseAttribute,
        UseCustom,
        IgnoreTTL,
        
    };
}