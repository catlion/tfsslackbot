﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackBot.Tfs.Configuration
{
    /// <summary>
    /// Represents configuration about a message sink.
    /// </summary>
    public class SinkConfigurationElement : ConfigurationElement
    {
        /// <summary>
        /// Gets the name of the sink.
        /// </summary>
        /// <value>
        /// The name of the sink.
        /// </value>
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)base["name"]; }
        }

        /// <summary>
        /// Gets the project collection URL.
        /// </summary>
        /// <value>
        /// The project collection URL.
        /// </value>
        [ConfigurationProperty("projectCollection", IsRequired = true)]
        public string ProjectCollection
        {
            get { return (string)base["projectCollection"]; }
        }

        /// <summary>
        /// Gets the project name.
        /// </summary>
        /// <value>
        /// The project name.
        /// </value>
        [ConfigurationProperty("project", IsRequired = true)]
        public string Project
        {
            get { return (string)base["project"]; }
        }

        [ConfigurationProperty("accessToken", IsRequired = true)]
        public string AccessToken
        {
            get { return (string)base["accessToken"]; }
        }

        [ConfigurationProperty("loginMethod", IsRequired = true)]
        public int LoginMethod
        {
            get { return (int)base["loginMethod"]; }
        }

        [ConfigurationProperty("username", IsRequired = true)]
        public string Username
        {
            get { return (string)base["username"]; }
        }

        [ConfigurationProperty("password", IsRequired = true)]
        public string Password
        {
            get { return (string)base["password"]; }
        }

        [ConfigurationProperty("searchString", IsRequired = true)]
        public string SearchString
        {
            get { return (string)base["searchString"]; }
        }

        [ConfigurationProperty("searchPeriodDays", IsRequired = false)]
        public int SearchPeriodDays
        {
            get
            {
                var str = (string)base["searchPeriodDays"] ?? string.Empty;
                return int.TryParse(str, out var val)
                    ? val
                    : 30;
            }
        }
    }
}
