/* LICENSE
 * This source code is copyrighted.
 * All rights reserved.
 * Copyright © Ryan Irecki 2013
 */

using System.Linq;
using UnityEngine;

namespace Kerbtown.EEComponents
{
    public class DistortionScript : MonoBehaviour
    {
        public bool AffectsIntensity = true;
        public float Amplitude = 1.0f;

        private bool _animateDistortion;
        private Material _bTextMat;

        // Keep a copy of the original values
        private Color _originalColorMat;

        private void Start()
        {
            Transform bTextTransform =
                GetComponentsInChildren<Transform>().FirstOrDefault(o => o.gameObject.name == "bText");

            if (bTextTransform == null)
            {
                Extensions.LogError("No bTextTransform");
                Destroy(this);
                return;
            }

            _bTextMat = bTextTransform.gameObject.renderer.material;
            _originalColorMat = _bTextMat.color;

            _animateDistortion = true;
        }

        private void LateUpdate()
        {
            // If we should turn off animation, and the visual state hasn't been reset ..
            if (!_animateDistortion)
                return;

            if (Random.value < 0.95f) // Flicker chance = (1.0-right assigned value*100)%
                return;

            float flickerValue = (1f - (Random.value*2f))*Amplitude;

            _bTextMat.color = _originalColorMat*flickerValue;
        }
    }
}