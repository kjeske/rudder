using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Rudder
{
    public static class WithExtension
    {
        private static readonly ConcurrentDictionary<Type, Func<object, object>> MemberwiseCloneCache =
            new ConcurrentDictionary<Type, Func<object, object>>();

        /// <summary>
        /// Returns new shallow copy of obj with changes defined by mutator.
        /// </summary>
        /// <param name="obj">Object to copy</param>
        /// <param name="mutator">Changes to apply</param>
        /// <returns></returns>
        public static T With<T>(this T obj, Action<T> mutator) where T : class
        {
            if (obj == null)
            {
                return null;
            }

            var cloneFunc = GetMemberwiseCloneFunc<T>();

            if (cloneFunc == null)
            {
                throw new InvalidOperationException($"There was a problem with cloning an instance of ${typeof(T).Name} type.");
            }

            var clone = (T) cloneFunc(obj);
            mutator(clone);
            return clone;
        }

        private static Func<object, object> GetMemberwiseCloneFunc<T>() where T : class
        {
            var type = typeof(T);

            if (MemberwiseCloneCache.ContainsKey(type))
            {
                return MemberwiseCloneCache[type];
            }
            
            var methodInfo = type.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

            if (methodInfo == null)
            {
                return null;
            }
            
            var delegateFunc = (Func<object, object>)Delegate.CreateDelegate(typeof(Func<object, object>), methodInfo);
            MemberwiseCloneCache.TryAdd(type, delegateFunc);
            return delegateFunc;
        }
    }
}