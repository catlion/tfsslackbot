using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackBot.Tfs.Model
{
    public class Human
    {
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public Guid Id { get; set; }
        public string ImageUrl { get; set; }
    }
}
