using System;
using Microsoft.Xrm.Sdk;

namespace Octono.Dynamics.Extensions
{
    /// <summary>
    /// None static class to contain ReadValue helper method allowing the definition of the entity constraint to
    /// be defined at the class level.
    /// </summary>
    /// <remarks>
    /// This cleans up the API in comparison to the extension method version on IPluginExecutionContext
    /// So instead of:
    /// context.Read<OptionSetValue, new_entitytype>(entity => entity.attributename);
    /// You get:
    /// context.Read(entity => entity.attributename);
    /// </remarks>  
    /// <typeparam name="TEntity"></typeparam>
    public class PluginHelper<TEntity> where TEntity : Entity
    {
        private readonly IPluginExecutionContext _context;

        public PluginHelper(IPluginExecutionContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Reads the latest attribute value using the priority Target -> PreImage -> PostImage
        /// </summary>
        /// <typeparam name="TValue">The type of the attribute value to read</typeparam>
        /// <param name="readProperty">The delegate that defines the property to read</param>
        /// <param name="exceptionMessage">The message to appear in the exception message when the value is not available on either target or image</param>
        /// <returns>The value read from the prioritised entities</returns> 
        public TValue ReadValue<TValue>(Func<TEntity, TValue> readProperty, string exceptionMessage = null)
        {
            var target  = _context.LoadTarget<TEntity>(false);
            var image   = _context.LoadPreImage<TEntity>() ?? _context.LoadPostImage<TEntity>();
            var result  = readProperty(target ?? image);

            //Resharper warnings/recommendations...
            //Cannot use Null Coalescing Expression due to not being able to apply class OR nullable constraint
            //Value type null compare won't occur as all CRM value types are nullable 
            if (result == null && !string.IsNullOrEmpty(exceptionMessage))
            {
                throw new InvalidPluginExecutionException(exceptionMessage);
            }
            return result;
        }
    }

    public static class PluginExtensions
    {
        public static T Resolve<T>(this IServiceProvider provider, Func<T> createWhenNull = null) where T : class
        {
            var instance = (T)provider.GetService(typeof(T));

            if (instance == null && createWhenNull != null)
            {
                return createWhenNull();
            }
            return instance;
        }

        public static T LoadTarget<T>(this IPluginExecutionContext context, bool throwOnNull = true) where T : Entity
        {
            var target = LoadInput<Entity>(context, "Target", throwOnNull);

            return target != null ? target.ToEntity<T>() : null;
        }

        public static T LoadInput<T>(this IPluginExecutionContext context, string key, bool throwOnNull = true)
            where T : class
        {
            T result = null;
            if (context.InputParameters.Contains(key))
            {
                result = context.InputParameters[key] as T;
            }

            if (result == null && throwOnNull)
            {
                throw new InvalidPluginExecutionException(
                    string.Format("{0} not defined on InputParameter collection.  Check plugin registration is valid.", key));
            }

            return result;
        }

        public static bool WasTriggeredByTargetAttribute(this IPluginExecutionContext context, string attributeName)
        {
            var target = context.LoadInput<Entity>("Target", false);
            return target != null && target.Attributes.Contains(attributeName);
        }

        public static OptionSetValue LoadStatus(this IPluginExecutionContext context)
        {
            return context.InputParameters.Contains("Status")
                       ? (OptionSetValue)context.InputParameters["Status"]
                       : null;
        }

        public static OptionSetValue LoadState(this IPluginExecutionContext context)
        {
            return context.InputParameters.Contains("State")
                       ? (OptionSetValue)context.InputParameters["State"]
                       : null;
        }

        public static T LoadPostImage<T>(this IPluginExecutionContext context) where T : Entity
        {
            return context.PostEntityImages != null && context.PostEntityImages.Contains("PostImage")
                       ? context.PostEntityImages["PostImage"].ToEntity<T>()
                       : null;
        }

        public static T LoadPreImage<T>(this IPluginExecutionContext context) where T : Entity
        {
            return context.PreEntityImages != null && context.PreEntityImages.Contains("PreImage")
                       ? context.PreEntityImages["PreImage"].ToEntity<T>()
                       : null;
        }

        /// <summary>
        /// Helper method for plugins for reading the property defined 
        /// on the <paramref name="readProperty"/> callback from either the <paramref name="target"/> 
        /// or <paramref name="image"/> (pre/post) depending on whichever is populated.  So if it is an update triggering
        /// the plugin it will always read the latest value, regardless of whether the attribute being read was one of the
        /// triggering attributes.
        /// </summary>
        /// <typeparam name="T">The type for the return value (referencee type only)</typeparam>
        /// <typeparam name="TE">The entity types for target and image</typeparam>
        /// <param name="readProperty">The callback to determine which property on the entity to be read</param>
        /// <param name="propertyDisplayName">Friendly display name for the property if an exception is thrown</param>
        /// <returns>The value off either the target if it exists or the image</returns>
        /// <exception cref="NullReferenceException"></exception>
        public static T Read<T, TE>(this IPluginExecutionContext context, Func<TE, T> readProperty,
                                    string propertyDisplayName = "Value")
            where T : class
            where TE : Entity
        {
            var image = context.LoadPreImage<TE>() ?? context.LoadPostImage<TE>();

            var result = readProperty(context.LoadTarget<TE>()) ?? readProperty(image);

            if (result == null)
            {
                throw new NullReferenceException(propertyDisplayName + " has not been defined.");
            }
            return result;
        }
    }
}