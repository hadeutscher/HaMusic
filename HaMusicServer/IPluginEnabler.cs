using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaMusicServer
{
    public interface IPluginEnabler
    {
        string Name { get; }
        void Enable(string[] args);
    }
}
