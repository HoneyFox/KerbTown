using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kerbtown.EEComponents
{
    public class KerbalSeatEx : KerbalSeat
    {
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (Occupant != null)
            {
                
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
        }
    }
}
