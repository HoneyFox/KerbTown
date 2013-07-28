/* LICENSE
 * This work is licensed under the Creative Commons Attribution-NoDerivs 3.0 Unported License. 
 * To view a copy of this license, visit http://creativecommons.org/licenses/by-nd/3.0/ or send a letter to Creative Commons, 444 Castro Street, Suite 900, Mountain View, California, 94041, USA.
 */

using System;
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
        private Rect _mainWindowRect = new Rect(20, 20, 410, 400);

        #endregion

        #region Visibility Flags

        private bool _availAssetsVisible = true;
        private bool _currAssetsVisible;
        private bool _mainWindowVisible;

        #endregion

        private Vector2 _availAssetScrollPos;
        private Vector2 _currAssetScrollPos;
        private string _currentObjectID = "";

        #endregion

        private string _rPosition = "";
        private string _xPosition = "";
        private string _yPosition = "";
        private string _zPosition = "";

        private void OnGUI()
        {
            if (!_mainWindowVisible)
                return;

            // Main Window
            _mainWindowRect = GUI.Window(0x8100, _mainWindowRect, DrawMainWindow, "KerbTown Editor");

            // Selected Object Window
            GUI.Window(0x8103,
                new Rect(_mainWindowRect.x + 5, _mainWindowRect.y + _mainWindowRect.height + 5, 400,
                    _currentSelectedObject != null ? 150 : 50), DrawSelectedWindow, "Selected Object Information");

            // Asset List window
            if (_availAssetsVisible)
                _assetListRect = GUI.Window(0x8101, _assetListRect, DrawAvailAssetWindow, "Available Static Assets List");

            // Current objects window
            if (_currAssetsVisible)
            {
                _currAssetListRect = GUI.Window(0x8102, _currAssetListRect, DrawCurrAssetWindow,
                    "Existing Static Assets List");
            }
        }

        private void DrawMainWindow(int windowID)
        {
            GUI.Label(new Rect(10, 20, 100, 22), "Asset Lists");

            if (GUI.Button(new Rect(20, 40, 180, 22), _availAssetsVisible ? "Hide Available" : "Show Available"))
                _availAssetsVisible = !_availAssetsVisible;

            if (GUI.Button(new Rect(210, 40, 180, 22), _currAssetsVisible ? "Hide Existing" : "Show Existing"))
                _currAssetsVisible = !_currAssetsVisible;

            GUI.Label(new Rect(10, 70, 100, 22), "Functions");

            if (GUI.Button(new Rect(20, 90, 220, 22), "Write current session to Configs."))
            {
                WriteSessionConfigs();
            }

            if (_currentSelectedObject != null)
            {
                DrawPositioningControls();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void DrawPositioningControls()
        {
            //if (GUI.Button(new Rect(20, 90, 180, 22), "Pick-up selected object."))
            //{
            //    _objectSelected = !_objectSelected;
            //    //todo pickup object
            //}
            //GUI.Label(new Rect(210, 90, 200, 22), "- Hold CTRL to drop object.");

            bool reorient = false;

            GUI.BeginGroup(new Rect(10, 125, 380, 300));

            GUI.Label(new Rect(0, 0, 200, 22), "Object Placement Controls");

            #region X Position

            GUI.backgroundColor = Color.red;
            GUI.Label(new Rect(10, 25, 100, 22), "X Position");
            if (GUI.RepeatButton(new Rect(100, 25, 30, 22), "<<"))
            {
                _currentSelectedObject.RadPosition.x--;
                reorient = true;
            }
            if (GUI.Button(new Rect(130, 25, 30, 22), "<"))
            {
                _currentSelectedObject.RadPosition.x -= 0.5f;
                reorient = true;
            }
            if (GUI.Button(new Rect(170, 25, 30, 22), ">"))
            {
                _currentSelectedObject.RadPosition.x += 0.5f;
                reorient = true;
            }
            if (GUI.RepeatButton(new Rect(200, 25, 30, 22), ">>"))
            {
                _currentSelectedObject.RadPosition.x++;
                reorient = true;
            }

            _xPosition = GUI.TextField(new Rect(240, 25, 140, 22), _xPosition);

            #endregion

            #region Z Position

            GUI.Label(new Rect(10, 75, 100, 22), "Z Position");
            if (GUI.RepeatButton(new Rect(100, 75, 30, 22), "<<"))
            {
                _currentSelectedObject.RadPosition.z--;
                reorient = true;
            }
            if (GUI.Button(new Rect(130, 75, 30, 22), "<"))
            {
                _currentSelectedObject.RadPosition.z -= 0.5f;
                reorient = true;
            }
            if (GUI.Button(new Rect(170, 75, 30, 22), ">"))
            {
                _currentSelectedObject.RadPosition.z += 0.5f;
                reorient = true;
            }
            if (GUI.RepeatButton(new Rect(200, 75, 30, 22), ">>"))
            {
                _currentSelectedObject.RadPosition.z++;
                reorient = true;
            }

            _zPosition = GUI.TextField(new Rect(240, 75, 140, 22), _zPosition);

            #endregion

            #region Y Position

            GUI.backgroundColor = Color.blue;
            GUI.Label(new Rect(10, 50, 100, 22), "Y Position");
            if (GUI.RepeatButton(new Rect(100, 50, 30, 22), "<<"))
            {
                _currentSelectedObject.RadPosition.y--;
                reorient = true;
            }
            if (GUI.Button(new Rect(130, 50, 30, 22), "<"))
            {
                _currentSelectedObject.RadPosition.y -= 0.5f;
                reorient = true;
            }
            if (GUI.Button(new Rect(170, 50, 30, 22), ">"))
            {
                _currentSelectedObject.RadPosition.y += 0.5f;
                reorient = true;
            }
            if (GUI.RepeatButton(new Rect(200, 50, 30, 22), ">>"))
            {
                _currentSelectedObject.RadPosition.y++;
                reorient = true;
            }

            _yPosition = GUI.TextField(new Rect(240, 50, 140, 22), _yPosition);

            #endregion

            #region R Offset

            GUI.backgroundColor = Color.green;
            GUI.Label(new Rect(10, 100, 100, 22), "Altitude");
            if (GUI.RepeatButton(new Rect(100, 100, 30, 22), "<<"))
            {
                _currentSelectedObject.RadOffset--;
                reorient = true;
            }
            if (GUI.Button(new Rect(130, 100, 30, 22), "<"))
            {
                _currentSelectedObject.RadOffset -= 0.5f;
                reorient = true;
            }
            if (GUI.Button(new Rect(170, 100, 30, 22), ">"))
            {
                _currentSelectedObject.RadOffset += 0.5f;
                reorient = true;
            }
            if (GUI.RepeatButton(new Rect(200, 100, 30, 22), ">>"))
            {
                _currentSelectedObject.RadOffset++;
                reorient = true;
            }

            _rPosition = GUI.TextField(new Rect(240, 100, 140, 22), _rPosition);

            #endregion

            GUI.backgroundColor = Color.yellow;
            if (GUI.Button(new Rect(310, 125, 70, 22), "Update ^"))
            {
                float floatVal;
                if (float.TryParse(_xPosition, out floatVal)) _currentSelectedObject.RadPosition.x = floatVal;
                if (float.TryParse(_zPosition, out floatVal)) _currentSelectedObject.RadPosition.z = floatVal;
                if (float.TryParse(_yPosition, out floatVal)) _currentSelectedObject.RadPosition.y = floatVal;
                if (float.TryParse(_rPosition, out floatVal)) _currentSelectedObject.RadOffset = floatVal;

                reorient = true;
            }

            #region Orientation

            GUI.backgroundColor = Color.white;
            GUI.Label(new Rect(10, 125, 100, 22), "Orientation");
            if (GUI.Button(new Rect(100, 125, 30, 22), "\x2191"))
            {
                _currentSelectedObject.Orientation = Vector3.up;
                reorient = true;
            }
            if (GUI.Button(new Rect(130, 125, 30, 22), "\x2192"))
            {
                _currentSelectedObject.Orientation = Vector3.right;
                reorient = true;
            }
            if (GUI.Button(new Rect(160, 125, 30, 22), "\x2193"))
            {
                _currentSelectedObject.Orientation = Vector3.down;
                reorient = true;
            }
            if (GUI.Button(new Rect(190, 125, 30, 22), "\x2190"))
            {
                _currentSelectedObject.Orientation = Vector3.left;
                reorient = true;
            }
            if (GUI.Button(new Rect(220, 125, 30, 22), "\x25cb"))
            {
                _currentSelectedObject.Orientation = Vector3.forward;
                reorient = true;
            }
            if (GUI.Button(new Rect(250, 125, 30, 22), "\x25cf"))
            {
                _currentSelectedObject.Orientation = Vector3.back;
                reorient = true;
            }

            #endregion

            #region Rotation

            GUI.Label(new Rect(10, 150, 100, 22), "Rotation");
            _currentSelectedObject.RotAngle = GUI.HorizontalSlider(new Rect(100, 155, 180, 20),
                _currentSelectedObject.RotAngle, 0f, 359f);

            if (Math.Abs(_prevRotationAngle - _currentSelectedObject.RotAngle) > 0.001f)
            {
                _prevRotationAngle = _currentSelectedObject.RotAngle;
                reorient = true;
            }

            #endregion

            if (reorient)
            {
                _xPosition = _currentSelectedObject.RadPosition.x.ToString(CultureInfo.InvariantCulture);
                _yPosition = _currentSelectedObject.RadPosition.y.ToString(CultureInfo.InvariantCulture);
                _zPosition = _currentSelectedObject.RadPosition.z.ToString(CultureInfo.InvariantCulture);
                _rPosition = _currentSelectedObject.RadOffset.ToString(CultureInfo.InvariantCulture);

                _currentSelectedObject.Latitude = GetLatitude(_currentSelectedObject.RadPosition);
                _currentSelectedObject.Longitude = GetLongitude(_currentSelectedObject.RadPosition);
                _currentSelectedObject.Reorientate();
            }

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
            GUI.Box(new Rect(10, 20, 580, 300), "");

            _availAssetScrollPos = GUI.BeginScrollView(new Rect(10, 20, 580, 300), _availAssetScrollPos,
                new Rect(0, 0, 560, _modelList.Count*25 + 5));

            int i = 0;

            // Model = ModelURL
            // Model[x] = ConfigURL
            foreach (string model in _modelList.Keys)
            {
                bool itemMatches = (model == _currentModelUrl);
                GUI.backgroundColor = new Color(0.3f, itemMatches ? 1f : 0.3f, 0.3f);

                if (GUI.Button(new Rect(5, (i*25) + 5, 550, 22),
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

            GUI.Label(new Rect(20, 330, 300, 22), "Current body: " + _currentBodyName);

            GUI.backgroundColor = new Color(0.0f, _currentModelUrl != "" ? 0.7f : 0.0f, 0.0f);
            if (GUI.Button(new Rect(480, 330, 100, 30), "Create") && _currentModelUrl != "")
            {
                // Clear old position info.
                _xPosition = _rPosition = _yPosition = _zPosition = "";

                // todo: remove old debug code and start using activevessel references.
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
                InstantiateStatic(_currentCelestialObj.PQSComponent, newObject);

                // Remove previously highlighted object if there is one.
                if (_currentSelectedObject != null) _currentSelectedObject.Manipulate(false);

                _currentObjectID = newObject.ObjectID;
                _currentSelectedObject = newObject;

                // Highlight new selected object.
                if (_currentSelectedObject != null) _currentSelectedObject.Manipulate(true);
            }

            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            if (GUI.Button(new Rect(410, 335, 60, 20), "Close"))
                _availAssetsVisible = false;

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void DrawCurrAssetWindow(int windowID)
        {
            // ScrollView, temporary background.
            GUI.Box(new Rect(10, 20, 580, 300), "");

            _currAssetScrollPos = GUI.BeginScrollView(new Rect(10, 20, 580, 300), _currAssetScrollPos,
                new Rect(0, 0, 560, _instancedList.Values.Sum(list => list.Count)*25 + 5));

            int i = 0;

            foreach (string objectList in _instancedList.Keys)
            {
                foreach (StaticObject sObject in _instancedList[objectList])
                {
                    bool itemMatches = sObject.ObjectID == _currentObjectID;
                    GUI.backgroundColor = new Color(0.3f, itemMatches ? 1f : 0.3f, 0.3f);

                    if (GUI.Button(new Rect(5, (i*25) + 5, 550, 22),
                        string.Format("{0} (ID: {1})", sObject.ModelUrl, sObject.ObjectID)))
                    {
                        if (itemMatches)
                        {
                            // Deselect
                            if (_currentSelectedObject != null)
                                _currentSelectedObject.Manipulate(false);

                            _currentObjectID = "";
                            _currentSelectedObject = null;
                        }
                        else
                        {
                            if (_currentSelectedObject != null)
                                _currentSelectedObject.Manipulate(false);

                            _currentObjectID = sObject.ObjectID; // Select
                            _currentSelectedObject = sObject;
                            _currentSelectedObject.Manipulate(true);
                        }
                    }
                    i++;
                }
            }
            GUI.EndScrollView();

            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            if (GUI.Button(new Rect(120, 335, 60, 20), "Close"))
                _currAssetsVisible = false;

            GUI.backgroundColor = new Color(_currentSelectedObject != null ? 1 : 0, 0, 0);
            if (GUI.Button(new Rect(10, 330, 100, 30), "Delete"))
            {
                if (_currentSelectedObject != null)
                {
                    //Destroy(_currentSelectedObject.StaticGameObject);
                    Deactivate(_currentSelectedObject.PQSCityComponent);
                    RemoveCurrentStaticObject(_currentSelectedObject.ModelUrl);
                }

                _currentObjectID = "";
                _currentSelectedObject = null;
            }


            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
    }
}