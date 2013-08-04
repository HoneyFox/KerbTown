/* LICENSE
 * This work is licensed under the Creative Commons Attribution-NoDerivs 3.0 Unported License. 
 * To view a copy of this license, visit http://creativecommons.org/licenses/by-nd/3.0/ or send a letter to Creative Commons, 444 Castro Street, Suite 900, Mountain View, California, 94041, USA.
 */

using UnityEngine;

namespace Kerbtown
{
    public partial class KerbTown
    {
        public float CameraDistance = 50.0f;
        private float _cameraX;
        private float _cameraY;

        // Rather than adding a script to the FlightCamera, we'll just control it natively from KerbTown.
        private void LateUpdate()
        {
            if (!KtCamera.ManualControl)
                return;

            if (Input.GetMouseButton(1))    // RMB
            {
                _cameraX += Input.GetAxis("Mouse X")*KtCamera.CameraSpeed;
                _cameraY -= Input.GetAxis("Mouse Y")*KtCamera.CameraSpeed;
            }

            if (GameSettings.AXIS_MOUSEWHEEL.GetAxis() != 0f)   // MMB
            {
                CameraDistance =
                    Mathf.Clamp(
                        CameraDistance*
                        (1f - (GameSettings.AXIS_MOUSEWHEEL.GetAxis()*KtCamera.ActiveFlightCamera.zoomScaleFactor)),
                        KtCamera.ActiveFlightCamera.minDistance, KtCamera.ActiveFlightCamera.maxDistance);
            }

            if (Mouse.Right.GetDoubleClick())
            {
                _cameraX = 0;
                _cameraY = 0;
            }

            KtCamera.ActiveCameraPivot.transform.rotation = KtCamera.ActiveCameraPivot.transform.parent.rotation;
            KtCamera.ActiveCameraPivot.transform.RotateAround(KtCamera.ActiveCameraPivot.transform.up, _cameraX);
            KtCamera.ActiveCameraPivot.transform.RotateAround(KtCamera.ActiveCameraPivot.transform.right, _cameraY);

            KtCamera.ActiveFlightCamera.transform.localPosition =
                Vector3.Lerp(
                    KtCamera.ActiveFlightCamera.transform.localPosition,
                    Vector3.back*CameraDistance,
                    KtCamera.ActiveFlightCamera.sharpness*Time.deltaTime);

            KtCamera.ActiveFlightCamera.transform.localRotation =
                Quaternion.LookRotation(-KtCamera.ActiveFlightCamera.transform.localPosition, Vector3.up);
        }
    }

    public static class KtCamera
    {
        private static Transform _originalParentTransform;
        private static bool _manualControl;

        public static FlightCamera ActiveFlightCamera;
        public static GameObject ActiveCameraPivot;

        public static float CameraSpeed = 0f;
        //public static Transform Target;

        public static bool ManualControl
        {
            set
            {
                if (value && ActiveFlightCamera == null)
                {
                    _manualControl = false;
                    Extensions.LogError("Tried to set manual camera control while FlightCamera.fetch was null.");
                    return;
                }
                _manualControl = value;
            }
            get { return _manualControl; }
        }

        public static void SetCameraParent(Transform parentTransform)
        {
            // Assign FlightCamera instance to public var.
            ActiveFlightCamera = FlightCamera.fetch;

            // For replacing the camera when done editing.
            if (_originalParentTransform == null)
                _originalParentTransform = ActiveFlightCamera.transform.parent;

            // For translating the camera
            if (ActiveCameraPivot != null) Object.Destroy(ActiveCameraPivot);
            ActiveCameraPivot = new GameObject("KtCamPivot");
            ActiveCameraPivot.transform.parent = parentTransform;
            ActiveCameraPivot.transform.localPosition = Vector3.zero;
            ActiveCameraPivot.transform.localRotation = Quaternion.identity;

            // Switch to active object.
            ActiveFlightCamera.transform.parent = ActiveCameraPivot.transform;

            // Use the FlightCamera sensitivity for the speed.
            CameraSpeed = ActiveFlightCamera.orbitSensitivity;

            // Take control of the flight camera.
            ActiveFlightCamera.DeactivateUpdate();

            // Instruct LateUpdate that we're controlling the camera manually now.
            ManualControl = true;

            // Say something.
            Extensions.PostScreenMessage("[KerbTown] FlightCamera switched to: " + parentTransform.name);
        }

        public static void RestoreCameraParent()
        {
            // Only execute if we're actually controlling the camera.
            if (!ManualControl) return;

            // Restore camera control to vessel.
            FlightCamera.fetch.transform.parent = _originalParentTransform;
            _originalParentTransform = null;

            ManualControl = false;

            ActiveFlightCamera.ActivateUpdate();

            // Say something.
            Extensions.PostScreenMessage("[KerbTown] FlightCamera restored to vessel.");
        }
    }
}