// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.AI.Runtime.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

// This auto-registers the AI extension when a webjobs/functions host starts up.
[assembly: WebJobsStartup(typeof(AgentsWebJobsStartup))]

namespace Azure.AI.Runtime.Extensions;

class AgentsWebJobsStartup : IWebJobsStartup2
{
    public void Configure(WebJobsBuilderContext context, IWebJobsBuilder builder)
    {
        this.Configure(builder);
    }

    public void Configure(IWebJobsBuilder builder)
    {
        builder.AddAgentsBindings();
    }
}