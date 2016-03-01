using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace SlackBot.Tfs
{
    /// <summary>
    /// Represents the TFS chat message sink.
    /// </summary>
    public class TfsChatMessageSink : IChatMessageSink
    {
        private static Dictionary<string, string> _colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Bug", "#F35A00" },
            { "Task", "#005AF3" },
            { "User Story", "#5A00F3" },
        };

        private static readonly Regex Pattern = new Regex("(^|\\b)tfs(?<id>[0-9]+)", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

        private WorkItemTrackingHttpClient _witClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TfsChatMessageSink"/> class.
        /// </summary>
        public TfsChatMessageSink()
        {

        }

        /// <summary>
        /// Asynchronously initializes the sink.
        /// </summary>
        /// <param name="name">The configuration name of the sink.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>
        /// A <see cref="Task" /> that represents the asynchronous initialize operation.
        /// </returns>
        public async Task InitializeAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            await Task.Delay(0);

            var element = Configuration.TfsConfigurationSection.Current.Sinks[name];

            // todo: support other auth methods, see the auth samples in https://www.visualstudio.com/en-us/integrate/get-started/client-libraries/samples
            // * OAuth
            // * ADD
            //var vssCredentials = new VssCredentials(); // Active directory auth - NTLM against a Team Foundation Server
            //var vssCredentials = new VssClientCredentials(); // Visual Studio sign-in prompt. Would need work to make this not prompt at every startup
            var vssCredentials = new VssBasicCredential("", element.AccessToken);
            var connection = new VssConnection(new Uri(element.ProjectCollection), vssCredentials);
            _witClient = connection.GetClient<WorkItemTrackingHttpClient>();
        }

        /// <summary>
        /// Asynchronously processes a message.
        /// </summary>
        /// <param name="slack">The slack.</param>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>
        /// A <see cref="Task{ChatMessageSinkResult}" /> that represents the asynchronous process operation.
        /// </returns>
        public async Task<ChatMessageSinkResult> ProcessMessageAsync(ISlack slack, Message message, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_witClient == null)
            {
                throw new InvalidOperationException("WorkItem Client is null");
            }

            await Task.Delay(0);

            var attachments = new List<Attachment>();
            var matches = Pattern.Matches(message.Text);
            foreach (var id in matches.OfType<Match>().Where(x => x.Success).Select(x => x.Groups["id"]).Where(x => x.Success).Select(x => x.Value))
            {
                WorkItem wi;
                try
                {
                    // todo: get all at once
                    wi = await _witClient.GetWorkItemAsync(XmlConvert.ToInt32(id));
                }
                catch (VssServiceException ex)
                {
                    // Ignore items that don't exist
                    // "TF401232: Work item 1 does not exist, or you do not have permissions to read it."
                    if (ex.Message.StartsWith("TF401232:"))
                    {
                        await slack.SendAsync(message.CreateReply(string.Format("WorkItem {0} not found", id)));
                        return ChatMessageSinkResult.Complete;
                    }
                    throw;
                }

                attachments.Add(WorkItemToAttachment(wi));
            }

            if (attachments.Any())
            {
                await slack.SendAsync(message.CreateReply("", attachments));
                return ChatMessageSinkResult.Complete;
            }

            return ChatMessageSinkResult.Continue;
        }

        private static Attachment WorkItemToAttachment(WorkItem wi)
        {
            var fields = new List<AttachmentField>();
            //AddField(fields, wi, "System.AssignedTo", "Assigned To"); // not available in the response
            AddField(fields, wi, "System.State", "State");

            //var apiUrl = wi.Url; // useful for seeing the raw json
            var wiType = GetField(wi, "System.WorkItemType");
            var wiTitle = GetField(wi, "System.Title");
            var link = GetLink(wi, "html");

            string color;
            _colors.TryGetValue(wiType, out color);

            var attachment = new Attachment(string.Format("{0} {1}: {2}", wiType, wi.Id, link), color,
                text: string.Format("<{0}|{1} {2} {3}>", SlackEscape(link), SlackEscape(wiType), wi.Id, SlackEscape(wiTitle)),
                fields: fields);
            return attachment;
        }

        private static string GetLink(WorkItem wi, string linkName)
        {
            if (!wi.Links.Links.ContainsKey(linkName))
            {
                throw new Exception(string.Format("Link of type '{0}' not returned from TFS", linkName));
            }
            var linkObj = wi.Links.Links[linkName];
            var referenceLink = linkObj as ReferenceLink;
            if (referenceLink == null)
            {
                throw new Exception(string.Format("Failed to case Link '{0}' to type ReferenceLink", linkName));
            }
            return referenceLink.Href;
        }

        private static void AddField(List<AttachmentField> fields, WorkItem wi, string name, string displayName, bool isShort = true)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }
            var value = GetField(wi, name);
            fields.Add(new AttachmentField(displayName, value, isShort));
        }

        private static string GetField(WorkItem wi, string name)
        {
            if (!wi.Fields.ContainsKey(name))
            {
                throw new Exception(string.Format("Field '{0}' was not returned by TFS", name));
            }
            return wi.Fields[name].ToString();
        }

        private static string SlackEscape(string val)
        {
            if (string.IsNullOrEmpty(val)) return val;

            var result = new StringBuilder(val.Length);
            for (var i = 0; i < val.Length; i++)
            {
                var c = val[i];
                switch (c)
                {
                    case '&': result.Append("&amp;"); break;
                    case '<': result.Append("&lt;"); break;
                    case '>': result.Append("&gt;"); break;
                    default: result.Append(c); break;
                }
            }

            return result.ToString();
        }
    }
}
