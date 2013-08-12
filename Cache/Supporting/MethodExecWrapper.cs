using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheAspect.Supporting
{
    [Serializable]
    public class MethodExecWrapper
    {
        public object ReturnValue { get; set; }
        public DateTime Timestamp { get; set; }
        public object[] Arguments { get; set; }
    }
}
