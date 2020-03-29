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
using System.Linq;
using System.Reflection;

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

        #endregion

        #region Parse

        private static bool TryParseValue(object jsObject, Type type, out object obj)
        {
            if (jsObject == null ||
                !(typeof(ValueType).IsAssignableFrom(type) || typeof(string).Equals(type)))
            {
                obj = null;
                return false;
            }

            if (IsGuidType(type))
            {
                var j = jsObject as string;
                if (j == null)
                {
                    obj = null;
                    return false;
                }
                obj = new Guid(j);
                return true;
            }

            if (IsNullable(type))
            {
                obj = Convert.ChangeType(jsObject, type.GetGenericArguments()[0]);
                return true;
            }

            obj = jsObject;
            return true;
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
                TryParseValue(jsObject, type, out value)) 
            {
                return value;
            }

            return null;
        }

        #endregion
    }
}