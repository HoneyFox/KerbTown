/* LICENSE
 * This source code is copyrighted.
 * All rights reserved.
 * Copyright © Ryan Irecki 2013
 */

using UnityEngine;

namespace Kerbtown
{
    public class StaticObjectModule : MonoBehaviour
    {
        public StaticObject StaticObjectRef;

        public virtual void OnSave(Game gameData)
        {
        }

        public virtual void OnLoad(Game gameData)
        {
        }

        public virtual void OnUnload()
        {
        }
    }
}