//------------------------------------------ START OF LICENSE -----------------------------------------
//Azure Usage and Billing Insights
//
//Copyright(c) Microsoft Corporation
//
//All rights reserved.
//
//MIT License
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
//associated documentation files (the ""Software""), to deal in the Software without restriction, 
//including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
//subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial 
//portions of the Software.
//
//THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
//BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
//OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
//CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------- END OF LICENSE ------------------------------------------

using Commons;
using Microsoft.Azure.WebJobs;
using System;
using System.Diagnostics;

namespace WebJobBillingData
{
	// To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
	// https://azure.microsoft.com/en-us/documentation/articles/billing-usage-rate-card-overview/
	// https://github.com/Azure/BillingCodeSamples

	class Program
	{
		// Please set the following connection strings in app.config for this WebJob to run:
		// AzureWebJobsDashboard and AzureWebJobsStorage
		static void Main()
		{
			if (Environment.UserInteractive) {
				// test only
				DateTime endDate = DateTime.UtcNow.Date;
				DateTime startDate = endDate.AddDays(-10);
				Guid subscriptionId = new Guid("[put subscription id here]");
				Guid organizationId = new Guid("[put organization id here]");
				BillingRequest br = new BillingRequest(subscriptionId, organizationId, startDate, endDate);
				Functions.ProcessQueueMessage(br);
				Console.WriteLine("Press any key");
				Console.ReadLine();
			} else {
				Trace.TraceInformation("*************************************************************************");
				Trace.TraceInformation($"{nameof(WebJobBillingData)}:{nameof(Main)} starting. DateTime UTC: {DateTime.UtcNow}");

				JobHostConfiguration config = new JobHostConfiguration();
				config.Queues.BatchSize = 3;
				config.Queues.MaxDequeueCount = 3;
				config.Queues.MaxPollingInterval = TimeSpan.FromSeconds(15);

				var host = new JobHost(config);
				// The following code ensures that the WebJob will be running continuously
				host.RunAndBlock();
			}
		}
	}
}
