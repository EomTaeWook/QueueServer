using Dignus.Collections;
using Dignus.Log;
using System.Reflection;

namespace QueueHubServer.Internals
{
    public class RequestValidator
    {
        private static readonly Dictionary<Type, ArrayQueue<PropertyInfo>> _typePropertyCache = [];
        public static void CacheRequestProperties(Assembly assembly, params Type[] assignableTypes)
        {
            var relevantTypes = assembly.GetTypes()
            .Where(type => assignableTypes.Any(assignableType => type.IsAssignableTo(assignableType)));

            foreach (var type in relevantTypes)
            {
                if (!_typePropertyCache.TryGetValue(type, out ArrayQueue<PropertyInfo> properties))
                {
                    properties = [];
                    _typePropertyCache.Add(type, properties);
                }

                foreach (var property in type.GetProperties())
                {
                    properties.Add(property);
                }
            }
        }

        public static bool CheckProperties<T>(T request)
        {
            if (_typePropertyCache.TryGetValue(request.GetType(), out ArrayQueue<PropertyInfo> queue) == false)
            {
                return false;
            }

            foreach (var property in queue)
            {
                var value = property.GetValue(request);
                if (value == null)
                {
                    LogHelper.Error($"property : {property.Name} is null");
                    return false;
                }
            }
            return true;
        }
    }
}
