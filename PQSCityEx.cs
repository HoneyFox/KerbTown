/* LICENSE
 * This source code is copyrighted.
 * All rights reserved.
 * Copyright © Ryan Irecki 2013
 */

using UnityEngine;

namespace Kerbtown
{
    internal class PQSCityEx : PQSCity
    {
        public StaticObject StaticObjectRef;

        void OnEnable()
        {
            // Repeat this action once every second, rather than every frame in OnUpdateFinished().
            InvokeRepeating("CalcVisibilityActivity", 0, 1);
        }

        void OnDisable()
        {
            CancelInvoke("CalcVisibilityActivity");
        }

        public void CalcVisibilityActivity()
        {
            // TODO Decide on implementation and remove this code.
            if (StaticObjectRef == null || StaticObjectRef.ModuleList == null) return;
            
            foreach (var mod in StaticObjectRef.ModuleList)
            {
                if (mod.KeepAlive) continue;

                mod.ModuleComponent.enabled =
                    (Vector3.Distance(sphere.target.transform.position, transform.position) <
                     StaticObjectRef.VisRange);

                //print(mod.ModuleComponent.name + ", " + mod.ModuleComponent.enabled);
            }
        }

        public new void OnSphereActive()
        {
            if (!modEnabled) return;

            base.OnSphereActive();
        }

        public new void OnSphereInactive()
        {
            if (!modEnabled) 
                return;

            // Do not disable if the player has decided this is a permanent object.
            if (StaticObjectRef != null && StaticObjectRef.IsSpaceActive) return;

            base.OnSphereInactive();
        }

        public new void OnSphereReset()
        {
            if (!modEnabled) return;

            base.OnSphereReset();
        }

        public new void OnSphereStart()
        {
            if (!modEnabled) return;

            base.OnSphereStart();
        }
        
        public new void OnUpdateFinished()
        {
            if (!modEnabled)
                return;

            base.OnUpdateFinished();
        }
    }
}