﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using createsend_dotnet;
using Newtonsoft.Json;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Campmon.Dynamics.Plugins.Operations
{
    public class RequestAccessTokenOperation : IOperation
    {
        private ConfigurationService configService;
        private ITracingService trace;

        public RequestAccessTokenOperation(ConfigurationService configSvc,  ITracingService tracer)
        {
            configService = configSvc;
            trace = tracer;
        }

        public string Execute(string serializedData)
        {
            trace.Trace("Deserializing input.");
            var userInput = JsonConvert.DeserializeObject<RequestAccessTokenInput>(serializedData);

            trace.Trace("Getting token.");
            OAuthTokenDetails auth = null;
            try
            {
                auth = General.ExchangeToken(userInput.ClientId, userInput.ClientSecret, userInput.RedirectUri, userInput.Code);
            }
            catch(Exception ex)
            {
                throw new InvalidPluginExecutionException("Unable to get exchange token. " + ex.Message);
            }
            //to be safe subtract a minute before calculating when the token expires to account for the time it took to get to now from when the server generated the token.
            var expiresOn = DateTime.UtcNow.AddMinutes(-1).AddSeconds(auth.expires_in);
            trace.Trace("Token will expire on: {0}", expiresOn);

            var configId = configService.GetConfigId();
            if (!configId.HasValue)
            {
                configId = Guid.Empty;
            }

            configService.SaveOAuthToken(configId.Value, auth.access_token, auth.refresh_token, expiresOn);

            return "Success";
            
        }        
    }

    public class RequestAccessTokenInput
    {
        public string Code { get; set; }
        public int ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
    }
}
