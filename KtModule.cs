/* LICENSE
 * This source code is copyrighted.
 * All rights reserved.
 * Copyright © Ryan Irecki 2013
 */

using UnityEngine;

namespace Kerbtown
{
    public class KtComponent
    {
        public readonly MonoBehaviour ModuleComponent;
        public bool KeepAlive = false;

        public KtComponent(MonoBehaviour moduleComponent)
        {
            ModuleComponent = moduleComponent;
        }
    }
}