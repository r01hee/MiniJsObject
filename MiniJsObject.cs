#region Copyright notice and license

//  Copyright (c) 2020 R01hee
//
// Released under the MIT license
// https://github.com/r01hee/MiniJsObject/blob/master/LICENSE
//
// For more information: https://github.com/r01hee/MiniJsObject

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace MiniJsObject
{
    public static class JsObject
    {
        #region  public

        public static T FromJsObject<T>(object jsObject)
        {
            var type = typeof(T);

            return (T)Parse(jsObject, type);
        }

        #endregion

        #region utils

        private static bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        private static bool IsGuidType(Type type)
        {
            return typeof(Guid).Equals(type) || typeof(Guid?).Equals(type);
        } 

        private static void FindMember(string name, PropertyInfo[] properties, FieldInfo[] fields, out PropertyInfo property, out FieldInfo field)
        {
            Func<MemberInfo, bool> predicate = x => x.Name == name || x.Name.ToLower() == name.ToLower();
            property = (PropertyInfo)properties.FirstOrDefault(predicate);
            field = (FieldInfo)fields.FirstOrDefault(predicate);
        }

        private static bool AnyGenericTypes(Type type, params Type[] genericTypes)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            var args = type.GetGenericArguments();

            return genericTypes.Any(x => x.MakeGenericType(args).Equals(type));
        }

        private static bool IsNumericType(Type type)
        {
            return (typeof(Byte).Equals(type) ||
                    typeof(Int16).Equals(type) ||
                    typeof(Int32).Equals(type) ||
                    typeof(Int64).Equals(type) ||
                    typeof(SByte).Equals(type) ||
                    typeof(UInt16).Equals(type) ||
                    typeof(UInt32).Equals(type) ||
                    typeof(UInt64).Equals(type) ||
                    typeof(BigInteger).Equals(type) ||
                    typeof(Decimal).Equals(type) ||
                    typeof(Double).Equals(type) ||
                    typeof(Single).Equals(type));
        }

        private static bool IsNumeric(object value)
        {
            return (value is Byte ||
                    value is Int16 ||
                    value is Int32 ||
                    value is Int64 ||
                    value is SByte ||
                    value is UInt16 ||
                    value is UInt32 ||
                    value is UInt64 ||
                    value is BigInteger ||
                    value is Decimal ||
                    value is Double ||
                    value is Single);
        }

        #endregion

        #region Parse

        private static bool TryParseEnum(object jsObject, Type type, out object obj)
        {
            Type actualType = type.IsGenericType ? type.GetGenericArguments()[0] : type;
            if (jsObject == null || !typeof(Enum).IsAssignableFrom(actualType))
            {
                obj = null;
                return false;
            }

            var members = Enum.GetValues(actualType).Cast<Enum>().ToArray();
            if (jsObject is string str)
            {
                Enum m;

                m = members.FirstOrDefault(m => str == actualType.GetField(m.ToString()).GetCustomAttribute<EnumMemberAttribute>()?.Value);
                if (m != null)
                {
                    obj = m;
                    return true;
                }

                m = members.FirstOrDefault(m => str == m.ToString());
                if (m != null)
                {
                    obj = m;
                    return true;
                }
            }
            
            if (TryParseNumeric(jsObject, Enum.GetUnderlyingType(actualType), out var parsedObj))
            {
                var parsedObjType = parsedObj.GetType();

                var m = members.FirstOrDefault(m => parsedObj.Equals(Convert.ChangeType(m, parsedObjType)));
                if (m != null)
                {
                    obj = m;
                    return true;
                }
            }
            
            obj = null;
            return false;
        }

        private static bool TryParseNumeric(object jsObject, Type type, out object obj)
        {
            Type actualType = IsNullable(type) ? type.GetGenericArguments()[0] : type;
            if (!IsNumeric(jsObject) || !IsNumericType(actualType))
            {
                obj = null;
                return false;
            }

            obj = Convert.ChangeType(jsObject, actualType);
            return true;
        }
        
        private static bool TryParseGuid(object jsObject, Type type, out object obj)
        {
            if (jsObject == null || !(jsObject is string jsObjectStr) || !IsGuidType(type))
            {
                obj = null;
                return false;
            }

            obj = new Guid(jsObjectStr);
            return true;
        }

        private static bool TryParseString(object jsObject, Type type, out object obj)
        {
            if (jsObject == null || !(jsObject is string jsObjectStr) || !typeof(string).Equals(type))
            {
                obj = null;
                return false;
            }

            obj = jsObject;
            return true;
        }

        private static bool TryParseValue(object jsObject, Type type, out object obj)
        {
            if (jsObject == null)
            {
                obj = null;
                return false;
            }

            if (jsObject is string jsObjectStr)
            {
                if (typeof(string).Equals(type))
                {
                    obj = jsObject;
                    return true;
                }

                if (IsGuidType(type))
                {
                    obj = new Guid(jsObjectStr);
                    return true;
                }
            }
            else if (typeof(ValueType).IsAssignableFrom(jsObject.GetType()) && typeof(ValueType).IsAssignableFrom(type))
            {
                if (IsNullable(type))
                {
                    obj = Convert.ChangeType(jsObject, type.GetGenericArguments()[0]);
                    return true;
                }

                obj = jsObject;
                return true;
            }

            obj = null;
            return false;
        }

        private static bool TryParseDictionary(IDictionary jsObject, Type type, out object value)
        {
            if (jsObject == null)
            {
                value = null;
                return false;
            }

            object obj;
            try
            {
                obj = Activator.CreateInstance(type);
            }
            catch
            {
                value = null;
                return false;
            }

            var properties = type.GetProperties();
            var fields = type.GetFields();

            foreach (DictionaryEntry j in jsObject)
            {
                FindMember((string)j.Key, properties, fields, out var p, out var f);

                if (p != null)
                {
                    p.SetValue(obj, Parse(j.Value, p.PropertyType));
                }
                else if (f != null)
                {
                    f.SetValue(obj, Parse(j.Value, f.FieldType));
                }
            }

            value = obj;
            return true;
        }

        private static bool TryParseList(IList jsObject, Type type, out object value)
        {
            if (jsObject == null || !typeof(IEnumerable).IsAssignableFrom(type))
            {
                value = null;
                return false;
            }

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var list = ParseListElements(jsObject, elementType);
                var array = Array.CreateInstance(elementType, list.Count);
                list.CopyTo(array, 0);

                value = array;
                return true;
            }

            if (typeof(ICollection).IsAssignableFrom(type) ||
                AnyGenericTypes(type, typeof(ICollection<>), typeof(IList<>), typeof(List<>)))
            {
                var elementType = type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object);
                value = ParseListElements(jsObject, elementType);
                return true;
            }

            value = null;
            return false;
        }

        private static IList ParseListElements(IList jsObject, Type type)
        {
            if (jsObject == null)
            {
                return null;
            }

            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type));

            foreach (var j in jsObject)
            {
                list.Add(Parse(j, type));
            }

            return list;
        }

        private static object Parse(object jsObject, Type type)
        {
            object value;
            if (TryParseDictionary(jsObject as IDictionary, type, out value) ||
                TryParseList(jsObject as IList, type, out value) ||
                TryParseEnum(jsObject, type, out value) ||
                TryParseGuid(jsObject, type, out value) ||
                TryParseString(jsObject, type, out value) ||
                TryParseNumeric(jsObject, type, out value)) 
            {
                return value;
            }

            return null;
        }

        #endregion
    }
}