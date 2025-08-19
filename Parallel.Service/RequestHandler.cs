// Copyright 2025 Kyle Ebbinga

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Parallel.Core.Net;
using Parallel.Service.Requests;

namespace Parallel.Service
{
    public class RequestHandler
    {
        private readonly Dictionary<string, Type> _requests;

        public RequestHandler()
        {
            Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => typeof(BaseRequest).IsAssignableFrom(t) && !t.IsAbstract).ToArray();
            _requests = types.ToDictionary(t => t.Name.Replace("Request", ""), t => t, StringComparer.OrdinalIgnoreCase);

            // Logs if all requests registered.
            if (_requests.Count == types.Length)
            {
                Log.Information($"Successfully registered all {types.Length} requests");
            }
            else
            {
                int remaining = types.Length - _requests.Count;
                Log.Warning($"Failed to register {remaining} requests");
            }
        }

        /// <summary>
        /// Creates an <see cref="IRequest"/> to be handled.
        /// </summary>
        /// <param name="request">The name of the request.</param>
        /// <returns>The corresponding <see cref="IRequest"/>. If none was found a help request will be returned.</returns>
        public IRequest CreateNew(ServerRequest request)
        {
            if (!_requests.TryGetValue(request.Name, out Type? requestType))
            {
                Log.Warning($"Unknown command: {request.Name}");
                throw new InvalidOperationException($"Unknown command: {request.Name}");
            }

            // Instantiate the request object
            object? instance = Activator.CreateInstance(requestType);
            if (instance is not IRequest requestInstance)
                throw new InvalidOperationException($"Type '{requestType.Name}' does not implement IRequest.");

            // Map parameters to object properties
            foreach (PropertyInfo? prop in requestType.GetProperties())
            {
                if (request.Parameters.TryGetValue(prop.Name, out string? value))
                {
                    object? converted = Convert.ChangeType(value, prop.PropertyType);
                    prop.SetValue(instance, converted);
                }
            }

            // Validate required properties
            List<ValidationResult>? validationResults = new List<ValidationResult>();
            ValidationContext? context = new ValidationContext(instance, serviceProvider: null, items: null);
            if (!Validator.TryValidateObject(instance, context, validationResults, validateAllProperties: true))
            {
                string? errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
                Log.Warning($"Validation failed for '{request.Name}': {errors}");
                throw new InvalidOperationException($"Validation failed: {errors}");
            }

            return requestInstance;
        }
    }
}