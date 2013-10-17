using System;
using System.Collections.Generic;
using System.Reflection;
using CacheAspect;
using CacheAspect.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PostSharp.Aspects;

namespace TestCache {
    [TestClass]
    public class KeyBuilderTest {
        const string splitter = "%";
        const string groupName = "TESTGROUP";

        const string methodName1 = "SampleMethod1";
        const string methodNameById = "SampleMethodById";
        const string methodName2 = "SampleMethod2";
        const string methodName3 = "SampleMethod3";

        const int int32ParaArgValue = 2048576;
        const string stringParaArgValue = "Sample String";
        static readonly DateTime dtParaValue = DateTime.Now;



        static readonly object instance = new object();

        ParameterInfo[] sampleParameters1;
        ParameterInfo[] sampleParametersById;
        ParameterInfo[] sampleParameters2;
        ParameterInfo[] sampleParameters3;

        Arguments args, argsForSampleClassA, argsForSampleClassB;

        [TestInitialize]
        public void Setup() {
            // prepare ParameterInfo array
            sampleParameters1 = typeof(KeyBuilderTest).GetMethod(methodName1).GetParameters();
            sampleParametersById = typeof(KeyBuilderTest).GetMethod(methodNameById).GetParameters();
            sampleParameters2 = typeof(KeyBuilderTest).GetMethod(methodName2).GetParameters();
            sampleParameters3 = typeof(KeyBuilderTest).GetMethod(methodName3).GetParameters();

            args = new PostSharp.Aspects.Internals.Arguments<int, string, DateTime>() {
                Arg0 = int32ParaArgValue,
                Arg1 = stringParaArgValue,
                Arg2 = dtParaValue
            };

            argsForSampleClassA = new PostSharp.Aspects.Internals.Arguments<SampleClassA>() {
                Arg0 = new SampleClassA() {
                    PropertyT = int32ParaArgValue,
                    PropertyU = stringParaArgValue,
                    PropertyV = dtParaValue,
                    InstanceB = null
                }
            };
        }

        [TestMethod]
        public void TestBuildCacheKeyWithSettingsAsDefault() {
            KeyBuilder keyBuilder = new KeyBuilder() {
                Settings = CacheSettings.Default,
                GroupName = groupName,
                MethodName = methodName1,
                MethodParameters = sampleParameters1
            };

            string expected = groupName + splitter + instance.ToString() + ";" + int32ParaArgValue + splitter + stringParaArgValue + splitter + dtParaValue.ToString();
            string actual = keyBuilder.BuildCacheKey(instance, args);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestBuildCacheKeyWithSettingsAsDefaultWhenSomeParametersAreIgnored() {
            KeyBuilder keyBuilder = new KeyBuilder() {
                Settings = CacheSettings.Default,
                GroupName = groupName,
                MethodName = methodName1,
                MethodParameters = sampleParameters2
            };

            string expected = groupName + splitter + instance.ToString() + ";" + stringParaArgValue + splitter + dtParaValue.ToString();
            string actual = keyBuilder.BuildCacheKey(instance, args);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestBuildCacheKeyWithSettingsAsDefaultWhenUseExtractPropertyAttribute() {
            KeyBuilder keyBuilder = new KeyBuilder() {
                Settings = CacheSettings.Default,
                GroupName = groupName,
                MethodName = methodName3,
                MethodParameters = sampleParameters3
            };

            string expected = groupName + splitter + instance.ToString() + ";" + int32ParaArgValue;
            string actual = keyBuilder.BuildCacheKey(instance, argsForSampleClassA);

            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void TestBuildCacheKeyWithSettingsAsIgnoreParameters() {
            KeyBuilder keyBuilder = new KeyBuilder() {
                Settings = CacheSettings.IgnoreParameters,
                GroupName = groupName,
                MethodName = methodName1,
                MethodParameters = sampleParameters1
            };

            string expected = groupName + splitter + instance.ToString() + ";";
            string actual = keyBuilder.BuildCacheKey(instance, args);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestBuildCacheKeyWithSettingsAsUseSelectedParameters() {
            KeyBuilder keyBuilder = new KeyBuilder() {
                Settings = CacheSettings.UseSelectedParameters,
                GroupName = groupName,
                MethodName = methodName1,
                MethodParameters = sampleParameters1,
                SelectedParameters = new string[] { "paraInt32" }
            };

            string expected = groupName + splitter + instance.ToString() + ";" + int32ParaArgValue;
            string actual = keyBuilder.BuildCacheKey(instance, args);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestBuildCacheKeyWithSettingsAsUseSelectedParametersWhenSomeParamtersAreIgnored() {
            KeyBuilder keyBuilder = new KeyBuilder() {
                Settings = CacheSettings.UseSelectedParameters,
                GroupName = groupName,
                MethodName = methodName1,
                MethodParameters = sampleParameters2,
                SelectedParameters = new string[] { "paraInt32", "paraString" }
            };

            string expected = groupName + splitter + instance.ToString() + ";" + stringParaArgValue;
            string actual = keyBuilder.BuildCacheKey(instance, args);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestBuildCacheKeyWithSettingsAsUseIdWhenNormal() {
            KeyBuilder keyBuilder = new KeyBuilder() {
                Settings = CacheSettings.UseId,
                GroupName = groupName,
                MethodName = methodNameById,
                MethodParameters = sampleParametersById,
            };

            string expected = groupName + splitter + instance.ToString() + ";" + int32ParaArgValue;
            string actual = keyBuilder.BuildCacheKey(instance, args);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestBuildCacheKeyWithSettingsAsUseIdWhenMethodHasNoIdParameter() {
            KeyBuilder keyBuilder = new KeyBuilder() {
                Settings = CacheSettings.UseId,
                GroupName = groupName,
                MethodName = methodNameById,
                MethodParameters = sampleParameters2,
            };

            string expected = groupName + splitter + instance.ToString() + ";";
            string actual = keyBuilder.BuildCacheKey(instance, args);

            Assert.AreEqual(expected, actual);
        }


        public string SampleMethod1(int paraInt32, string paraString, DateTime dtParameter) { return string.Empty; }
        public string SampleMethod2(
            [CacheAspect.Attributes.Ignore] int paraInt32, 
            string paraString, 
            DateTime dtParameter) { 
            return string.Empty; 
        }
        public DateTime SampleMethodById(int id) { return DateTime.Now; }
        public string SampleMethod3([ExtractProperty("PropertyT")]SampleClassA instanceA) { return null; }


        public class SampleClassA {
            public int PropertyT { get; set; }
            public string PropertyU { get; set; }
            public DateTime PropertyV { get; set; }

            public SampleClassB InstanceB { get; set; }
        }

        public class SampleClassB {
            public int PropertyX { get; set; }
            public string PropertyY { get; set; }
            public DateTime PropertyZ { get; set; }
        }

    }
}

