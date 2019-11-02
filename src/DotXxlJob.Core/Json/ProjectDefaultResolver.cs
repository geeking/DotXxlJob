using System.Reflection;
using Utf8Json;
using Utf8Json.Formatters;
using Utf8Json.Resolvers;

namespace DotXxlJob.Core.Json
{
    public class ProjectDefaultResolver : IJsonFormatterResolver
    {
        public static IJsonFormatterResolver Instance = new ProjectDefaultResolver();

        // configure your resolver and formatters.
        private static readonly IJsonFormatter[] formatters = {
            new DateTimeFormatter("yyyy-MM-dd HH:mm:ss"),
            new NullableDateTimeFormatter("yyyy-MM-dd HH:mm:ss")
        };

        private static readonly IJsonFormatterResolver[] resolvers = new[]
        {
            EnumResolver.UnderlyingValue,
            StandardResolver.AllowPrivateExcludeNullSnakeCase
        };

        private ProjectDefaultResolver()
        {
        }

        public IJsonFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IJsonFormatter<T> formatter;

            static FormatterCache()
            {
                foreach (var item in formatters)
                {
                    foreach (var implInterface in item.GetType().GetTypeInfo().ImplementedInterfaces)
                    {
                        var ti = implInterface.GetTypeInfo();
                        if (ti.IsGenericType && ti.GenericTypeArguments[0] == typeof(T))
                        {
                            formatter = (IJsonFormatter<T>)item;
                            return;
                        }
                    }
                }

                foreach (var item in resolvers)
                {
                    var f = item.GetFormatter<T>();
                    if (f != null)
                    {
                        formatter = f;
                        return;
                    }
                }
            }
        }
    }
}