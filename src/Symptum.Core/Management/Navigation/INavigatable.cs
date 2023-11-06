using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Symptum.Core.Management.Navigation
{
    public interface INavigatable
    {
        public Uri Uri { get; set; }

        public bool IsNavigationHandled { get; set; }

        public Type PageType { get; set; }

        public NavigationType NavigationType { get; set; }
    }
}
