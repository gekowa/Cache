using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using PostSharp.Aspects;
using System.Collections;

namespace CacheAspect
{
    [Serializable]
    public class KeyBuilder
    {
        public static readonly char SPLITTER = '%';

        public string MethodName { get; set; }
        public CacheSettings Settings { get; set; }
        public string GroupName { get; set; }
        public string[] ParameterProperties { get; set; }
        private Dictionary<int, string> _parametersNameValueMapper;
        private ParameterInfo[] _methodParameters;
        public ParameterInfo[] MethodParameters
        {
            get { return _methodParameters; }
            set
            {
                _methodParameters = value;
                TransformParametersIntoNameValueMapper(_methodParameters);
            }
        }

        private void TransformParametersIntoNameValueMapper(ParameterInfo[] methodParameters)
        {
            _parametersNameValueMapper = new Dictionary<int, string>();
            for (var i = 0; i < methodParameters.Count(); i++)
            {
                _parametersNameValueMapper.Add(i, methodParameters[i].Name);
            }
        }

        public string BuildCacheKey(object instance, Arguments arguments)
        {
            StringBuilder cacheKeyBuilder = new StringBuilder();

            // start building a key based on the method name if a group name not set
            if (string.IsNullOrWhiteSpace(GroupName))
            {
                cacheKeyBuilder.Append(MethodName).Append(SPLITTER);
            }
            else
            {
                cacheKeyBuilder.Append(GroupName).Append(SPLITTER);
            }

            if (instance != null)
            {
                cacheKeyBuilder.Append(instance);
                cacheKeyBuilder.Append(";");
            }

            int argIndex;
            switch (Settings)
            {
                case CacheSettings.IgnoreParameters:
                    return cacheKeyBuilder.ToString();
                case CacheSettings.UseId:
                    argIndex = GetArgumentIndexByName("Id");
                    cacheKeyBuilder.Append(arguments.GetArgument(argIndex) ?? "Null").Append(SPLITTER);
                    break;
                case CacheSettings.UseProperty:
                    //argIndex = GetArgumentIndexByName(ParameterProperty);
                    //cacheKeyBuilder.Append(arguments.GetArgument(argIndex) ?? "Null");
                    for (var i = 0; i < ParameterProperties.Length; i++)
                    {
                        argIndex = GetArgumentIndexByName(ParameterProperties[i]);
                        // cacheKeyBuilder.Append( ?? "Null");
                        BuildDefaultKey(arguments.GetArgument(argIndex), cacheKeyBuilder);
                    }
                    break;
                case CacheSettings.Default:
                    for (var i = 0; i < arguments.Count; i++)
                    {
                        BuildDefaultKey(arguments.GetArgument(i), cacheKeyBuilder);
                    }
                    break;
            }

            return cacheKeyBuilder.ToString().TrimEnd(SPLITTER);
        }

        private static void BuildDefaultKey(object argument, StringBuilder cacheKeyBuilder)
        {
            if (argument != null && typeof(ICollection).IsAssignableFrom(argument.GetType()))
            {
                cacheKeyBuilder.Append("{");
                foreach (object o in (ICollection)argument)
                {
                    cacheKeyBuilder.Append(o ?? "Null").Append(SPLITTER);
                }
                cacheKeyBuilder.Append("}");
            }
            else
            {
                cacheKeyBuilder.Append(argument ?? "Null").Append(SPLITTER);
            }
        }

        private int GetArgumentIndexByName(string paramName)
        {
            var paramKeyValue = _parametersNameValueMapper.SingleOrDefault(arg => string.Compare(arg.Value, paramName, CultureInfo.InvariantCulture,
                CompareOptions.IgnoreCase) == 0);

            return paramKeyValue.Key;
        }
    }

    public enum CacheSettings { 
        Default, 
        IgnoreParameters, 
        UseId,
        UseProperty, 
        UseCustom, 
        IgnoreTTL 
    };
}