/* LICENSE
 * This source code is copyrighted.
 * All rights reserved.
 * Copyright © Ryan Irecki 2013
 */

using System.Linq;
using UnityEngine;

namespace Kerbtown.NativeModules
{
    public class GenericLadder : MonoBehaviour
    {
        public string ObjectName = "";

        public void Setup()
        {
            foreach (var obj in gameObject.GetComponentsInChildren<Transform>().Where(o => o.name == ObjectName))
            {
                if (!obj.collider.isTrigger)
                {
                    Extensions.LogWarning("Setting current collider as a trigger.");

                    obj.collider.isTrigger = true;
                }

                obj.tag = "Ladder";
            }

            Destroy(this);
        }
    }
}