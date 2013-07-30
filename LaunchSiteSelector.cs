using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kerbtown
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class LaunchSiteSelector : MonoBehaviour
    {
        private readonly List<string> _launchSiteList = new List<string>();

        private void GetLaunchSiteList()
        {
            UrlDir.UrlConfig[] staticConfigs = GameDatabase.Instance.GetConfigs("STATIC");
            _launchSiteList.Add("LaunchPad");
            _launchSiteList.Add("Runway");

            foreach (UrlDir.UrlConfig staticUrlConfig in staticConfigs)
            {
                if (!staticUrlConfig.config.HasNode("Instances"))
                    continue;

                foreach (ConfigNode ins in staticUrlConfig.config.GetNodes("Instances"))
                {
                    string launchSiteName = ins.GetValue("LaunchSiteName");

                    if (string.IsNullOrEmpty(launchSiteName))
                        continue;

                    _launchSiteList.Add(launchSiteName);
                }
            }
        }

        public void Start()
        {
            GetLaunchSiteList();
            
        }

        private bool _menuVisible;
        private Rect _boxRect = new Rect(Screen.width/2 + 290, -100, 170, 100);

        private readonly Rect _launchSiteButtonRect = new Rect(Screen.width/2 + 280, 0, 180, 36);
        private float _topPosition = -250;
        private Vector2 _scrollPosition = new Vector2(0, 0);
        private bool _menuSliding;
        private string _currentLaunchSite = "";

        public void OnGUI()
        {
            if (EditorLogic.fetch == null) 
                return;

            GUI.skin = EditorPartsListController.fetch != null ? EditorPartsListController.fetch.pageButtonsSkin : HighLogic.Skin;

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
                            EditorLogic.fetch.launchSiteName = launchSite;
                            _currentLaunchSite = launchSite;
                            Debug.LogWarning("[KerbTown] Set LaunchSite to: " + launchSite);
                            StartCoroutine(ToggleMenu());
                        }
                    }

                    GUI.EndScrollView();
                    GUI.EndGroup();
                }
            }

            GUI.depth = -99;
            GUI.backgroundColor = new Color(0.5f, 0.65f, 0.27f, 1);

            if (GUI.Button(_launchSiteButtonRect, _currentLaunchSite == "" ? "Select Launch Site" : _currentLaunchSite))
            {
                StartCoroutine(ToggleMenu());
            }
        }

        private IEnumerator ToggleMenu()
        {
            _menuSliding = true;
            if (!_menuVisible)
            {
                _menuVisible = true;
                _topPosition = -250;
                while (_topPosition < 0)
                {
                    yield return new WaitForSeconds(0.01f);
                    _topPosition += 10;

                    if (_topPosition <= 0)
                        _boxRect = new Rect(Screen.width / 2 + 290, _topPosition, 250, 250);
                }
            }
            else
            {
                _topPosition = 0;
                while (_topPosition > -250)
                {
                    yield return new WaitForSeconds(0.01f);
                    _topPosition -= 10;

                    if (_topPosition >= -250)
                        _boxRect = new Rect(Screen.width / 2 + 290, _topPosition, 250, 250);
                }
                _menuVisible = false;
            }
            _menuSliding = false;
        }
    }
}
