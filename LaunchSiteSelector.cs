/* LICENSE
 * This work is licensed under the Creative Commons Attribution-NoDerivs 3.0 Unported License. 
 * To view a copy of this license, visit http://creativecommons.org/licenses/by-nd/3.0/ or send a letter to Creative Commons, 444 Castro Street, Suite 900, Mountain View, California, 94041, USA.
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
        private string _defaultLaunchSite = "LaunchPad";

        private Rect _launchSiteButtonRect = new Rect(Screen.width/2 + 280, 0, 180, 36);
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
            if (Screen.width < 1600)
            {
                _alternatePosition = true;
                _launchSiteButtonRect = new Rect(Screen.width - 180, 50, 180, 36);
            }

            if (HighLogic.LoadedScene == GameScenes.SPH)
                _defaultLaunchSite = "Runway";

            PopulateLaunchSiteList();

            string lastLaunchSite = Extensions.ReadSetting("lastLaunchSite");
            if (lastLaunchSite == null) return;
            
            // If it's available, set the launch site to the last used launch site.
            foreach (var launchSite in _launchSiteList.Where(launchSite => launchSite == lastLaunchSite))
            {
                SetLaunchSite(launchSite);
                break;
            }
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

            if (_currentLaunchSite == "" || _currentLaunchSite == _defaultLaunchSite)
                GUI.backgroundColor = new Color(0.5f, 0.65f, 0.27f, 1);
            else
                GUI.backgroundColor = new Color(0.0f, 0.2f, 0.8f, 1);


            if (GUI.Button(_launchSiteButtonRect, _currentLaunchSite == "" ? "Select Launch Site" : _currentLaunchSite))
            {
                StartCoroutine(!_alternatePosition ? ToggleMenu() : ToggleMenuAlt());
            }
        }

        private void SetLaunchSite(string launchSite)
        {
            _currentLaunchSite = launchSite;
            EditorLogic.fetch.launchSiteName = launchSite;
            Extensions.WriteSetting("lastLaunchSite",launchSite);
            Extensions.LogWarning("Set LaunchSite to: " + launchSite);
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