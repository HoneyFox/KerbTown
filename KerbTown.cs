/* LICENSE
 * This source code is copyrighted.
 * All rights reserved.
 * Copyright © Ryan Irecki 2013
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Kerbtown.EEComponents;
using Kerbtown.NativeModules;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Kerbtown
{
	[KSPAddonFixed(KSPAddon.Startup.SpaceCentre, true, typeof(KerbTown))]
    public partial class KerbTown : MonoBehaviour
    {
        private string _currentBodyName = "";
        private CelestialObject _currentCelestialObj;
        private string _currentConfigUrl = "";
        private string _currentModelUrl = "";
        private StaticObject _currentSelectedObject;

		private Dictionary<string, Dictionary<string, string>> _staticPropertyList;

        private Dictionary<string, List<StaticObject>> _eeInstancedList;
        private Dictionary<string, List<StaticObject>> _instancedList;

        private Dictionary<string, string> _modelList;

        private PQSCity.LODRange _myLodRange;
        private float _prevRotationAngle;
        private Dictionary<string, List<StaticObject>> _ssInstancedList;

        private void Awake()
        {
            PopulateLists();
            InstantiateEasterEggs();
            InstantiateStaticsFromInstanceList();
            InstantiateStaticsFromSave();

            if (FlightGlobals.currentMainBody != null)
                _currentBodyName = FlightGlobals.currentMainBody.bodyName; //todo remove redundant code

            GameEvents.onDominantBodyChange.Add(OnDominantBodyChangeCallback);
            GameEvents.onFlightReady.Add(OnFlightReadyCallback);
            GameEvents.onGameStateSaved.Add(OnSave);
            GameEvents.onGameStateCreated.Add(OnLoad);

			DontDestroyOnLoad(this);
        }

        private void OnLoad(Game data)
        {
            foreach (KtComponent module in from ins in _instancedList
                from o in ins.Value
                where o.ModuleList != null
                from module in o.ModuleList
                where module.ModuleComponent.GetType() == typeof (StaticObjectModule)
                select module)
                ((StaticObjectModule) module.ModuleComponent).OnLoad(data);
        }

        private void OnSave(Game data)
        {
            foreach (KtComponent module in from ins in _instancedList
                from o in ins.Value
                where o.ModuleList != null
                from module in o.ModuleList
                where module.ModuleComponent.GetType() == typeof (StaticObjectModule)
                select module)
                ((StaticObjectModule) module.ModuleComponent).OnSave(data);
        }

        private void OnDestroy()
        {
            Extensions.LogInfo("Removing script references ..");

            GameEvents.onDominantBodyChange.Remove(OnDominantBodyChangeCallback);
            GameEvents.onFlightReady.Remove(OnFlightReadyCallback);
            GameEvents.onGameStateSaved.Remove(OnSave);
            GameEvents.onGameStateCreated.Remove(OnLoad);

            KtCamera.RestoreCameraParent();

			// Don't do anything...
			return;

            DestroyInstances(_instancedList);

            // Destroy Easter Eggs
            foreach (StaticObject i in _eeInstancedList.SelectMany(ins => ins.Value))
                DestroySoInstance(i);
        }

        private static void DestroyInstances(Dictionary<string, List<StaticObject>> instanceDictionary)
        {
			Extensions.LogInfo("Destoying Static Object Instances");
            foreach (StaticObject i in instanceDictionary.SelectMany(ins => ins.Value))
            {
                DestroySoInstance(i);
            }
        }

        // Create instanced lists.
        private void PopulateLists()
        {
            UrlDir.UrlConfig[] staticConfigs = GameDatabase.Instance.GetConfigs("STATIC");
			_staticPropertyList = new Dictionary<string, Dictionary<string, string>>();
            _instancedList = new Dictionary<string, List<StaticObject>>();
            _modelList = new Dictionary<string, string>();

            foreach (UrlDir.UrlConfig staticUrlConfig in staticConfigs)
            {
                // Skip easter eggs all together.
                if (staticUrlConfig.url.IndexOf("KerbTown/EE", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                string model = staticUrlConfig.config.GetValue("mesh");
                if (string.IsNullOrEmpty(model))
                {
                    Extensions.LogError("Missing 'mesh' parameter for " + staticUrlConfig.url);
                    continue;
                }

                model = model.Substring(0, model.LastIndexOf('.'));
                string modelUrl = staticUrlConfig.url.Substring(0, staticUrlConfig.url.SecondLastIndex('/')) + "/" +
                                  model;

                //Extensions.LogWarning("Model url: " + modelUrl);
                //Extensions.LogWarning("Config url: " + staticUrlConfig.url);

				if(_staticPropertyList.ContainsKey(modelUrl) == false)
					_staticPropertyList[modelUrl] = new Dictionary<string,string>();
				if(staticUrlConfig.config.HasValue("DefaultLaunchPadTransform"))
					_staticPropertyList[modelUrl]["DefaultLaunchPadTransform"] = staticUrlConfig.config.GetValue("DefaultLaunchPadTransform");

                // Skip adding the object if it is not yielding.
                //string isYielding = staticUrlConfig.config.GetValue("isYielding");

                //if (string.IsNullOrEmpty(isYielding) || isYielding != "0")
                _modelList.Add(modelUrl, staticUrlConfig.url);

                // If we already have previous instances of the object, fill up the lists so that KerbTown can start instantiating them
                if (!staticUrlConfig.config.HasNode("Instances"))
                    continue;

                foreach (ConfigNode ins in staticUrlConfig.config.GetNodes("Instances"))
                {
                    Vector3 radPosition = ConfigNode.ParseVector3(ins.GetValue("RadialPosition"));
                    float rotAngle = float.Parse(ins.GetValue("RotationAngle"));
                    float radOffset = float.Parse(ins.GetValue("RadiusOffset"));
                    Vector3 orientation = ConfigNode.ParseVector3(ins.GetValue("Orientation"));
                    float visRange = float.Parse(ins.GetValue("VisibilityRange"));
                    string celestialBodyName = ins.GetValue("CelestialBody");
                    string launchSiteName = ins.GetValue("LaunchSiteName") ?? "";
					string launchPadTransform = ins.GetValue("LaunchPadTransform") ?? "";

                    //string scaleStr = ""; //ins.GetValue("Scale");
                    //Vector3 scale = ConfigNode.ParseVector3(string.IsNullOrEmpty(scaleStr) ? "1,1,1" : scaleStr);

                    if (_instancedList.ContainsKey(modelUrl))
                    {
                        //_instancedList[modelUrl].Add(
                        //    new StaticObject(radPosition, rotAngle, radOffset, orientation,
                        //        visRange, modelUrl, staticUrlConfig.url, celestialBodyName, scale, "", launchSiteName));
                        _instancedList[modelUrl].Add(
                            new StaticObject(radPosition, rotAngle, radOffset, orientation,
                                visRange, modelUrl, staticUrlConfig.url, celestialBodyName, "", launchSiteName, launchPadTransform));
                    }
                    else
                    {
                        _instancedList.Add(modelUrl,
                            new List<StaticObject>
                            {
                                //new StaticObject(radPosition, rotAngle, radOffset, orientation,
                                //    visRange, modelUrl, staticUrlConfig.url, celestialBodyName, scale, "",
                                //    launchSiteName)
                                new StaticObject(radPosition, rotAngle, radOffset, orientation,
                                    visRange, modelUrl, staticUrlConfig.url, celestialBodyName, "",
                                    launchSiteName, launchPadTransform)
                            });
                    }
                }
            }
        }

        private void InstantiateStatic(PQS celestialPQS, StaticObject stObject, bool freshObject = false)
        {
            #region Staitc Object Core Parameters

            float visibilityRange = stObject.VisRange;
            float localRotationAngle = stObject.RotAngle;
            float radiusOffset = stObject.RadOffset;
            string modelUrl = stObject.ModelUrl;
            Vector3 orientDirection = stObject.Orientation;
            Vector3 radialPosition = stObject.RadPosition;

            if (radialPosition == Vector3.zero)
            {
                radialPosition =
                    _currentCelestialObj.CelestialBodyComponent.transform.InverseTransformPoint(
                        FlightGlobals.ActiveVessel.transform.position);

                stObject.RadPosition = radialPosition;
            }

            if (orientDirection == Vector3.zero)
            {
                orientDirection = Vector3.up;
                stObject.Orientation = orientDirection;
            }

            stObject.Latitude = GetLatitude(radialPosition);
            stObject.Longitude = GetLongitude(radialPosition);

            #endregion

            // Instantiate
            GameObject ktGameObject = GameDatabase.Instance.GetModel(modelUrl);

            // Add the reference component.
            var soModule = ktGameObject.AddComponent<StaticObjectModule>();

            // Active the game object.
            ktGameObject.SetActive(true);

            // Set objects to layer 15 so that they collide correctly with Kerbals.
            SetLayerRecursively(ktGameObject, 15);

            // Set the parent object to the celestial component's GameObject.
            ktGameObject.transform.parent = celestialPQS.transform;
			Debug.Log("Setting static game object's parent to: " + ktGameObject.transform.parent.name);

            // Obtain all active transforms in the static game object.
            Transform[] gameObjectList = ktGameObject.GetComponentsInChildren<Transform>();

            // Create a list of renderers to be manipulated by the default PQSCity class.
            List<GameObject> rendererList =
                (from t in gameObjectList where t.gameObject.renderer != null select t.gameObject).ToList();

            // Create the LOD range.
            _myLodRange = new PQSCity.LODRange
                          {
                              renderers = rendererList.ToArray(),
                              objects = new GameObject[0],
                              //new[] {staticGameObject},  // Todo: change to GameObject children.
                              visibleRange = visibilityRange
                          };

            // Add the PQSCity class (extended by KerbTown).
            var myCity = ktGameObject.AddComponent<PQSCityEx>();

            // Assign PQSCity variables.
            myCity.lod = new[] {_myLodRange};
            myCity.frameDelta = 1;
            myCity.repositionToSphere = true;
            myCity.repositionToSphereSurface = false;
            myCity.repositionRadial = radialPosition;
            myCity.repositionRadiusOffset = radiusOffset;
            myCity.reorientFinalAngle = localRotationAngle;
            myCity.reorientToSphere = true;
            myCity.reorientInitialUp = orientDirection;
            myCity.sphere = celestialPQS;
            myCity.order = 100;
            myCity.modEnabled = true;

            // Assign custom variables.
            myCity.StaticObjectRef = stObject;

            // Setup and orientate the PQSCity instanced object.
            myCity.OnSetup();
            myCity.Orientate();

            // If the object was instantiated by "Create", override all renderers to active.
            if (freshObject)
            {
                foreach (GameObject renObj in rendererList)
                    renObj.renderer.enabled = true;
            }

            // Add component references to the static object.
            stObject.PQSCityComponent = myCity;
            stObject.StaticGameObject = ktGameObject;
            //stObject.ModuleReference = soModule;

            // Add the static object as a reference to the StaticObjectModule
            soModule.StaticObjectRef = stObject;

            // Add remaining modules.
            switch (stObject.ObjectID)
            {
                case "MushroomCave":
                    AddNativeComponent(ktGameObject, typeof (MushroomCave));
                    break;

                case "PurplePathway":
                    AddNativeComponent(ktGameObject, typeof (PurplePathway));
                    break;

                default:
                    AddModuleComponents(stObject);
                    break;
            }

            // Alter the Launch Site spawn object name if necessary.
            // Todo: optimize
            if (stObject.LaunchSiteName != "")
            {
                if(stObject.LaunchPadTransform == "./" || ktGameObject.transform.Find(stObject.LaunchPadTransform) != null)
				{
					ktGameObject.transform.name = stObject.LaunchSiteName;
					ktGameObject.name = stObject.LaunchSiteName;
					Debug.Log("Launch pad transform found: " + stObject.LaunchPadTransform + ", object renamed to: " + ktGameObject.name);

					// Need to update PSystemSetup.Instance.LaunchSites as well.
					foreach (FieldInfo fi in PSystemSetup.Instance.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
					{
						if (fi.FieldType.Name == "LaunchSite[]")
						{
							Debug.Log("Acquiring launch sites.");
							PSystemSetup.LaunchSite[] sites = (PSystemSetup.LaunchSite[])fi.GetValue(PSystemSetup.Instance);
							if (sites == null)
								Debug.Log("Fail to acquire launch sites.");

							if (PSystemSetup.Instance.GetLaunchSite(stObject.LaunchSiteName) == null)
							{
								PSystemSetup.LaunchSite newSite = new PSystemSetup.LaunchSite();
								if (stObject.LaunchPadTransform == "./")
									newSite.launchPadName = stObject.LaunchSiteName;
								else
									newSite.launchPadName = stObject.LaunchSiteName + "/" + stObject.LaunchPadTransform;
								newSite.name = stObject.LaunchSiteName;
								newSite.pqsName = stObject.CelestialBodyName;
								PSystemSetup.LaunchSite[] newSites = new PSystemSetup.LaunchSite[sites.Length + 1];
								for (int i = 0; i < sites.Length; ++i)
								{
									newSites[i] = sites[i];
								}
								newSites[newSites.Length - 1] = newSite;
								fi.SetValue(PSystemSetup.Instance, newSites);
								sites = newSites;
							}
							else
							{
								Debug.Log("Launch site " + stObject.LaunchSiteName + " already exists.");
							}
							break;
						}
					}
					// Now update the sites again.
					MethodInfo updateSitesMI = PSystemSetup.Instance.GetType().GetMethod("SetupLaunchSites", BindingFlags.NonPublic | BindingFlags.Instance);
					if (updateSitesMI == null)
						Debug.Log("Fail to find SetupLaunchSites().");
					else
						updateSitesMI.Invoke(PSystemSetup.Instance, null);
                }
                else
                {
                    Extensions.LogWarning("Launch Site '" + ktGameObject.name + "'does not have the defined spawn transform.");
                }
            }
        }

        private void InstantiateEasterEggs()
        {
            var configDict = new Dictionary<string, string>
                             {
                                 {"Hubs/Static/KerbTown/EE01/EE01", "MushroomCave"},
                                 {"Hubs/Static/KerbTown/EE02/EE02", "PurplePathway"}
                             };

            _eeInstancedList = new Dictionary<string, List<StaticObject>>(configDict.Count);

            foreach (var configItem in configDict)
            {
                ConfigNode staticUrlConfig = GameDatabase.Instance.GetConfigNode(configItem.Key);

                if (staticUrlConfig == null)
                    continue;

                string model = staticUrlConfig.GetValue("mesh");
                if (string.IsNullOrEmpty(model))
                {
                    Extensions.LogError("Missing 'mesh' parameter for " + configItem);
                    continue;
                }

                model = model.Substring(0, model.LastIndexOf('.'));
                string modelUrl = configItem.Key.Substring(0, configItem.Key.SecondLastIndex('/')) + "/" + model;

                // TODO: Instantiate through code rather than config.
                foreach (ConfigNode ins in staticUrlConfig.GetNodes("Instances"))
                {
                    Vector3 radPosition = ConfigNode.ParseVector3(ins.GetValue("RadialPosition"));
                    float rotAngle = float.Parse(ins.GetValue("RotationAngle"));
                    float radOffset = float.Parse(ins.GetValue("RadiusOffset"));
                    Vector3 orientation = ConfigNode.ParseVector3(ins.GetValue("Orientation"));
                    float visRange = float.Parse(ins.GetValue("VisibilityRange"));
                    string celestialBodyName = ins.GetValue("CelestialBody");

                    //string scaleStr = "";//ins.GetValue("Scale");
                    //Vector3 scale = ConfigNode.ParseVector3(string.IsNullOrEmpty(scaleStr) ? "1,1,1" : scaleStr);

                    //var staticObject = new StaticObject(radPosition, rotAngle, radOffset, orientation,
                    //    visRange, modelUrl, configItem.Key, celestialBodyName, scale, configItem.Value);
                    var staticObject = new StaticObject(radPosition, rotAngle, radOffset, orientation,
                        visRange, modelUrl, configItem.Key, celestialBodyName, configItem.Value);

                    if (_eeInstancedList.ContainsKey(modelUrl))
                        _instancedList[modelUrl].Add(staticObject);
                    else
                        _eeInstancedList.Add(modelUrl, new List<StaticObject> {staticObject});

                    InstantiateStatic(
                        (_currentCelestialObj = GetCelestialObject(staticObject.CelestialBodyName)).PQSComponent,
                        staticObject);
                }
            }
        }

        // Load Global Instances
        private void InstantiateStaticsFromInstanceList()
        {
            Stopwatch stopWatch = Stopwatch.StartNew();

            foreach (StaticObject instance in _instancedList.Keys.SelectMany(instList => _instancedList[instList]))
            {
                InstantiateStatic((_currentCelestialObj = GetCelestialObject(instance.CelestialBodyName)).PQSComponent,
                    instance);
            }

            stopWatch.Stop();
            Extensions.LogInfo(string.Format("Loaded static objects. ({0}ms)", stopWatch.ElapsedMilliseconds));
        }

        // Load Save Instances
        private void InstantiateStaticsFromSave()
        {
            string saveConfigPath;

            if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
            {
                saveConfigPath = string.Format("{0}saves/{1}/KTInstances.cfg", KSPUtil.ApplicationRootPath,
                    HighLogic.SaveFolder);
            }
            else // Career / Scenario
            {
                saveConfigPath = string.Format("{0}saves/scenarios/KT_{1}.cfg", KSPUtil.ApplicationRootPath,
                    HighLogic.CurrentGame.Title);
            }

            if (!File.Exists(saveConfigPath)) return;

            ConfigNode rootNode = ConfigNode.Load(saveConfigPath);
            _ssInstancedList = new Dictionary<string, List<StaticObject>>();

            foreach (ConfigNode ins in rootNode.GetNodes("Instances"))
            {
                Vector3 radPosition = ConfigNode.ParseVector3(ins.GetValue("RadialPosition"));
                float rotAngle = float.Parse(ins.GetValue("RotationAngle"));
                float radOffset = float.Parse(ins.GetValue("RadiusOffset"));
                Vector3 orientation = ConfigNode.ParseVector3(ins.GetValue("Orientation"));
                float visRange = float.Parse(ins.GetValue("VisibilityRange"));
                string celestialBodyName = ins.GetValue("CelestialBody");
                string launchSiteName = ins.GetValue("LaunchSiteName") ?? "";

                string modelUrl = ins.GetValue("ModelURL");
                string configUrl = ins.GetValue("ConfigURL");

                if (string.IsNullOrEmpty(modelUrl) || string.IsNullOrEmpty(configUrl)) continue;

                if (_ssInstancedList.ContainsKey(modelUrl))
                {
                    _ssInstancedList[modelUrl].Add(
                        new StaticObject(radPosition, rotAngle, radOffset, orientation,
                            visRange, modelUrl, configUrl, celestialBodyName, "", launchSiteName));
                }
                else
                {
                    _ssInstancedList.Add(modelUrl,
                        new List<StaticObject>
                        {
                            new StaticObject(radPosition, rotAngle, radOffset, orientation,
                                visRange, modelUrl, configUrl, celestialBodyName, "",
                                launchSiteName)
                        });
                }
            }

            //_ssInstancedList = new Dictionary<string, List<StaticObject>>();
            Stopwatch stopWatch = Stopwatch.StartNew();

            foreach (StaticObject instance in _ssInstancedList.Keys.SelectMany(instList => _ssInstancedList[instList]))
            {
                InstantiateStatic((_currentCelestialObj = GetCelestialObject(instance.CelestialBodyName)).PQSComponent,
                    instance);
            }

            stopWatch.Stop();
            Extensions.LogInfo(string.Format("Loaded save specific static objects. ({0}ms)",
                stopWatch.ElapsedMilliseconds));
        }

        // Save
        private void SaveInstances()
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            ConfigNode modelPartRootNode = null;

            foreach (string instList in _instancedList.Keys)
            {
                var staticNode = new ConfigNode("STATIC");
                string modelPhysPath = "";
                bool nodesCleared = false;

                foreach (StaticObject inst in _instancedList[instList])
                {
                    if (!nodesCleared)
                    {
                        // Assign the root node for this static part.
                        modelPartRootNode = GameDatabase.Instance.GetConfigNode(inst.ConfigURL);

                        // Assign physical path to object config.
                        modelPhysPath = inst.ConfigURL.Substring(0, inst.ConfigURL.LastIndexOf('/')) + ".cfg";

                        // Remove existing nodes.
                        modelPartRootNode.RemoveNodes("Instances");

                        // Skip this until next static part.
                        nodesCleared = true;
                    }

                    var instanceNode = new ConfigNode("Instances");

                    instanceNode.AddValue("RadialPosition", ConfigNode.WriteVector(inst.RadPosition));
                    instanceNode.AddValue("RotationAngle", inst.RotAngle.ToString(CultureInfo.InvariantCulture));
                    instanceNode.AddValue("RadiusOffset", inst.RadOffset.ToString(CultureInfo.InvariantCulture));
                    instanceNode.AddValue("Orientation", ConfigNode.WriteVector(inst.Orientation));
                    instanceNode.AddValue("VisibilityRange", inst.VisRange.ToString(CultureInfo.InvariantCulture));
                    instanceNode.AddValue("CelestialBody", inst.CelestialBodyName);
					instanceNode.AddValue("LaunchSiteName", inst.LaunchSiteName);
					instanceNode.AddValue("LaunchPadTransform", inst.LaunchPadTransform);

                    //instanceNode.AddValue("Scale", ConfigNode.WriteVector(inst.Scale));

                    modelPartRootNode.nodes.Add(instanceNode);
                }

                // No current instances - find the config url that is paired with the model url.

                if (_instancedList[instList].Count == 0)
                {
                    modelPartRootNode = GameDatabase.Instance.GetConfigNode(_modelList[instList]);
                    modelPhysPath = _modelList[instList].Substring(0, _modelList[instList].LastIndexOf('/')) + ".cfg";
                    modelPartRootNode.RemoveNodes("Instances");
                }

                staticNode.AddNode(modelPartRootNode);
                staticNode.Save(KSPUtil.ApplicationRootPath + "GameData/" + modelPhysPath,
                    " Generated by KerbTown - Hubs' Electrical");
            }

            stopWatch.Stop();
            Extensions.LogInfo(string.Format("Saved static objects. ({0}ms)", stopWatch.ElapsedMilliseconds));
        }

        private void WritePersistence(bool deleteInstead = false)
        {
            string saveConfigPath;

            if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
            {
                saveConfigPath = string.Format("{0}saves/{1}/KTInstances.cfg", KSPUtil.ApplicationRootPath,
                    HighLogic.SaveFolder);
            }
            else // Career / Scenario
            {
                saveConfigPath = string.Format("{0}saves/scenarios/KT_{1}.cfg", KSPUtil.ApplicationRootPath,
                    HighLogic.CurrentGame.Title);
            }

            if (deleteInstead)
            {
                DestroyInstances(_ssInstancedList);
                File.Delete(saveConfigPath);
                return;
            }

            Stopwatch stopWatch = Stopwatch.StartNew();

            var staticNode = new ConfigNode("KtStaticPersistence");
            foreach (string instList in _instancedList.Keys)
            {
                foreach (StaticObject inst in _instancedList[instList])
                {
                    var instanceNode = new ConfigNode("Instances");

                    instanceNode.AddValue("RadialPosition", ConfigNode.WriteVector(inst.RadPosition));
                    instanceNode.AddValue("RotationAngle", inst.RotAngle.ToString(CultureInfo.InvariantCulture));
                    instanceNode.AddValue("RadiusOffset", inst.RadOffset.ToString(CultureInfo.InvariantCulture));
                    instanceNode.AddValue("Orientation", ConfigNode.WriteVector(inst.Orientation));
                    instanceNode.AddValue("VisibilityRange", inst.VisRange.ToString(CultureInfo.InvariantCulture));
                    instanceNode.AddValue("CelestialBody", inst.CelestialBodyName);
					instanceNode.AddValue("LaunchSiteName", inst.LaunchSiteName);
					instanceNode.AddValue("LaunchPadTransform", inst.LaunchPadTransform);

                    instanceNode.AddValue("ModelURL", inst.ModelUrl);
                    instanceNode.AddValue("ConfigURL", inst.ConfigURL);

                    staticNode.nodes.Add(instanceNode);
                }
            }

            staticNode.Save(saveConfigPath, " Generated by KerbTown - Hubs' Electrical");

            stopWatch.Stop();
            Extensions.LogInfo(string.Format("Saved static objects. [*] ({0}ms)", stopWatch.ElapsedMilliseconds));
        }

        private void Update()
        {
            // CTRL + K for show/hide.
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.K))
                {
                    _mainWindowVisible = !_mainWindowVisible;
                }
            }

            if (_mainWindowVisible)
            {
                _deletePersistence = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            }
        }

/*
        private static Vector3 GetLocalPosition(CelestialBody celestialObject, double latitude, double longitude)
        {
            return Vector3.zero;
        }
*/

        private static double GetLongitude(Vector3d radialPosition)
        {
            Vector3d norm = radialPosition.normalized;
            double longitude = Math.Atan2(norm.z, norm.x)*57.295780181884766 + 180; // Todo: Recheck validity.
            return (!double.IsNaN(longitude) ? longitude : 0.0);
        }

        private static double GetLatitude(Vector3d radialPosition)
        {
            double latitude = Math.Asin(radialPosition.normalized.y)*57.295780181884766;
            return (!double.IsNaN(latitude) ? latitude : 0.0);
        }

		private static Vector3d GetRadialPosition(double lat, double lon)
		{
			double x, y, z;
			x = Math.Cos(lat / 57.295780181884766) * Math.Cos((lon - 180) / 57.295780181884766) * 100000.0;
			y = Math.Sin(lat / 57.295780181884766) * 100000.0;
			z = Math.Cos(lat / 57.295780181884766) * Math.Sin((lon - 180) / 57.295780181884766) * 100000.0;
			return new Vector3d(x, y, z);
		}

        private static void SetLayerRecursively(GameObject sGameObject, int newLayerNumber)
        {
            // Only set to layer 'newLayerNumber' if the collider is not a trigger.
            if ((sGameObject.collider != null &&
                 sGameObject.collider.enabled &&
                 !sGameObject.collider.isTrigger) || sGameObject.collider == null)
            {
                sGameObject.layer = newLayerNumber;
            }

            foreach (Transform child in sGameObject.transform)
            {
                SetLayerRecursively(child.gameObject, newLayerNumber);
            }
        }

        private CelestialObject GetCelestialObject(string celestialName)
        {
            if (_currentCelestialObj != null &&
                (_currentCelestialObj.CelestialBodyComponent != null &&
                 _currentCelestialObj.CelestialBodyComponent.bodyName == celestialName))
                return _currentCelestialObj;

            return (from PQS gameObjectInScene in FindObjectsOfType(typeof (PQS))
                where gameObjectInScene.name == celestialName
                select new CelestialObject(gameObjectInScene.transform.parent.gameObject)).FirstOrDefault();
        }

        private StaticObject GetDefaultStaticObject(string modelUrl, string configUrl)
        {
            // 150000f is flightcamera max distance
            // Space Center clips at about 80-90km
            return new
                StaticObject(Vector3.zero, 0, GetSurfaceRadiusOffset(), Vector3.up, 100000, modelUrl, configUrl, "");
        }

        private float GetSurfaceRadiusOffset()
        {
            // Todo: change to activevessel.altitude after further testing or just to surface height..

            Vector3d relativePosition =
                _currentCelestialObj.PQSComponent.GetRelativePosition(FlightGlobals.ActiveVessel.GetWorldPos3D());
            Vector3d rpNormalized = relativePosition.normalized;

            return (float) (relativePosition.x/rpNormalized.x - _currentCelestialObj.PQSComponent.radius);
        }

        private static void DestroySoInstance(StaticObject staticObject)
        {
            try
            {
				Extensions.LogInfo("Disabling PQSCityComponent");
                staticObject.PQSCityComponent.modEnabled = false;

				Extensions.LogInfo("Destroying PQSCityComponent LODs");
                foreach (PQSCity.LODRange lod in staticObject.PQSCityComponent.lod)
                {
                    lod.SetActive(false);

                    foreach (GameObject lodren in lod.renderers)
                        Destroy(lodren);

                    foreach (GameObject lodobj in lod.objects)
                        Destroy(lodobj);
                }

                if (staticObject.ModuleList != null)
				{
					Extensions.LogInfo("Unloading Static Object Modules");
                    foreach (KtComponent module in staticObject.ModuleList.Where
                        (module => module.ModuleComponent.GetType() == typeof (StaticObjectModule)))
                    {
                        ((StaticObjectModule) module.ModuleComponent).OnUnload();
                    }
                }

				Extensions.LogInfo("Unparent GameObject");
                staticObject.StaticGameObject.transform.parent = null;

				Extensions.LogInfo("Destoying PQSCityComponent and GameObject");
                Destroy(staticObject.PQSCityComponent);
                Destroy(staticObject.StaticGameObject);
            }
            catch (Exception ex)
            {
                Extensions.LogError("An exception was caught while destroying a static object.");
                Debug.LogException(ex);
            }
        }

        private static void InvokeSetup(StaticObject staticObject)
        {
            if (staticObject.ModuleList == null)
                return;

            // Loop incase the content creator has decided to add multiple SOM's.
            foreach (KtComponent module in staticObject.ModuleList.Where
                (module => module.ModuleComponent.GetType() == typeof (StaticObjectModule)))
            {
                ((StaticObjectModule) module.ModuleComponent).OnFirstSetup();
            }
        }

        #region Static Components

        private void AddModuleComponents(StaticObject staticObject)
        {
            Stopwatch stopWatch = Stopwatch.StartNew();

            string rootNodeUrl = staticObject.ConfigURL;

            ConfigNode rootNode = GameDatabase.Instance.GetConfigNode(rootNodeUrl);
            IEnumerator nodeEnum = rootNode.nodes.GetEnumerator();

            while (nodeEnum.MoveNext())
            {
                var currentNode = (ConfigNode) nodeEnum.Current;
                if (currentNode == null) continue;

                name = currentNode.name;
                if (name == null) continue;

                var moduleTypes = new Dictionary<string, int> {{"MODULE", 0}, {"RESOURCE", 1}, {"RIGIDBODY", 2}};

                int nodeType;
                if (!moduleTypes.TryGetValue(name, out nodeType))
                    continue;

                switch (nodeType)
                {
                    case 0: // Module
                        AddModule(currentNode, staticObject);
                        break;

                    case 1: // Resource
                        // Not used any more.
                        break;

                    case 2: // Rigidbody
                        AddRigidBody(currentNode, staticObject.StaticGameObject);
                        break;
                }
            }

            stopWatch.Stop();
            Extensions.LogInfo("Modules loaded for " + staticObject.NameID + ". (" + stopWatch.ElapsedMilliseconds +
                               "ms)");
        }

        private static void AddModule(ConfigNode configNode, StaticObject staticObject)
        {
            string namespaceName = configNode.GetValue("namespace");
            string className = configNode.GetValue("name");

            if (string.IsNullOrEmpty(namespaceName) || string.IsNullOrEmpty(className))
            {
                Extensions.LogError(
                    string.Format(
                        "Could not add a module to the static object because the node has no name{0} parameter.",
                        string.IsNullOrEmpty(namespaceName) ? "space" : ""));
                return;
            }

            if (namespaceName == "KerbTown")
            {
                AddNativeComponent(staticObject.StaticGameObject, configNode, className);
                return;
            }

            Type moduleClass =
                AssemblyLoader.loadedAssemblies.SelectMany(asm => asm.assembly.GetTypes())
                    .FirstOrDefault(t => t.Namespace == namespaceName && t.Name == className);

            if (moduleClass == null)
            {
                Extensions.LogError("Could not obtain module of type \"" + namespaceName + "." + className +
                                    "\" from AssemblyLoader.");
                return;
            }

            var moduleComponent = staticObject.StaticGameObject.AddComponent(moduleClass) as MonoBehaviour;

            if (moduleComponent == null)
            {
                Extensions.LogError("Could not add the obtained module \"" + moduleClass.Name +
                                    "\" to the static game object.");
                return;
            }

            // Assign variables specified in the config for the module.
            AssignVariables(configNode, moduleComponent);

            // Add the module to the module list. Creating the list if it hasn't been created already.
            if (staticObject.ModuleList == null) staticObject.ModuleList = new List<KtComponent>();
            staticObject.ModuleList.Add(new KtComponent(moduleComponent));
        }

        private static void AddNativeComponent(GameObject staticGameObject, Type classType)
        {
            staticGameObject.AddComponent(classType);
        }

        private static void AddNativeComponent(GameObject staticGameObject, ConfigNode configNode, string className)
        {
            switch (className)
            {
                case "Ladder":
                    string ladderObjectName = configNode.GetValue("name");
                    if (string.IsNullOrEmpty(ladderObjectName))
                    {
                        Extensions.LogError("The GenericLadder component requires the 'name' field to be set.");
                        return;
                    }

                    var genericLadder = staticGameObject.AddComponent<GenericLadder>();
                    genericLadder.ObjectName = ladderObjectName;
                    genericLadder.Setup();
                    break;

                case "TimeOfDayController":
                    var todController = staticGameObject.AddComponent<TimeOfDayController>();
                    //Todo add variables for config entries.

                    break;

                case "AnimateOnCollision":
                case "AnimateOnClick":
                    string objectName = configNode.GetValue("collider");
                    string animName = configNode.GetValue("animationName");

                    if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(animName))
                    {
                        Extensions.LogError(string.Format("GenericAnimation is missing the '{0}' parameter.",
                            string.IsNullOrEmpty(objectName) ? "collider" : "animationName"));
                        return;
                    }

                    bool shouldHighlight;
                    float animationSpeed;

                    var genericAnimationModule = staticGameObject.AddComponent<GenericAnimation>();

                    if (bool.TryParse(configNode.GetValue("HighlightOnHover"), out shouldHighlight))
                        genericAnimationModule.HighlightOnHover = shouldHighlight;

                    if (float.TryParse(configNode.GetValue("animationSpeed"), out animationSpeed))
                        genericAnimationModule.AnimationSpeed = animationSpeed;

                    genericAnimationModule.ClassName = className;
                    genericAnimationModule.AnimationName = animName;
                    genericAnimationModule.ObjectName = objectName;

                    genericAnimationModule.Setup();
                    break;

                default:
                    Extensions.LogWarning("KerbTown." + className + " does not exist or is not accessible.");
                    Extensions.LogWarning(
                        "The 'name' parameter should be either: 'AnimateOnCollision' or 'AnimateOnClick'.");
                    break;
            }
        }

        private static void AddRigidBody(ConfigNode currentNode, GameObject staticGameObject)
        {
            string objectName = currentNode.GetValue("name");
            if (string.IsNullOrEmpty(objectName))
            {
                Extensions.LogError(
                    "The 'name' parameter is empty. You must specify the GameObject name for a Rigidbody component to be added.");
                return;
            }

            float fVal;
            float rbMass = 1f;
            float rbDrag = 0f;
            float rbAngularDrag = 0.05f;
            bool rbUseGravity;
            bool rbIsKinematic;
            RigidbodyInterpolation rbInterpolation;
            CollisionDetectionMode rbCollisionDetectionMode;

            if (float.TryParse(currentNode.GetValue("mass"), out fVal))
                rbMass = fVal;

            if (float.TryParse(currentNode.GetValue("drag"), out fVal))
                rbDrag = fVal;

            if (float.TryParse(currentNode.GetValue("angularDrag"), out fVal))
                rbAngularDrag = fVal;

            if (!bool.TryParse(currentNode.GetValue("useGravity"), out rbUseGravity))
                rbUseGravity = false; // Failed, set default.

            if (!bool.TryParse(currentNode.GetValue("isKinematic"), out rbIsKinematic))
                rbIsKinematic = true; // Failed, set default.

            switch (currentNode.GetValue("interpolationMode"))
            {
                case "Extrapolate":
                    rbInterpolation = RigidbodyInterpolation.Extrapolate;

                    break;
                case "Interpolate":
                    rbInterpolation = RigidbodyInterpolation.Interpolate;
                    break;

                default:
                    rbInterpolation = RigidbodyInterpolation.None;
                    break;
            }

            switch (currentNode.GetValue("collisionDetectionMode"))
            {
                case "ContinuousDynamic":
                    rbCollisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                    break;
                case "Continuous":
                    rbCollisionDetectionMode = CollisionDetectionMode.Continuous;
                    break;

                default:
                    rbCollisionDetectionMode = CollisionDetectionMode.Discrete;
                    break;
            }

            // Recursive
            AddRigidBodies(staticGameObject, objectName, rbMass, rbDrag, rbAngularDrag, rbUseGravity,
                rbIsKinematic, rbInterpolation, rbCollisionDetectionMode);
        }

        private static void AddRigidBodies(GameObject sGameObject, string gameObjectName,
            float rbMass, float rbDrag, float rbAngularDrag, bool rbUseGravity,
            bool rbIsKinematic, RigidbodyInterpolation rbInterpolation,
            CollisionDetectionMode rbCollisionDetectionMode)
        {
            if (sGameObject.name == gameObjectName)
            {
                var rigidBody = sGameObject.AddComponent<Rigidbody>();
                rigidBody.isKinematic = rbIsKinematic;
                rigidBody.useGravity = rbUseGravity;
                rigidBody.mass = rbMass;
                rigidBody.drag = rbDrag;
                rigidBody.angularDrag = rbAngularDrag;
                rigidBody.interpolation = rbInterpolation;
                rigidBody.collisionDetectionMode = rbCollisionDetectionMode;
            }

            foreach (Transform childTransform in sGameObject.transform)
            {
                AddRigidBodies(childTransform.gameObject, gameObjectName, rbMass, rbDrag, rbAngularDrag, rbUseGravity,
                    rbIsKinematic, rbInterpolation, rbCollisionDetectionMode);
            }
        }

        private static void AssignVariables(ConfigNode configNode, MonoBehaviour moduleComponent)
        {
            IEnumerator enumerator = configNode.values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var current = (ConfigNode.Value) enumerator.Current;

                if (current == null || current.name == "name" || current.name == "namespace")
                    continue;

                FieldInfo field = moduleComponent.GetType().GetField(current.name);
                if (field == null)
                {
                    Extensions.LogWarning("Could not find the '" + current.name + "' field.");
                    continue;
                }

                field.SetValue(moduleComponent, ParseValue(current.value, field));
            }
        }

        private static object ParseValue(string value, FieldInfo field)
        {
            if (!field.FieldType.IsValueType)
            {
                if (field.FieldType == typeof (string))
                    return value;
            }
            else
            {
                try
                {
                    if (field.FieldType == typeof (sbyte)) return sbyte.Parse(value);
                    if (field.FieldType == typeof (short)) return short.Parse(value);
                    if (field.FieldType == typeof (int)) return int.Parse(value);
                    if (field.FieldType == typeof (long)) return long.Parse(value);
                    if (field.FieldType == typeof (byte)) return byte.Parse(value);
                    if (field.FieldType == typeof (ushort)) return ushort.Parse(value);
                    if (field.FieldType == typeof (uint)) return uint.Parse(value);
                    if (field.FieldType == typeof (ulong)) return ulong.Parse(value);
                    if (field.FieldType == typeof (float)) return float.Parse(value);
                    if (field.FieldType == typeof (double)) return double.Parse(value);
                    if (field.FieldType == typeof (decimal)) return decimal.Parse(value);
                    if (field.FieldType == typeof (char)) return char.Parse(value);
                    if (field.FieldType == typeof (bool)) return bool.Parse(value);

                    char[] separators = {',', ' ', '\t'};
                    string[] stringArray = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                    if (field.FieldType == typeof (Vector2))
                    {
                        if (stringArray.Length != 2) throw new Exception("Vector2 not formatted properly.");
                        return new Vector2(float.Parse(stringArray[0]), float.Parse(stringArray[1]));
                    }
                    if (field.FieldType == typeof (Vector2d))
                    {
                        if (stringArray.Length != 2) throw new Exception("Vector2d not formatted properly.");
                        return new Vector2d(double.Parse(stringArray[0]), double.Parse(stringArray[1]));
                    }
                    if (field.FieldType == typeof (Vector3))
                    {
                        if (stringArray.Length != 3) throw new Exception("Vector3 not formatted properly.");
                        return new Vector3(float.Parse(stringArray[0]), float.Parse(stringArray[1]),
                            float.Parse(stringArray[2]));
                    }
                    if (field.FieldType == typeof (Vector3d))
                    {
                        if (stringArray.Length != 3) throw new Exception("Vector3d not formatted properly.");
                        return new Vector3d(double.Parse(stringArray[0]), double.Parse(stringArray[1]),
                            double.Parse(stringArray[2]));
                    }
                    if (field.FieldType == typeof (Vector4))
                    {
                        if (stringArray.Length != 4) throw new Exception("Vector4 not formatted properly.");
                        return new Vector4(float.Parse(stringArray[0]), float.Parse(stringArray[1]),
                            float.Parse(stringArray[2]), float.Parse(stringArray[3]));
                    }
                    if (field.FieldType == typeof (Vector4d))
                    {
                        if (stringArray.Length != 4) throw new Exception("Vector4d not formatted properly.");
                        return new Vector4d(double.Parse(stringArray[0]), double.Parse(stringArray[1]),
                            double.Parse(stringArray[2]), double.Parse(stringArray[3]));
                    }
                    if (field.FieldType == typeof (Quaternion))
                    {
                        if (stringArray.Length != 4) throw new Exception("Quaternion not formatted properly.");
                        return new Quaternion(float.Parse(stringArray[0]), float.Parse(stringArray[1]),
                            float.Parse(stringArray[2]), float.Parse(stringArray[3]));
                    }
                    if (field.FieldType == typeof (QuaternionD))
                    {
                        if (stringArray.Length != 4) throw new Exception("QuaternionD not formatted properly.");
                        return new QuaternionD(double.Parse(stringArray[0]), double.Parse(stringArray[1]),
                            double.Parse(stringArray[2]), double.Parse(stringArray[3]));
                    }
                    if (field.FieldType == typeof (Color))
                    {
                        if (stringArray.Length == 4 || stringArray.Length == 3)
                        {
                            return new Color(float.Parse(stringArray[0]), float.Parse(stringArray[1]),
                                float.Parse(stringArray[2]), stringArray.Length == 4 ? float.Parse(stringArray[3]) : 1);
                        }
                        throw new Exception("Color not formatted properly.");
                    }
                    if (field.FieldType == typeof (Color32))
                    {
                        if (stringArray.Length == 4 || stringArray.Length == 3)
                        {
                            return new Color32(byte.Parse(stringArray[0]), byte.Parse(stringArray[1]),
                                byte.Parse(stringArray[2]),
                                stringArray.Length == 4 ? byte.Parse(stringArray[3]) : (byte) 255);
                        }
                        throw new Exception("Color32 not formatted properly.");
                    }

                    if (field.FieldType.IsEnum) return Enum.Parse(field.FieldType, value);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    Extensions.LogError(
                        string.Format(
                            "Could not parse the value '{0}' for '{1}' as '{2}'. It may have been incorrectly formatted.",
                            value,
                            field.Name, field.FieldType.Name));
                }
            }

            return null;
        }

        #endregion

        #region Callbacks

        private void OnDominantBodyChangeCallback(GameEvents.FromToAction<CelestialBody, CelestialBody> data)
        {
            _currentBodyName = data.to.bodyName;
        }

        private void OnFlightReadyCallback()
        {
            _currentBodyName = FlightGlobals.currentMainBody.bodyName;
        }

        #endregion

        private class CelestialObject
        {
            public readonly CelestialBody CelestialBodyComponent;
            public readonly GameObject CelestialGameObject;
            public readonly PQS PQSComponent;

            public CelestialObject(GameObject parentObject)
            {
                CelestialGameObject = parentObject;
                CelestialBodyComponent = parentObject.GetComponentInChildren<CelestialBody>();
                PQSComponent = parentObject.GetComponentInChildren<PQS>();

                if (CelestialBodyComponent == null)
                    Extensions.LogError("Could not obtain CelestialBody component from: " + parentObject.name);

                if (PQSComponent == null)
                    Extensions.LogError("Could not obtain PQS component from: " + parentObject.name);
            }
        }
    }

	    /// <summary>
    /// KSPAddon with equality checking using an additional type parameter. Fixes the issue where AddonLoader prevents multiple start-once addons with the same start scene.
    /// By Majiir
    /// </summary>
    public class KSPAddonFixed : KSPAddon, IEquatable<KSPAddonFixed>
    {
        private readonly Type type;

        public KSPAddonFixed(KSPAddon.Startup startup, bool once, Type type)
            : base(startup, once)
        {
            this.type = type;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != this.GetType()) { return false; }
            return Equals((KSPAddonFixed)obj);
        }

        public bool Equals(KSPAddonFixed other)
        {
            if (this.once != other.once) { return false; }
            if (this.startup != other.startup) { return false; }
            if (this.type != other.type) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            return this.startup.GetHashCode() ^ this.once.GetHashCode() ^ this.type.GetHashCode();
        }
    }
}

