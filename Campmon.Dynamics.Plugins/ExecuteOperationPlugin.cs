﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Campmon.Dynamics.Utilities;
using Campmon.Dynamics.Plugins.Operations;
using Newtonsoft.Json;
using Microsoft.Xrm.Sdk;

namespace Campmon.Dynamics.Plugins
{
    //  campmon_ExecuteOperationAction
    //  Input:
    //    OperationName
    //    InputData
    //  Output:
    //    OutputData        
    public class ExecuteOperationPlugin : PluginBase
    {
        public override void OnExecute(IServiceProvider serviceProvider)
        {
            var orgService = serviceProvider.CreateSystemOrganizationService();
            var configService = new ConfigurationService(orgService, serviceProvider.GetTracingService());
            
            var trace = serviceProvider.GetTracingService();

            var operationFactory = new Dictionary<string, Func<IOperation>>
            {
                { "getclients", () => new GetClientsOperation(configService, orgService) },
                { "getclientlist", ()=> new GetClientListOperation(configService, orgService) },
                { "loadmetadata", () => new LoadMetadataOperation(configService, orgService, trace) },
                { "saveconfiguration", () => new SaveConfigurationOperation(orgService, configService, trace) },
                { "disconnect", () => new DisconnectOperation(configService) },
                { "requestaccesstoken", () => new RequestAccessTokenOperation(configService, trace) },
                { "isconnectedtocampaignmonitor", () => new IsConnectedToCampaignMonitorOperation(configService, trace) },
            };

            var pluginContext = serviceProvider.GetPluginExecutionContext();

            var operationName = pluginContext.InputParameters["OperationName"] as string;
            var inputData = pluginContext.InputParameters["InputData"] as string;

            trace.Trace("Operation: {0} Input: {1}", operationName, inputData);

            if (!operationFactory.ContainsKey(operationName))
            {
                trace.Trace("Operation not defined.");
                return;
            }

            var operation = operationFactory[operationName].Invoke();
            string outputData = null;

            try
            {
                trace.Trace("Executing operation.");
                outputData = operation.Execute(inputData);
            }
            catch (Exception ex)
            {
                trace.Trace("Fatal error: {0}", ex.Message);
                trace.Trace("Stack trace: {0}", ex.StackTrace);
                throw new InvalidPluginExecutionException("Fatal error: " + ex.Message);
            }

            pluginContext.OutputParameters["OutputData"] = outputData;
        }
    }
}
