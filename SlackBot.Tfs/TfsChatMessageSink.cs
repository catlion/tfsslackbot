using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

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

        private TfsTeamProjectCollection _teamProjectCollection;
        private Guid _teamProjectCollectionGuid;
        private TeamFoundationServer _tfs;
        private WorkItemStore _wis;
        private TswaClientHyperlinkService _tswaHyperlink;

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
        /// <exception cref="System.NotImplementedException"></exception>
        public async Task InitializeAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            await Task.Delay(0);

            var element = Configuration.TfsConfigurationSection.Current.Sinks[name];
            _teamProjectCollection = new TfsTeamProjectCollection(new Uri(element.ProjectCollection));
            _teamProjectCollectionGuid = _teamProjectCollection.InstanceId;
            _teamProjectCollection.Connect(ConnectOptions.IncludeServices);
            _wis = _teamProjectCollection.GetService<WorkItemStore>();
            if (_wis == null)
            {
                throw new Exception("_teamProjectCollection.GetService<WorkItemStore>() returned null");
            }

            _tswaHyperlink = _teamProjectCollection.GetService<TswaClientHyperlinkService>();
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
        /// <exception cref="System.NotImplementedException"></exception>
        public async Task<ChatMessageSinkResult> ProcessMessageAsync(ISlack slack, Message message, CancellationToken cancellationToken = default(CancellationToken))
        {
            await Task.Delay(0);

            if (_wis == null)
            {
                throw new InvalidOperationException("WorkItemStore is null");
            }
            var attachments = new List<Attachment>();
            var matches = Pattern.Matches(message.Text);
            foreach (var id in matches.OfType<Match>().Where(x => x.Success).Select(x => x.Groups["id"]).Where(x => x.Success).Select(x => x.Value))
            {
                WorkItem wi;
                try
                {
                    wi = _wis.GetWorkItem(XmlConvert.ToInt32(id));

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

                var link = _tswaHyperlink.GetWorkItemEditorUrl(wi.Id);

                var fields = new List<AttachmentField>()
                    {
                        new AttachmentField("Assigned To", Convert.ToString(wi[CoreField.AssignedTo]), true),
                        new AttachmentField("State", wi.State, true),
                    };

                string color;
                _colors.TryGetValue(wi.Type.Name, out color);

                attachments.Add(new Attachment(string.Format("{0} {1}: {2}", wi.Type, wi.Id, link), color,
                    text: string.Format("<{0}|{1} {2} {3}>", SlackEscape(link.ToString()), SlackEscape(wi.Type.Name), id, SlackEscape(wi.Title)),
                    fields: fields));
            }

            if (attachments.Any())
            {
                await slack.SendAsync(message.CreateReply("", attachments));
                return ChatMessageSinkResult.Complete;
            }

            return ChatMessageSinkResult.Continue;
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
