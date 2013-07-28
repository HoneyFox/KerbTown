/* LICENSE
 * This work is licensed under the Creative Commons Attribution-NoDerivs 3.0 Unported License. 
 * To view a copy of this license, visit http://creativecommons.org/licenses/by-nd/3.0/ or send a letter to Creative Commons, 444 Castro Street, Suite 900, Mountain View, California, 94041, USA.
 */

using System.Collections;
using UnityEngine;

namespace Kerbtown.EEComponents
{
    public class MushroomCave : MonoBehaviour
    {
        private GameObject _plankGameObject;

        private void Start()
        {
            foreach (Transform t in transform)
            {
                // This loop expects children in alphabetical order (Unity Hierarchy) because of the next line.
                if (t.gameObject.name == "Plank")
                {
                    _plankGameObject = t.gameObject;
                    continue;
                }

                if (t.gameObject.name.StartsWith("Trigger_Mush"))
                {
                    t.gameObject.AddComponent<MushroomScript>();
                    continue;
                }

                if (_plankGameObject != null && t.gameObject.name.StartsWith("Trigger_Plank"))
                {
                    var plankScript = t.gameObject.AddComponent<PlankScript>();
                    plankScript.PlankGameObject = _plankGameObject;
                }
            }
        }
    }

    public class MushroomScript : MonoBehaviour
    {
        public string AnimationName = "MushroomAnimation";
        private Animation _animationComponent;
        private bool _isPlaying;

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (otherCollider.name != "capsuleCollider") return;
            if (!HasAnimation()) return;

            StartCoroutine(PlayAnimation(1));
        }

        private IEnumerator PlayAnimation(int direction)
        {
            while (_isPlaying)
                yield return null;

            _isPlaying = true;

            _animationComponent[AnimationName].normalizedTime = direction == 1 ? 0 : 1;
            _animationComponent[AnimationName].speed = direction;
            _animationComponent.Play(AnimationName);

            yield return new WaitForSeconds(_animationComponent[AnimationName].length);

            _isPlaying = false;
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (otherCollider.name != "capsuleCollider") return;
            if (!HasAnimation()) return;

            StartCoroutine(PlayAnimation(-1));
        }

        private bool HasAnimation()
        {
            if (_animationComponent != null)
                return true;

            _animationComponent = GetComponent<Animation>();
            return _animationComponent != null;
        }
    }

    public class PlankScript : MonoBehaviour
    {
        public GameObject PlankGameObject;

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (otherCollider.name != "capsuleCollider") return;

            Rigidbody rb = PlankGameObject.GetComponent<Rigidbody>() ?? PlankGameObject.AddComponent<Rigidbody>();

            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            rb.isKinematic = false; // redundant
            rb.AddForce(Vector3.up*0.01f);

            StartCoroutine(RemoveRigidBodyPhysicsOnSleep(PlankGameObject));

            Destroy(this);
        }

        private IEnumerator RemoveRigidBodyPhysicsOnSleep(GameObject plankGameObject)
        {
            while (!rigidbody.IsSleeping())
            {
                yield return null;
            }

            rigidbody.isKinematic = true;
        }
    }
}