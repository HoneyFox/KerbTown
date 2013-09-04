/* LICENSE
 * This source code is copyrighted.
 * All rights reserved.
 * Copyright © Ryan Irecki 2013
 */

using System.Collections;
using System.Linq;
using UnityEngine;

namespace Kerbtown.NativeModules
{
    public class GenericAnimation : MonoBehaviour
    {
        public string AnimationName = "";
        public float AnimationSpeed = 1f;
        public string ClassName = "";
        public bool HighlightOnHover = true;
        public string ObjectName = "";

        public void Setup()
        {
            Animation animationComponent = (from animationList in gameObject.GetComponentsInChildren<Animation>()
                where animationList != null
                from AnimationState animationState in animationList
                where animationState.name == AnimationName
                select animationList).FirstOrDefault();

            if (animationComponent == null || animationComponent[AnimationName] == null)
            {
                Extensions.LogError("Animation '" + AnimationName + "' could not be found.");
                return;
            }

            GameObject gobj = (from t in gameObject.GetComponentsInChildren<Transform>()
                where t.gameObject != null && t.gameObject.name == ObjectName
                select t.gameObject).FirstOrDefault();

            if (gobj == null)
            {
                Extensions.LogError("GameObject '" + ObjectName + "' could not be found.");
                return;
            }


            switch (ClassName)
            {
                case "AnimateOnClick":
                    var clickComponent = gobj.AddComponent<AnimateOnClick>();
                    clickComponent.AnimationComponent = animationComponent;
                    clickComponent.AnimationName = AnimationName;
                    clickComponent.HighlightOnMouseOver = HighlightOnHover;

                    break;

                case "AnimateOnCollision":
                    if (!gobj.collider.isTrigger)
                    {
                        Extensions.LogWarning("The collider on '" + gobj.name +
                                              "' has had it's property isTrigger set to true.");
                        gobj.collider.isTrigger = true;
                    }

                    var collisionComponent = gobj.AddComponent<AnimateOnCollision>();
                    collisionComponent.AnimationComponent = animationComponent;
                    collisionComponent.AnimationName = AnimationName;
                    break;
            }
        }
    }

    public class AnimateOnClick : MonoBehaviour
    {
        public Animation AnimationComponent;
        public string AnimationName;
        public bool HighlightOnMouseOver = true;
        private bool _animationPlaying;
        private bool _atStart = true;

        private void OnMouseDown()
        {
            if (!_animationPlaying)
                StartCoroutine(PlayAnimation());
        }

        private IEnumerator PlayAnimation()
        {
            _animationPlaying = true;

            AnimationComponent[AnimationName].speed = _atStart ? 1 : -1;
            AnimationComponent[AnimationName].normalizedTime = _atStart ? 0 : 1;

            AnimationComponent.Play(AnimationName);

            yield return new WaitForSeconds(AnimationComponent[AnimationName].length);

            _atStart = !_atStart;

            _animationPlaying = false;
        }

        private void OnMouseEnter()
        {
            if (HighlightOnMouseOver)
                Highlight(gameObject, true);
        }

        private void OnMouseExit()
        {
            if (HighlightOnMouseOver)
                Highlight(gameObject, false);
        }

        public void Highlight(GameObject sgameObject, bool highlightActive)
        {
            if (sgameObject == null || sgameObject.renderer == null)
                return;

            sgameObject.renderer.material.SetFloat("_RimFalloff", 2.5f);
            sgameObject.renderer.material.SetColor("_RimColor", highlightActive ? Color.green : Color.clear);
        }
    }

    public class AnimateOnCollision : MonoBehaviour
    {
        public Animation AnimationComponent;
        public string AnimationName;
        private bool _animationPlaying;
        private bool _atStart = true;

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (otherCollider.name == "DestructionCollider") return;

            if (!_animationPlaying)
                StartCoroutine(PlayAnimation());
        }

        private IEnumerator PlayAnimation()
        {
            _animationPlaying = true;

            AnimationComponent[AnimationName].speed = _atStart ? 1 : -1;
            AnimationComponent[AnimationName].normalizedTime = _atStart ? 0 : 1;

            AnimationComponent.Play(AnimationName);

            yield return new WaitForSeconds(AnimationComponent[AnimationName].length);

            _atStart = !_atStart;

            _animationPlaying = false;
        }
    }

    public class TimeOfDayController : MonoBehaviour
    {
        // I believe the TimeOfDayAnimation component would probably do similar to what I've written ..
        // .. but it has no public variables to specify an animation component (for future plans).
        // So I wrote a new one. :)

        public Transform SunTransform;
        public Animation EmissiveAnimation;
        private string _clipName = "";
        private float _dot;

        void Start()
        {
            // If no sun transform is predefined, we use the default 'Sun'.
            if (SunTransform == null && Sun.Instance != null)
                SunTransform = Sun.Instance.transform;

            if (EmissiveAnimation == null)
            {
                EmissiveAnimation = GetComponentInChildren<Animation>(); //animation
                if (EmissiveAnimation == null)
                {
                    Extensions.LogWarning("Could not obtain an animation component for the TimeOfDayController component.");
                    return;
                }
            }

            _clipName = EmissiveAnimation.clip.name;
            
            EmissiveAnimation.Stop();

        }
        private void Update()
        {
            if (SunTransform == null || EmissiveAnimation == null)
            {
                DestroyImmediate(this);
                return;
            }

            _dot = 0.5f - ((Vector3.Dot(SunTransform.forward, -transform.up) + 1f) * 0.25f);
            
            EmissiveAnimation[_clipName].enabled = true;
            EmissiveAnimation[_clipName].normalizedTime = _dot;
            EmissiveAnimation.Sample();

            EmissiveAnimation[_clipName].enabled = false;
        }
    }
}