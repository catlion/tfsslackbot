using Newtonsoft.Json;
using SlackBot.Tfs.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SlackBot.Tfs
{
    public class PullRequestsMonitor
    {
        private readonly string apiUrl;
        private readonly HttpClient httpClient;
        private DateTimeOffset lastUpdate;

        public PullRequestsMonitor(string tfsConfigName)
        {
            var element = Configuration.TfsConfigurationSection.Current.Sinks[tfsConfigName];
            apiUrl = $"{element.ProjectCollection}/{element.Project}/_apis/git/pullrequests?" +
                $"searchCriteria.includeLinks=true&" +
                $"searchCriteria.status='active'&api-version=3.0-preview";

            var cred = new NetworkCredential(element.Username, element.Password);
            httpClient = HttpClientFactory.Create(new HttpClientHandler {
                Credentials = cred,
                PreAuthenticate = true
            });
        }

        public async Task<List<PullRequestInfo>> LoadAsync()
        {
            var str = await httpClient.GetStringAsync(apiUrl).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<List<PullRequestInfo>>(str);
        }
    }
}
