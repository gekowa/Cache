using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using PostSharp.Aspects;

namespace CacheAspect.Attributes {
    [Serializable]
    public class TriggerInvalidationAttribute : OnMethodBoundaryAspect {
        private KeyBuilder keyBuilder;
        public KeyBuilder KeyBuilder {
            get {
                return keyBuilder ?? (keyBuilder = new KeyBuilder());
            }
        }

        private bool bySimilar = false;

        #region Constructors

        public TriggerInvalidationAttribute(string groupName, CacheSettings settings, string parameterProperty) {
            KeyBuilder.GroupName = groupName;
            KeyBuilder.Settings = settings;
            KeyBuilder.SelectedParameters = new string[] { parameterProperty };
        }

        public TriggerInvalidationAttribute(string groupName, CacheSettings settings, string[] parameterProperties, bool bySimilar) {
            KeyBuilder.GroupName = groupName;
            KeyBuilder.Settings = settings;
            KeyBuilder.SelectedParameters = parameterProperties;
            this.bySimilar = bySimilar;
        }

        public TriggerInvalidationAttribute(string groupName, CacheSettings settings)
            : this(groupName, settings, string.Empty) { }

        public TriggerInvalidationAttribute(string groupName)
            : this(groupName, CacheSettings.Default, string.Empty) { }

        public TriggerInvalidationAttribute()
            : this(string.Empty) {

        }
        #endregion

        //Method executed at build time.
        public override void CompileTimeInitialize(MethodBase method, AspectInfo aspectInfo) {
            KeyBuilder.MethodParameters = method.GetParameters();
            KeyBuilder.MethodName = string.Format("{0}.{1}", method.DeclaringType.FullName, method.Name);
        }

        public override void OnExit(MethodExecutionArgs args) {
            string key = KeyBuilder.BuildCacheKey(args.Instance, args.Arguments);

            if (bySimilar) {
                CacheService.Cache.DeleteSimilar(key);
            } else if (CacheService.Cache.Contains(key)) {
                CacheService.Cache.Delete(key);
            }

            base.OnExit(args);
        }
    }
}