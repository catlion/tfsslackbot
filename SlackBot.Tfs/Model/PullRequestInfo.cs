using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackBot.Tfs.Model
{
    public class PullRequestInfo
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Uri RepositoryUri { get; set; }
        public Human CreatedBy { get; set; }
    }
}
