/* LICENSE
 * This work is licensed under the Creative Commons Attribution-NoDerivs 3.0 Unported License. 
 * To view a copy of this license, visit http://creativecommons.org/licenses/by-nd/3.0/ or send a letter to Creative Commons, 444 Castro Street, Suite 900, Mountain View, California, 94041, USA.
 */

using UnityEngine;

namespace Kerbtown
{
    class PQSCityEx : PQSCity
    {
        public new void OnSphereActive()
        {
            Debug.LogWarning("OSA");
            base.OnSphereActive();
        }

        public new void OnSphereInactive()
        {
            Debug.LogWarning("OSI");
            base.OnSphereInactive();
        }
    }
}
