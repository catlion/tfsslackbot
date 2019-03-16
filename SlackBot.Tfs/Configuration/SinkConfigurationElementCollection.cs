using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackBot.Tfs.Configuration
{
    /// <summary>
    /// Represent a collection of <see cref="SinkConfigurationElement"/> objects.
    /// </summary>
    public class SinkConfigurationElementCollection : ConfigurationElementCollection, IEnumerable<SinkConfigurationElement>
    {
        /// <summary>
        /// Gets the <see cref="SinkConfigurationElement"/> with the specified name.
        /// </summary>
        /// <value>
        /// The <see cref="SinkConfigurationElement"/>.
        /// </value>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException"></exception>
        public new SinkConfigurationElement this[string name]
        {
            get
            {
                var e = this.FirstOrDefault(x => x.Name == name);
                if (e == null) throw new KeyNotFoundException();
                return e;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SinkConfigurationElementCollection"/> class.
        /// </summary>
        public SinkConfigurationElementCollection()
        {

        }

        /// <summary>
        /// Creates a new <see cref="SinkConfigurationElement" />.
        /// </summary>
        /// <returns>
        /// A new <see cref="SinkConfigurationElement" />.
        /// </returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new SinkConfigurationElement();
        }

        /// <summary>
        /// Gets the element key for a specified configuration element.
        /// </summary>
        /// <param name="element">The <see cref="SinkConfigurationElement" /> to return the key for.</param>
        /// <returns>
        /// An <see cref="T:System.Object" /> that acts as the key for the specified <see cref="SinkConfigurationElement" />.
        /// </returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SinkConfigurationElement)element).Name;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public new IEnumerator<SinkConfigurationElement> GetEnumerator()
        {
            return ((System.Collections.IEnumerable)this).OfType<SinkConfigurationElement>().GetEnumerator();
        }
    }
}
