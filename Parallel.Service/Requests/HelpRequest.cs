// Copyright 2025 Kyle Ebbinga

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Parallel.Service.Responses;

namespace Parallel.Service.Requests
{
    [Description("Lists all avalible requests to the server.")]
    public class HelpRequest : BaseRequest
    {
        public override Task<IResponse> ExecuteAsync()
        {
            RequestHandler handler = new RequestHandler();

            JArray jsonArray = new JArray();
            foreach (KeyValuePair<string, Type> request in handler.Requests.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                Type type = request.Value;
                DescriptionAttribute? descAttr = type.GetCustomAttribute<DescriptionAttribute>();
                string description = descAttr?.Description ?? "No description provided.";

                JArray parameters = new JArray();
                foreach (PropertyInfo prop in type.GetProperties())
                {
                    parameters.Add(new JObject
                    {
                        ["name"] = prop.Name,
                        ["type"] = prop.PropertyType.Name,
                        ["required"] = prop.GetCustomAttribute<RequiredAttribute>() != null
                    });
                }

                // Build JObject for this request
                JObject summary = new JObject
                {
                    ["name"] = request.Key,
                    ["description"] = description,
                    ["parameters"] = parameters
                };

                jsonArray.Add(summary);
            }

            return Task.FromResult<IResponse>(Json(jsonArray));
        }
    }
}