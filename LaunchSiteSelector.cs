/* LICENSE
 * This source code is copyrighted.
 * All rights reserved.
 * Copyright © Ryan Irecki 2013
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Kerbtown
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class LaunchSiteSelector : MonoBehaviour
    {
        private readonly List<string> _launchSiteList = new List<string>();
        private bool _alternatePosition;

        private Rect _boxRect = new Rect(Screen.width/2 + 290, -100, 170, 100);
        private string _currentLaunchSite = "";
        // private string _defaultLaunchSite = "LaunchPad"; // Deprecated

        private Rect _launchSiteButtonRect = new Rect(Screen.width/2 + 280, 0, 48, 48);
        private bool _menuSliding;
        private bool _menuVisible;
        private float _positionOffset = -250;
        private Vector2 _scrollPosition = new Vector2(0, 0);

        private void PopulateLaunchSiteList()
        {
            UrlDir.UrlConfig[] staticConfigs = GameDatabase.Instance.GetConfigs("STATIC");
            _launchSiteList.Add("LaunchPad");
            _launchSiteList.Add("Runway");

            foreach (string launchSiteName in from staticUrlConfig in staticConfigs
                where staticUrlConfig.config.HasNode("Instances")
                from ins in staticUrlConfig.config.GetNodes("Instances")
                select ins.GetValue("LaunchSiteName")
                into launchSiteName
                where !string.IsNullOrEmpty(launchSiteName)
                select launchSiteName)
                _launchSiteList.Add(launchSiteName);
        }

        public void Start()
        {
            if (Screen.width < 1152)
            {
                _alternatePosition = true;
                _launchSiteButtonRect = new Rect(Screen.width - 240, 45, 48, 48);
            }

            // Deprecated
            // if (HighLogic.LoadedScene == GameScenes.SPH) _defaultLaunchSite = "Runway";

            PopulateLaunchSiteList();

            string lastLaunchSite = Extensions.ReadSetting("lastLaunchSite");

            CreateButtonStyle();

            if (lastLaunchSite == null || lastLaunchSite == "Runway" || lastLaunchSite == "LaunchPad")
                return;
            
            // If it's available, set the launch site to the last used launch site.
            foreach (var launchSite in _launchSiteList.Where(launchSite => launchSite == lastLaunchSite))
            {
                SetLaunchSite(launchSite);
                break;
            }
        }

        private GUIStyle _buttonStyle;
        private void CreateButtonStyle()
        {
            _buttonStyle = new GUIStyle(GUIStyle.none);

            var tNormal = GameDatabase.Instance.GetTexture("Hubs/Assets/KerbTown/ktbNormal", false);
            var tActive = GameDatabase.Instance.GetTexture("Hubs/Assets/KerbTown/ktbActive", false);
            var tOver = GameDatabase.Instance.GetTexture("Hubs/Assets/KerbTown/ktbOver", false);
            //var tDisabled = GameDatabase.Instance.GetTexture("Hubs/Assets/KerbTown/ktbDisabled", false);

            _buttonStyle.normal.background = tNormal;
            _buttonStyle.active.background = tActive;
            _buttonStyle.hover.background = tOver;
        }

        public void OnGUI()
        {
            if (EditorLogic.fetch == null)
                return;

            GUI.skin = EditorPartsListController.fetch != null
                ? EditorPartsListController.fetch.pageButtonsSkin
                : HighLogic.Skin;

            if (_menuVisible)
            {
                GUI.depth = 99;
                GUI.Box(_boxRect, "");

                if (!_menuSliding)
                {
                    GUI.BeginGroup(_boxRect);
                    _scrollPosition = GUI.BeginScrollView(
                        new Rect(10, 50, 230, 190), _scrollPosition,
                        new Rect(0, 0, 230, _launchSiteList.Count*30 + 10));


                    for (int index = 0; index < _launchSiteList.Count; index++)
                    {
                        string launchSite = _launchSiteList[index];
                        if (GUI.Button(new Rect(5, index*30 + 5, 220, 30), launchSite))
                        {
                            SetLaunchSite(launchSite);
                            StartCoroutine(!_alternatePosition ? ToggleMenu() : ToggleMenuAlt());
                        }
                    }

                    GUI.EndScrollView();
                    GUI.EndGroup();
                }
            }

            GUI.depth = -99;

            if (_buttonStyle == null) return;
            if (GUI.Button(_launchSiteButtonRect,new GUIContent("", _currentLaunchSite == "" ? "Select Launch Site" : _currentLaunchSite), _buttonStyle))
            {
                StartCoroutine(!_alternatePosition ? ToggleMenu() : ToggleMenuAlt());
            }
            if (GUI.tooltip != "")
            {
                var labelSize = GUI.skin.GetStyle("Label").CalcSize(new GUIContent(GUI.tooltip));
                GUI.Box(
                    new Rect(Event.current.mousePosition.x + 10, Event.current.mousePosition.y, labelSize.x+10, labelSize.y), GUI.tooltip); 
            }
        }

        private void SetLaunchSite(string launchSite)
        {
            _currentLaunchSite = launchSite;
            EditorLogic.fetch.launchSiteName = launchSite;
            Extensions.WriteSetting("lastLaunchSite",launchSite);
            Extensions.PostScreenMessage("Launch Site set to: " + launchSite, 5);
        }

        private IEnumerator ToggleMenu()
        {
            _menuSliding = true;
            if (!_menuVisible)
            {
                _menuVisible = true;
                _positionOffset = -250;
                while (_positionOffset < 0)
                {
                    yield return null;
                    _positionOffset += 500 * Time.deltaTime;

                    if (_positionOffset <= 0)
                        _boxRect = new Rect(Screen.width/2 + 290, _positionOffset, 250, 250);
                }
            }
            else
            {
                _positionOffset = 0;
                while (_positionOffset > -250)
                {
                    yield return null;
                    _positionOffset -= 500 * Time.deltaTime;

                    if (_positionOffset >= -250)
                        _boxRect = new Rect(Screen.width/2 + 290, _positionOffset, 250, 250);
                }
                _menuVisible = false;
            }
            _menuSliding = false;
        }

        private IEnumerator ToggleMenuAlt()
        {
            _menuSliding = true;
            if (!_menuVisible)
            {
                _menuVisible = true;
                _positionOffset = Screen.width;
                while (_positionOffset > Screen.width - 250)
                {
                    yield return null;
                    _positionOffset -= 500 * Time.deltaTime;

                    if (_positionOffset >= Screen.width - 250)
                        _boxRect = new Rect(_positionOffset, 40, 250, 250);
                }
            }
            else
            {
                _positionOffset = 40;
                while (_positionOffset > -250)
                {
                    yield return null;
                    _positionOffset -= 500 * Time.deltaTime;

                    if (_positionOffset >= -250)
                        _boxRect = new Rect(Screen.width - 250, _positionOffset, 250, 250);
                }
                _menuVisible = false;
            }
            _menuSliding = false;
        }
    }
}