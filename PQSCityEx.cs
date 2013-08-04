/* LICENSE
 * This work is licensed under the Creative Commons Attribution-NoDerivs 3.0 Unported License. 
 * To view a copy of this license, visit http://creativecommons.org/licenses/by-nd/3.0/ or send a letter to Creative Commons, 444 Castro Street, Suite 900, Mountain View, California, 94041, USA.
 */

using UnityEngine;

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

            //float currentDistance = Vector3.Distance(sphere.target.transform.position, transform.position);
            //for (int i = lod.Length - 1; i >= 0; i--)
            //{
            //    lod[i].SetActive(currentDistance < lod[i].visibleRange);
            //}
            
            base.OnUpdateFinished();
        }

        public new void OnSphereReset()
        {
            if (!modEnabled) return;

            base.OnSphereReset();
        }
    }
}