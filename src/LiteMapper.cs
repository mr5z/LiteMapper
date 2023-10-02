using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LiteMapper
{
    public class MappingService
    {
        private readonly IDictionary<Type, Type> typeMappings = new Dictionary<Type, Type>();
        private readonly Builder builder = new Builder();

        public Builder Configure(Assembly assembly, string sourceNs, string destinationNs, string pattern = "Dto")
        {
            // Retrieve all classes in the source and destination namespaces
            var sourceTypes = GetTypesInNamespace(assembly, sourceNs);
            var destinationTypes = GetTypesInNamespace(assembly, destinationNs);

            // Build type mappings based on naming conventions
            foreach (var sourceType in sourceTypes)
            {
                var destinationType = destinationTypes.FirstOrDefault(dt => Regex.IsMatch(dt.Name, $"{sourceType.Name}{pattern}"));
                if (destinationType != null)
                {
                    typeMappings[sourceType] = destinationType;
                    typeMappings[destinationType] = sourceType;
                }
            }

            return builder;
        }

        public T Map<T>(object source)
        {
            var sourceType = source.GetType();

            if (!typeMappings.ContainsKey(sourceType))
            {
                throw new InvalidOperationException($"Mapping for {sourceType} is not configured.");
            }

            var destinationType = typeMappings[sourceType];
            var destination = Activator.CreateInstance(destinationType);

            // Copy property values from source to destination
            foreach (var sourceProperty in sourceType.GetProperties())
            {
                var destinationProperty = destinationType.GetProperty(sourceProperty.Name);
                if (destinationProperty != null)
                {
                    var transformationFunc = builder.GetTransformationFunc(sourceProperty);
                    var transformedValue = transformationFunc?.Invoke(sourceProperty.GetValue(source)) ?? sourceProperty.GetValue(source);
                    destinationProperty.SetValue(destination, transformedValue);
                }
            }

            return (T)destination;
        }

        public class Builder
        {
            private readonly IDictionary<Type, Dictionary<string, Func<object, object>>> propertyMappings = 
                new Dictionary<Type, Dictionary<string, Func<object, object>>>();

            public Builder On<TSource, TDestination>(Expression<Func<TSource, object?>> sourceProperty, Func<object, object> transformer)
            {
                MapProperty<TSource, TDestination>(sourceProperty, transformer);
                return this;
            }

            public Builder On<TSource, TDestination>(Expression<Func<TSource, object?>> sourceProperty, IDataTransformer<object> transformer)
            {
                MapProperty<TSource, TDestination>(sourceProperty, t => transformer.Transform(t));
                return this;
            }

            public Func<object, object>? GetTransformationFunc(PropertyInfo sourceProperty)
            {
                var sourceType = sourceProperty.DeclaringType;
                var propertyName = sourceProperty.Name;

                if (propertyMappings.ContainsKey(sourceType) && propertyMappings[sourceType].ContainsKey(propertyName))
                {
                    return propertyMappings[sourceType][propertyName];
                }

                return null;
            }

            private void MapProperty<TSource, TDestination>(Expression<Func<TSource, object?>> sourceProperty, Func<object, object> transformation)
            {
                var sourceType = typeof(TSource);
                var propertyName = ((MemberExpression)sourceProperty.Body).Member.Name;

                if (!propertyMappings.ContainsKey(sourceType))
                {
                    propertyMappings[sourceType] = new Dictionary<string, Func<object, object>>();
                }

                propertyMappings[sourceType][propertyName] = transformation;
            }
        }

        private IEnumerable<Type> GetTypesInNamespace(Assembly assembly, string namespaceName)
        {
            // Retrieve all types in the specified namespace
            return assembly.GetTypes()
                .Where(t => string.Equals(t.Namespace, namespaceName, StringComparison.Ordinal));
        }
    }
}