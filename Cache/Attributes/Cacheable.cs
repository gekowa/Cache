using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Aspects;
using CacheAspect.Supporting;
using System.Reflection;

namespace CacheAspect.Attributes {
    [Serializable]
    public class CacheableAttribute : OnMethodBoundaryAspect {
        private KeyBuilder keyBuilder;
        public KeyBuilder KeyBuilder {
            get { 
                return keyBuilder ?? (keyBuilder = new KeyBuilder()); 
            }
        }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the Cacheable class
        /// </summary>
        public CacheableAttribute() : this(string.Empty) { }

        /// <summary>
        /// Initializes a new instance of the Cacheable class
        /// </summary>
        /// <param name="groupName">Group name for the cached objects</param>
        public CacheableAttribute(string groupName) : this(groupName, CacheSettings.Default) { }

        /// <summary>
        /// Initializes a new instance of the Cacheable class
        /// </summary>
        /// <param name="groupName">Group name for the cached objects</param>
        /// <param name="settings">An instance of CacheSettings for the settings of Cacheable</param>
        public CacheableAttribute(string groupName, CacheSettings settings) : this(groupName, settings, string.Empty) { }

        /// <summary>
        /// Initializes a new instance of the Cacheable class
        /// </summary>
        /// <param name="groupName">Group name for the cached objects</param>
        /// <param name="settings">An instance of CacheSettings for the settings of Cacheable</param>
        /// <param name="parameterProperty">The name of the property to be used to generated the key for the cached object, when CacheSettings.UseProperty is used</param>
        public CacheableAttribute(string groupName, CacheSettings settings, string parameterProperty)
            : this(groupName, settings, new string[] { parameterProperty }) { }

        /// <summary>
        /// Initializes a new instance of the Cacheable class
        /// </summary>
        /// <param name="groupName">Group name for the cached objects</param>
        /// <param name="settings">An instance of CacheSettings for the settings of Cacheable</param>
        /// <param name="parameterProperties">The names of the properties to be used to generated the key for the cached object, when CacheSettings.UseProperty is used</param>
        public CacheableAttribute(string groupName, CacheSettings settings, string[] parameterProperties) {
            KeyBuilder.GroupName = groupName;
            KeyBuilder.Settings = settings;
            KeyBuilder.SelectedParameters = parameterProperties;
        }

        #endregion

        // Method executed at build time.
        public override void CompileTimeInitialize(MethodBase method, AspectInfo aspectInfo) {
            KeyBuilder.MethodParameters = method.GetParameters();
            KeyBuilder.MethodName = string.Format("{0}.{1}", method.DeclaringType.FullName, method.Name);
        }

        // This method is executed before the execution of target methods of this aspect.
        public override void OnEntry(MethodExecutionArgs args) {
            // Compute the cache key.
            string cacheKey = KeyBuilder.BuildCacheKey(args.Instance, args.Arguments);

            // Fetch the value from the cache.
            ICache cache = CacheService.Cache;
            MethodExecWrapper cachedWrapper = (MethodExecWrapper)(cache.Contains(cacheKey) ? cache[cacheKey] : null);

            if (cachedWrapper != null && !IsTooOld(cachedWrapper.Timestamp)) {
                // The value was found in cache. Don't execute the method. Return immediately.
                args.ReturnValue = cachedWrapper.ReturnValue;
                
                for (int i = 0; i < cachedWrapper.Arguments.Length; i++) {
                    // args.Arguments.SetArgument(i, value.Arguments[i]);
                    object fromArgs = args.Arguments[i];
                    object cached = cachedWrapper.Arguments[i];

                    if (cached == null) {
                        continue;
                    }

                    if (fromArgs != null && cached != null &&
                        !object.ReferenceEquals(fromArgs.GetType(), cached.GetType())) {
                        continue;
                    }

                    if (fromArgs != null) {
                        Type commonType = fromArgs.GetType();
                        foreach (PropertyInfo pi in commonType.GetProperties()) {
                            if (pi.CanRead && pi.CanWrite) {
                                object fromValue = pi.GetValue(fromArgs, null);
                                object cachedValue = pi.GetValue(cached, null);
                                if (fromValue != cachedValue) {
                                    pi.SetValue(fromArgs, cachedValue, null);
                                }
                            }
                        }
                    }
                }

                // parameters out?
                ParameterInfo[] methodParameters = args.Method.GetParameters();
                for (int i = 0; i < methodParameters.Length; i++) {
                    ParameterInfo pi = methodParameters[i];
                    if (pi.IsOut) {
                        // copy argument
                        args.Arguments.SetArgument(i, cachedWrapper.Arguments[i]);
                    }
                }

                args.FlowBehavior = FlowBehavior.Return;
            } else {
                // The value was NOT found in cache. Continue with method execution, but store
                // the cache key so that we don't have to compute it in OnSuccess.
                args.MethodExecutionTag = cacheKey;
            }
        }

        // This method is executed upon successful completion of target methods of this aspect.
        public override void OnSuccess(MethodExecutionArgs args) {
            string cacheKey = (string)args.MethodExecutionTag;
            CacheService.Cache[cacheKey] = new MethodExecWrapper {
                ReturnValue = args.ReturnValue,
                Timestamp = DateTime.UtcNow,
                Arguments = args.Arguments.ToArray()
            };
        }

        private bool IsTooOld(DateTime time) {
            if (KeyBuilder.Settings == CacheSettings.IgnoreTTL) {
                return false;
            }
            return DateTime.UtcNow - time > CacheService.TimeToLive;
        }
    }
}
