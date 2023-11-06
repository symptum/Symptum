using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symptum.Core.Management.Deployment
{
    public interface IPackage
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public Version Version { get; set; }

        public string Authors { get; set; }

        public object Content { get; set; }

        public string Dependencies { get; set; }

        public IList<string> Tags { get; set; }
    }
}
