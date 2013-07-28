/* LICENSE
 * This work is licensed under the Creative Commons Attribution-NoDerivs 3.0 Unported License. 
 * To view a copy of this license, visit http://creativecommons.org/licenses/by-nd/3.0/ or send a letter to Creative Commons, 444 Castro Street, Suite 900, Mountain View, California, 94041, USA.
 */

namespace Kerbtown
{
    internal class PQSCityEx : PQSCity
    {
        public new void OnSphereActive()
        {
            if (!modEnabled) return;

            base.OnSphereActive();
        }

        public new void OnSphereInactive()
        {
            if (!modEnabled) return;

            base.OnSphereInactive();
        }

        public new void OnUpdateFinished()
        {
            if (!modEnabled) return;

            base.OnUpdateFinished();
        }

        public new void OnSphereReset()
        {
            if (!modEnabled) return;

            base.OnSphereReset();
        }
    }
}