using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSaliens.Presence
{
    internal interface IPresenceUpdateTrigger
    {
        void SetPresence(IPresence presence);
    }
}
