/* LICENSE
 * This source code is copyrighted.
 * All rights reserved.
 * Copyright © Ryan Irecki 2013
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Kerbtown
{
    public partial class KerbTown
    {
        #region Window Properties

        #region Window Rects

        private Rect _assetListRect = new Rect(460, 40, 600, 370);
        private Rect _currAssetListRect = new Rect(480, 60, 600, 370);

        private Rect _lsNameWindowRect = new Rect(Screen.width/2 - 150, Screen.height/2 - 44, 300, 88);
        private Rect _mainWindowRect = new Rect(20, 20, 410, 400);

        #endregion

        #region Visibility Flags

        private bool _availAssetsVisible = true;
        private bool _currAssetsVisible;
        private bool _lsNameVisible;
        private bool _mainWindowVisible;

        #endregion

        private Vector2 _availAssetScrollPos;
        private Vector2 _currAssetScrollPos;
        private string _currentObjectID = "";

        #endregion

        private string _currentLaunchSiteName = "";
        private bool _deletePersistence;
        private GUISkin _mySkin;
        private string _rPosition = "";
        private bool _showSavedLabel;
        private int _speedMultiplier = 1;
        private string _visRange = "";

        private string _xPosition = "";
        private string _yPosition = "";
        private string _zPosition = "";
        //private string _xScale = "";
        //private string _yScale = "";
        //private string _zScale = "";

        private void Start()
        {
            //_mySkin = AssetBase.GetGUISkin("KSP window 5");
        }

        private void OnGUI()
        {
            if (!_mainWindowVisible)
                return;

            GUI.skin = _mySkin;

            // Main Window
            _mainWindowRect = GUI.Window(0x8100, _mainWindowRect, DrawMainWindow, "KerbTown Editor");

            // Selected Object Window
            GUI.Window(0x8101,
                new Rect(_mainWindowRect.x + 5, _mainWindowRect.y + _mainWindowRect.height + 5, 400,
                    _currentSelectedObject != null ? 150 : 50), DrawSelectedWindow, "Selected Object Information");

            // Asset List window
            if (_availAssetsVisible)
                _assetListRect = GUI.Window(0x8102, _assetListRect, DrawAvailAssetWindow, "Available Static Assets List");

            // Current objects window
            if (_currAssetsVisible)
            {
                _currAssetListRect = GUI.Window(0x8103, _currAssetListRect, DrawCurrAssetWindow,
                    "Existing Static Assets List");
            }

            if (_lsNameVisible)
            {
                _lsNameWindowRect = GUI.Window(0x8104, _lsNameWindowRect, DrawLSNamingWindow,
                    "Enter the name of your Launch Site");
                //!isLaunchSite
            }
        }

        private void DrawLSNamingWindow(int windowID)
        {
            _currentLaunchSiteName = GUI.TextField(new Rect(10, 25, 280, 22), _currentLaunchSiteName);

            GUI.backgroundColor = Color.green;
            if (GUI.Button(new Rect(10, 55, 80, 22), "Done"))
            {
                if (_currentLaunchSiteName.Trim().Length > 0 &&
                    (_currentLaunchSiteName != "LaunchPad" && _currentLaunchSiteName != "Runway"))
                {
					_currentSelectedObject.MakeLaunchSite(true, _currentLaunchSiteName, _currentSelectedObject.LaunchPadTransform);
                    _lsNameVisible = false;
                }
                else
                    Extensions.LogError("Invalid Launch Site name.");
            }

            GUI.backgroundColor = Color.red;
            if (GUI.Button(new Rect(100, 55, 80, 22), "Cancel"))
            {
                _currentLaunchSiteName = "";
                _lsNameVisible = false;
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void DrawMainWindow(int windowID)
        {
            GUI.Label(new Rect(10, 20, 100, 25), "Asset Lists");

            if (GUI.Button(new Rect(20, 40, 180, 25), _availAssetsVisible ? "Hide Available" : "Show Available"))
                _availAssetsVisible = !_availAssetsVisible;

            if (GUI.Button(new Rect(210, 40, 180, 25), _currAssetsVisible ? "Hide Existing" : "Show Existing"))
                _currAssetsVisible = !_currAssetsVisible;

            GUI.Label(new Rect(10, 70, 100, 25), "Functions");

            if (GUI.Button(new Rect(20, 90, 120, 25), "Save Session"))
            {
                SaveInstances();
                if (!_showSavedLabel)
                {
                    _showSavedLabel = true;
                    StartCoroutine(HideStatus());
                }
            }

            if (GUI.Button(new Rect(210, 90, 180, 25),
                _deletePersistence ? "Delete Persistence" : "Write to Persistence"))
            {
                WritePersistence(_deletePersistence);
                if (!_showSavedLabel)
                {
                    _showSavedLabel = true;
                    StartCoroutine(HideStatus());
                }
            }

            if (_showSavedLabel)
                GUI.Label(new Rect(150, 90, 200, 25), "Saved.");

            if (_currentSelectedObject != null)
            {
                DrawPositioningControls();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private IEnumerator HideStatus()
        {
            yield return new WaitForSeconds(1);
            _showSavedLabel = false;
        }

        private void DrawPositioningControls()
        {
            #region Launch Site

            bool isLaunchSite = (_currentSelectedObject.LaunchSiteName != "");

            GUI.backgroundColor = !isLaunchSite ? XKCDColors.Orange : XKCDColors.OrangeRed;
            if (GUI.Button(new Rect(210, 120, 180, 25), isLaunchSite ? "Unmark as Launch Site" : "Mark as Launch Site"))
            {
                if (isLaunchSite)
                {
                    _currentSelectedObject.MakeLaunchSite(false);
                }
                else
                {
                    _currentLaunchSiteName = "";
                    _lsNameVisible = true;
                }
            }

            #endregion

            bool reorient = false;

            GUI.BeginGroup(new Rect(10, 125, 380, 300));
            GUI.Label(new Rect(0, 0, 200, 22), "Object Placement Controls");

            #region Speed Multipliers

            GUI.Label(new Rect(10, 25, 200, 22), "Transition Speed Multiplier");
            GUI.backgroundColor = _speedMultiplier == 1 ? Color.cyan : Color.white;
            if (GUI.Button(new Rect(185, 25, 35, 22), "1x"))
                _speedMultiplier = 1;

            GUI.backgroundColor = _speedMultiplier == 5 ? Color.cyan : Color.white;
            if (GUI.Button(new Rect(225, 25, 35, 22), "5x"))
                _speedMultiplier = 5;

            GUI.backgroundColor = _speedMultiplier == 10 ? Color.cyan : Color.white;
            if (GUI.Button(new Rect(265, 25, 35, 22), "10x"))
                _speedMultiplier = 10;

            GUI.backgroundColor = _speedMultiplier == 20 ? Color.cyan : Color.white;
            if (GUI.Button(new Rect(305, 25, 35, 22), "20x"))
                _speedMultiplier = 20;

            GUI.backgroundColor = _speedMultiplier == 50 ? Color.cyan : Color.white;
            if (GUI.Button(new Rect(345, 25, 35, 22), "50x"))
                _speedMultiplier = 50;

            #endregion

            #region X Position

            GUI.backgroundColor = Color.red;
            GUI.Label(new Rect(10, 50, 100, 22), "X Position");
            if (GUI.RepeatButton(new Rect(100, 50, 30, 22), "<<"))
            {
                _currentSelectedObject.RadPosition.x -= (1*_speedMultiplier);
                reorient = true;
            }
            if (GUI.Button(new Rect(130, 50, 30, 22), "<"))
            {
                _currentSelectedObject.RadPosition.x -= 0.5f;
                reorient = true;
            }
            if (GUI.Button(new Rect(170, 50, 30, 22), ">"))
            {
                _currentSelectedObject.RadPosition.x += 0.5f;
                reorient = true;
            }
            if (GUI.RepeatButton(new Rect(200, 50, 30, 22), ">>"))
            {
                _currentSelectedObject.RadPosition.x += (1*_speedMultiplier);
                reorient = true;
            }

            _xPosition = GUI.TextField(new Rect(240, 50, 140, 22), _xPosition);

            #endregion

            #region Z Position

            GUI.Label(new Rect(10, 100, 100, 22), "Z Position");
            if (GUI.RepeatButton(new Rect(100, 100, 30, 22), "<<"))
            {
                _currentSelectedObject.RadPosition.z -= (1*_speedMultiplier);
                reorient = true;
            }
            if (GUI.Button(new Rect(130, 100, 30, 22), "<"))
            {
                _currentSelectedObject.RadPosition.z -= 0.5f;
                reorient = true;
            }
            if (GUI.Button(new Rect(170, 100, 30, 22), ">"))
            {
                _currentSelectedObject.RadPosition.z += 0.5f;
                reorient = true;
            }
            if (GUI.RepeatButton(new Rect(200, 100, 30, 22), ">>"))
            {
                _currentSelectedObject.RadPosition.z += (1*_speedMultiplier);
                reorient = true;
            }

            _zPosition = GUI.TextField(new Rect(240, 100, 140, 22), _zPosition);

            #endregion

            #region Y Position

            GUI.backgroundColor = Color.blue;
            GUI.Label(new Rect(10, 75, 100, 22), "Y Position");
            if (GUI.RepeatButton(new Rect(100, 75, 30, 22), "<<"))
            {
                _currentSelectedObject.RadPosition.y -= (1*_speedMultiplier);
                reorient = true;
            }
            if (GUI.Button(new Rect(130, 75, 30, 22), "<"))
            {
                _currentSelectedObject.RadPosition.y -= 0.5f;
                reorient = true;
            }
            if (GUI.Button(new Rect(170, 75, 30, 22), ">"))
            {
                _currentSelectedObject.RadPosition.y += 0.5f;
                reorient = true;
            }
            if (GUI.RepeatButton(new Rect(200, 75, 30, 22), ">>"))
            {
                _currentSelectedObject.RadPosition.y += (1*_speedMultiplier);
                reorient = true;
            }

            _yPosition = GUI.TextField(new Rect(240, 75, 140, 22), _yPosition);

            #endregion

            #region R Offset

            GUI.backgroundColor = Color.green;
            GUI.Label(new Rect(10, 125, 100, 22), "Altitude");
            if (GUI.RepeatButton(new Rect(100, 125, 30, 22), "<<"))
            {
                _currentSelectedObject.RadOffset -= (1*_speedMultiplier);
                reorient = true;
            }
            if (GUI.Button(new Rect(130, 125, 30, 22), "<"))
            {
                _currentSelectedObject.RadOffset -= 0.5f;
                reorient = true;
            }
            if (GUI.Button(new Rect(170, 125, 30, 22), ">"))
            {
                _currentSelectedObject.RadOffset += 0.5f;
                reorient = true;
            }
            if (GUI.RepeatButton(new Rect(200, 125, 30, 22), ">>"))
            {
                _currentSelectedObject.RadOffset += (1*_speedMultiplier);
                reorient = true;
            }

            _rPosition = GUI.TextField(new Rect(240, 125, 140, 22), _rPosition);

            #endregion

            #region Visibility Range

            GUI.backgroundColor = XKCDColors.Orange;
            GUI.Label(new Rect(10, 150, 100, 22), "Vis. Range");
            _visRange = GUI.TextField(new Rect(100, 150, 130, 22), _visRange);

            #endregion

            #region Scale Factor

            //GUI.Label(new Rect(10, 175, 100, 22), "Scale");
            //GUI.backgroundColor = Color.red;
            //_xScale = GUI.TextField(new Rect(100, 175, 40, 22), _xScale);
            //GUI.backgroundColor = Color.green;
            //_yScale = GUI.TextField(new Rect(140, 175, 50, 22), _yScale);
            //GUI.backgroundColor = Color.blue;
            //_zScale = GUI.TextField(new Rect(190, 175, 40, 22), _zScale);

            #endregion

            #region Update

            GUI.backgroundColor = Color.yellow;
            if (GUI.Button(new Rect(240, 150, 140, 50), "\x2190      Update      \x2191"))
            {
                float floatVal;
                if (float.TryParse(_xPosition, out floatVal)) _currentSelectedObject.RadPosition.x = floatVal;
                if (float.TryParse(_zPosition, out floatVal)) _currentSelectedObject.RadPosition.z = floatVal;
                if (float.TryParse(_yPosition, out floatVal)) _currentSelectedObject.RadPosition.y = floatVal;
                if (float.TryParse(_rPosition, out floatVal)) _currentSelectedObject.RadOffset = floatVal;
                if (float.TryParse(_visRange, out floatVal)) _currentSelectedObject.VisRange = floatVal;
                //if (float.TryParse(_xScale, out floatVal))
                //{
                //    _currentSelectedObject.Scale.x = floatVal;
                //    if (float.TryParse(_yScale, out floatVal)) _currentSelectedObject.Scale.y = floatVal;
                //    if (float.TryParse(_zScale, out floatVal)) _currentSelectedObject.Scale.z = floatVal;
                //}

                reorient = true;
            }

            #endregion

            #region Orientation

            GUI.backgroundColor = Color.white;
            GUI.Label(new Rect(10, 210, 100, 22), "Orientation");
            if (GUI.Button(new Rect(100, 210, 30, 22), "\x2191"))
            {
                _currentSelectedObject.Orientation = Vector3.up;
                reorient = true;
            }
            if (GUI.Button(new Rect(140, 210, 30, 22), "\x2192"))
            {
                _currentSelectedObject.Orientation = Vector3.right;
                reorient = true;
            }
            if (GUI.Button(new Rect(180, 210, 30, 22), "\x2193"))
            {
                _currentSelectedObject.Orientation = Vector3.down;
                reorient = true;
            }
            if (GUI.Button(new Rect(220, 210, 30, 22), "\x2190"))
            {
                _currentSelectedObject.Orientation = Vector3.left;
                reorient = true;
            }
            if (GUI.Button(new Rect(255, 210, 55, 22), "Back"))
            {
                _currentSelectedObject.Orientation = Vector3.forward;
                reorient = true;
            }
            if (GUI.Button(new Rect(315, 210, 65, 22), "Forward"))
            {
                _currentSelectedObject.Orientation = Vector3.back;
                reorient = true;
            }

            #endregion

            #region Rotation

            GUI.Label(new Rect(10, 240, 100, 22), "Rotation");
            _currentSelectedObject.RotAngle = GUI.HorizontalSlider(new Rect(100, 245, 280, 20),
                _currentSelectedObject.RotAngle, 0f, 360f);

            if (Math.Abs(_prevRotationAngle - _currentSelectedObject.RotAngle) > 0.001f)
            {
                _prevRotationAngle = _currentSelectedObject.RotAngle;
                reorient = true;
            }

            #endregion

            #region if(reorient)

            if (reorient)
            {
                _xPosition = _currentSelectedObject.RadPosition.x.ToString(CultureInfo.InvariantCulture);
                _yPosition = _currentSelectedObject.RadPosition.y.ToString(CultureInfo.InvariantCulture);
                _zPosition = _currentSelectedObject.RadPosition.z.ToString(CultureInfo.InvariantCulture);
                _rPosition = _currentSelectedObject.RadOffset.ToString(CultureInfo.InvariantCulture);

                //_xScale = _currentSelectedObject.Scale.x.ToString(CultureInfo.InvariantCulture).Replace("0.9999998", "1");
                //_yScale = _currentSelectedObject.Scale.y.ToString(CultureInfo.InvariantCulture).Replace("0.9999998", "1");
                //_zScale = _currentSelectedObject.Scale.z.ToString(CultureInfo.InvariantCulture).Replace("0.9999998", "1"); 

                _visRange = _currentSelectedObject.VisRange.ToString(CultureInfo.InvariantCulture);

                _currentSelectedObject.Latitude = GetLatitude(_currentSelectedObject.RadPosition);
                _currentSelectedObject.Longitude = GetLongitude(_currentSelectedObject.RadPosition);
                _currentSelectedObject.Reorientate();
                //_currentSelectedObject.Rescale();
            }

            #endregion

            GUI.EndGroup();
        }

        private void DrawSelectedWindow(int windowID)
        {
            if (_currentSelectedObject == null)
            {
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                GUI.Label(new Rect(20, 20, 150, 22), "No object selected.");
                return;
            }

            string latitude = _currentSelectedObject.Latitude.ToString("N6");
            string longitude = _currentSelectedObject.Longitude.ToString("N6");
            string radialOffset = _currentSelectedObject.RadOffset.ToString("N2");

            Vector3 vecPosition = _currentSelectedObject.RadPosition;
            Vector3 vecOrientation = _currentSelectedObject.Orientation;

            GUI.Label(new Rect(10, 20, 150, 22), "Latitude / Longitude:");
            GUI.Label(new Rect(150, 20, 300, 22), latitude + " / " + longitude);

            GUI.Label(new Rect(10, 40, 150, 22), "Radial Offset (Alt.):");
            GUI.Label(new Rect(150, 40, 300, 22), radialOffset);

            GUI.Label(new Rect(10, 60, 150, 22), "Local Position (Vec3):");
            GUI.Label(new Rect(150, 60, 300, 22), vecPosition.ToString());

            GUI.Label(new Rect(10, 80, 150, 22), "Orientation:");
            GUI.Label(new Rect(150, 80, 300, 22), vecOrientation.ToString());

            GUI.Label(new Rect(10, 100, 150, 22), "Body Name:");
            GUI.Label(new Rect(150, 100, 300, 22), _currentSelectedObject.CelestialBodyName);

            GUI.Label(new Rect(10, 120, 150, 22), "Selected Object:");
            GUI.Label(new Rect(150, 120, 300, 22), _currentSelectedObject.NameID);
        }

        private void DrawAvailAssetWindow(int windowID)
        {
            GUI.Box(new Rect(10, 30, 580, 290), "");

            _availAssetScrollPos = GUI.BeginScrollView(new Rect(10, 30, 580, 290), _availAssetScrollPos,
                new Rect(0, 0, 560, _modelList.Count*28 + 5));

            int i = 0;

            // Model = ModelURL
            // Model[x] = ConfigURL
            foreach (string model in _modelList.Keys)
            {
                bool itemMatches = (model == _currentModelUrl);
                GUI.backgroundColor = new Color(0.3f, itemMatches ? 1f : 0.3f, 0.3f);

                if (GUI.Button(new Rect(5, (i*28) + 5, 550, 25),
                    itemMatches
                        ? string.Format("[ {0} ]", model)
                        : model))
                {
                    _currentModelUrl = itemMatches ? "" : model; // Select / Deselect
                    _currentConfigUrl = _currentConfigUrl == _modelList[model] ? "" : _modelList[model];
                }

                i++;
            }

            GUI.EndScrollView();

            GUI.Label(new Rect(20, 330, 300, 25), "Current body: " + _currentBodyName);

            GUI.backgroundColor = new Color(0.0f, _currentModelUrl != "" ? 0.7f : 0.0f, 0.0f);
            if (GUI.Button(new Rect(480, 330, 100, 30), "Create") && _currentModelUrl != "")
            {
                // Clear old vis/position/scale info.
                //_visRange = _xScale = _yScale = _zScale = _xPosition = _rPosition = _yPosition = _zPosition = "";
                _visRange = _xPosition = _rPosition = _yPosition = _zPosition = "";


                // Set the current celestial object. (Needs to be set before GetDefaultStaticObject).
                _currentBodyName = FlightGlobals.ActiveVessel.mainBody.bodyName;
                _currentCelestialObj = GetCelestialObject(_currentBodyName);

                StaticObject newObject = GetDefaultStaticObject(_currentModelUrl, _currentConfigUrl);
                if (_instancedList.ContainsKey(_currentModelUrl))
                {
                    _instancedList[_currentModelUrl].Add(newObject);
                }
                else
                {
                    _instancedList.Add(_currentModelUrl,
                        new List<StaticObject>
                        {
                            newObject
                        });
                }

                newObject.CelestialBodyName = _currentCelestialObj.CelestialBodyComponent.name;

                // _currentCelestialObj assigned above.
                InstantiateStatic(_currentCelestialObj.PQSComponent, newObject, true);

                // Remove previously highlighted object if there is one.
                if (_currentSelectedObject != null) _currentSelectedObject.Manipulate(false);

                _currentObjectID = newObject.ObjectID;
                _currentSelectedObject = newObject;

                // Highlight new selected object.
                if (_currentSelectedObject != null) _currentSelectedObject.Manipulate(true);
            }

            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            if (GUI.Button(new Rect(410, 335, 60, 25), "Close"))
                _availAssetsVisible = false;

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void DrawCurrAssetWindow(int windowID)
        {
            GUI.Box(new Rect(10, 30, 580, 290), ""); // Background

            _currAssetScrollPos = GUI.BeginScrollView(new Rect(10, 30, 580, 290), _currAssetScrollPos,
                new Rect(0, 0, 560, _instancedList.Values.Sum(list => list.Count)*28 + 5));

            int i = 0;

            foreach (string objectList in _instancedList.Keys)
            {
                foreach (StaticObject sObject in _instancedList[objectList])
                {
                    bool itemMatches = sObject.ObjectID == _currentObjectID;
                    GUI.backgroundColor = new Color(0.3f, itemMatches ? 1f : 0.3f, 0.3f);

                    if (GUI.Button(new Rect(5, (i*28) + 5, 550, 25),
                        string.Format("{0} (ID: {1})", sObject.ModelUrl, sObject.ObjectID)))
                    {
                        // Clear text fields.
                        //_visRange = _xScale = _yScale = _zScale = _xPosition = _rPosition = _yPosition = _zPosition = "";
                        _visRange = _xPosition = _rPosition = _yPosition = _zPosition = "";

                        if (itemMatches)
                        {
                            // Deselect
                            if (_currentSelectedObject != null)
                                _currentSelectedObject.Manipulate(false);

                            _currentObjectID = "";
                            _currentSelectedObject = null;
                            _lsNameVisible = false;
                        }
                        else
                        {
                            // Select
                            if (_currentSelectedObject != null)
                                _currentSelectedObject.Manipulate(false);

                            _currentObjectID = sObject.ObjectID;
                            _currentSelectedObject = sObject;
                            _currentSelectedObject.Manipulate(true);
                        }
                    }
                    i++;
                }
            }
            GUI.EndScrollView();

            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            if (GUI.Button(new Rect(120, 335, 60, 25), "Close"))
                _currAssetsVisible = false;

            GUI.backgroundColor = new Color(_currentSelectedObject != null ? 1 : 0, 0, 0);
            if (GUI.Button(new Rect(10, 330, 100, 30), "Delete"))
            {
                if (_currentSelectedObject != null)
                {
                    KtCamera.RestoreCameraParent();
                    //Destroy(_currentSelectedObject.StaticGameObject);
                    DestroySoInstance(_currentSelectedObject);
                    //RemoveCurrentStaticObject(_currentSelectedObject.ModelUrl);
                    _instancedList[_currentSelectedObject.ModelUrl].Remove(_currentSelectedObject);
                }

                _currentObjectID = "";
                _currentSelectedObject = null;
            }

            GUI.backgroundColor = new Color(0, 0, _currentSelectedObject != null ? 1 : 0);
            if (GUI.Button(new Rect(490, 330, 100, 30), "Initialize"))
            {
                if (_currentSelectedObject != null)
                {
                    InvokeSetup(_currentSelectedObject);
                }
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
    }
}