// Copyright 2025 Kyle Ebbinga

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Parallel.Core.Net;
using Parallel.Service.Requests;

namespace Parallel.Service
{
    public class RequestHandler
    {
        public Dictionary<string, Type> Requests { get; }

        public RequestHandler()
        {
            Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => typeof(BaseRequest).IsAssignableFrom(t) && !t.IsAbstract).ToArray();
            Requests = types.ToDictionary(t => t.Name.Replace("Request", ""), t => t, StringComparer.OrdinalIgnoreCase);

            // Logs if any requests failed
            if (Requests.Count != types.Length)
            {
                int remaining = types.Length - Requests.Count;
                Log.Warning($"Failed to register {remaining} requests");
            }
        }

        /// <summary>
        /// Creates an <see cref="IRequest"/> to be handled.
        /// </summary>
        /// <param name="request">The name of the request.</param>
        /// <returns>The corresponding <see cref="IRequest"/>. If none was found a help request will be returned.</returns>
        public IRequest? CreateNew(ServerRequest request)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>(request.Parameters, StringComparer.OrdinalIgnoreCase);
            if (!Requests.TryGetValue(request.Name, out Type? requestType))
            {
                Log.Warning($"Unknown command: {request.Name}");
                return null;
            }

            // Instantiate the request object
            object? instance = Activator.CreateInstance(requestType);
            if (instance is not IRequest requestInstance) return null;

            // Map parameters to object properties
            foreach (PropertyInfo prop in requestType.GetProperties())
            {
                if (headers.TryGetValue(prop.Name, out string? value))
                {
                    try
                    {
                        object? converted = Convert.ChangeType(value, prop.PropertyType);
                        prop.SetValue(instance, converted);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Failed to convert '{value}' to {prop.PropertyType.Name} for property '{prop.Name}': {ex.Message}");
                    }
                }
            }


            // Validate required properties
            List<ValidationResult>? validationResults = new List<ValidationResult>();
            ValidationContext? context = new ValidationContext(instance, serviceProvider: null, items: null);
            if (!Validator.TryValidateObject(instance, context, validationResults, validateAllProperties: true))
            {
                string? errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
                Log.Warning($"Validation failed for '{request.Name}': {errors}");
                return null;
            }

            return requestInstance;
        }
    }
}