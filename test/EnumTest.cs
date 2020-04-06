using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Xunit;

namespace MiniJsObject.Test
{
    public class EnumTest
    {
        enum SerializationEnum
        {
            [EnumMember(Value="FIRST_MEMBER")] FirstMember = 1,
            [EnumMember(Value="SECOND_MEMBER")] SecondMember = 2,
        }

        enum NonSerializationEnum
        {
            FirstMember = 1,
            SecondMember = 2,
        }

        class TestClass<T> where T : struct
        {
            public T Enum1 { get; set; }

            public T? Enum2 { get; set; }
        }

        [Fact]
        public void EnumMemberValue()
        {
            IDictionary jsObject = new Dictionary<string, object>
            {
                {"Enum1", "FIRST_MEMBER"},
                {"Enum2", "SECOND_MEMBER"},
            };

            var res = JsObject.FromJsObject<TestClass<SerializationEnum>>(jsObject);

            Assert.Equal(SerializationEnum.FirstMember, res.Enum1);
            Assert.Equal(SerializationEnum.SecondMember, res.Enum2);
        }

        [Fact]
        public void Numeric_SerializationEnum()
        {
            TestNumeric<SerializationEnum>();
        }

        [Fact]
        public void Numeric_NonSerializationEnum()
        {
            TestNumeric<NonSerializationEnum>();
        }

        private void TestNumeric<T>() where T : struct
        {
            IDictionary jsObject = new Dictionary<string, object>
            {
                {"Enum1", 1},
                {"Enum2", 2},
            };

            var res = JsObject.FromJsObject<TestClass<T>>(jsObject);

            Assert.Equal(Enum.ToObject(typeof(T), 1), res.Enum1);
            Assert.Equal(Enum.ToObject(typeof(T), 2), res.Enum2);
        }

        [Fact]
        public void MemberName_NonSerializationEnum()
        {
            TestMemberName<SerializationEnum>();
        }

        [Fact]
        public void MemberName_SerializationEnum()
        {
            TestMemberName<NonSerializationEnum>();
        }

        private void TestMemberName<T>() where T : struct
        {
            IDictionary jsObject = new Dictionary<string, object>
            {
                {"Enum1", "FirstMember"},
                {"Enum2", "SecondMember"},
            };

            var res = JsObject.FromJsObject<TestClass<T>>(jsObject);

            Assert.Equal(Enum.ToObject(typeof(T), 1), res.Enum1);
            Assert.Equal(Enum.ToObject(typeof(T), 2), res.Enum2);
        }

        [Fact]
        public void Null_SerializationEnum()
        {
            TestNull<SerializationEnum>();
        }

        [Fact]
        public void Null_NonSerializationEnum()
        {
            TestNull<NonSerializationEnum>();
        }

        private void TestNull<T>() where T : struct
        {
            IDictionary jsObject = new Dictionary<string, object>
            {
                {"Enum2", null},
            };

            var res = JsObject.FromJsObject<TestClass<T>>(jsObject);

            Assert.Null(res.Enum2);
        }

        [Fact]
        public void InvalidValue_SerializationEnum()
        {
            TestInvalidValue<SerializationEnum>();
        }

        [Fact]
        public void InvalidValue_NonSerializationEnum()
        {
            TestInvalidValue<NonSerializationEnum>();
        }

        private void TestInvalidValue<T>() where T : struct
        {
            IDictionary jsObject = new Dictionary<string, object>
            {
                {"Enum1", "FIRST-MEMBER"},
                {"Enum2", "SECOND-MEMBER"},
            };

            var res = JsObject.FromJsObject<TestClass<SerializationEnum>>(jsObject);

            Assert.Equal(default, res.Enum1);
            Assert.Null(res.Enum2);
        }
    }
}
