using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheAspect.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class IgnoreAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class OverrideValueAttribute : Attribute {
        public object Value { get; set; }
        
        public OverrideValueAttribute(object value) {
            Value = value;
        }

        //public OverrideValueAttribute(string propertyName) {
        //    PropertyName = propertyName;
        //}

        //public OverrideValueAttribute(string propertyName, object value) {
        //    PropertyName = propertyName;
        //    Value = value;
        //}
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class ExtractPropertyAttribute : Attribute {
        public string Accessor { get; set; }
        
        public ExtractPropertyAttribute(string accessor)  {
            Accessor = accessor;
        }

        //public ExtractPropertyAttribute(string accessor, string overridePropertyName) {
        //    Accessor = accessor;
        //    PropertyName = overridePropertyName;
        //}
    }

    //[AttributeUsage(AttributeTargets.Parameter)]
    //public class UsePropertyAttribute : Attribute {
    //    public UsePropertyAttribute(string parameterValue)
    //    {
    //        _parameterValue = parameterValue;
    //    }

    //    string _parameterValue = string.Empty;
    //}

    //[AttributeUsage(AttributeTargets.Parameter)]
    //public class UsePropertyMemberAttribute : Attribute {

    //}
}
