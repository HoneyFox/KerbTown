/* LICENSE
 * This work is licensed under the Creative Commons Attribution-NoDerivs 3.0 Unported License. 
 * To view a copy of this license, visit http://creativecommons.org/licenses/by-nd/3.0/ or send a letter to Creative Commons, 444 Castro Street, Suite 900, Mountain View, California, 94041, USA.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Kerbtown.EEComponents;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace Kerbtown
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public partial class KerbTown : MonoBehaviour
    {
        private string _currentBodyName = "";
        private CelestialObject _currentCelestialObj;
        private string _currentConfigUrl = "";
        private string _currentModelUrl = "";
        private StaticObject _currentSelectedObject;

        private Dictionary<string, List<StaticObject>> _instancedList; // Initialized OnStart()
        private Dictionary<string, string> _modelList;

        private PQSCity.LODRange _myLodRange;
        private float _prevRotationAngle;

        private void Start()
        {
            GenerateModelLists();
            InstantiateEasterEggs();
            InstantiateStaticsFromInstanceList();

            _currentBodyName = FlightGlobals.currentMainBody.bodyName;
            GameEvents.onDominantBodyChange.Add(BodyChangedCallback);
            GameEvents.onFlightReady.Add(FlightReadyCallBack);
        }

        // todo remove code clones
        private void InstantiateEasterEggs()
        {
            var configDict = new Dictionary<string, string>
                             {
                                 {"Hubs/Static/KerbTown/EE01/EE01", "MushroomCave"}
                             };

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

                foreach (ConfigNode ins in staticUrlConfig.GetNodes("Instances"))
                {
                    Vector3 radPosition = ConfigNode.ParseVector3(ins.GetValue("RadialPosition"));
                    float rotAngle = float.Parse(ins.GetValue("RotationAngle"));
                    float radOffset = float.Parse(ins.GetValue("RadiusOffset"));
                    Vector3 orientation = ConfigNode.ParseVector3(ins.GetValue("Orientation"));
                    float visRange = float.Parse(ins.GetValue("VisibilityRange"));
                    string celestialBodyName = ins.GetValue("CelestialBody");

                    var staticObject = new StaticObject(radPosition, rotAngle, radOffset, orientation,
                        visRange, modelUrl, configItem.Key, celestialBodyName, configItem.Value);

                    InstantiateStatic(
                        (_currentCelestialObj = GetCelestialObject(staticObject.CelestialBodyName)).PQSComponent,
                        staticObject);
                }
            }
        }

        // Load
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

        // Save
        private void WriteSessionConfigs()
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

        private void FlightReadyCallBack()
        {
            _currentBodyName = FlightGlobals.currentMainBody.bodyName;
        }

        private void BodyChangedCallback(GameEvents.FromToAction<CelestialBody, CelestialBody> data)
        {
            _currentBodyName = data.to.bodyName;
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
        }

        private static Vector3 GetLocalPosition(CelestialBody celestialObject, double latitude, double longitude)
        {
            return Vector3.zero;
        }

        private static double GetLongitude(Vector3d radialPosition)
        {
            Vector3d norm = radialPosition.normalized;
            double longitude = Math.Atan2(norm.z, norm.x)*57.295780181884766f + 180;
            return (!double.IsNaN(longitude) ? longitude : 0.0);
        }

        private static double GetLatitude(Vector3d radialPosition)
        {
            double latitude = Math.Asin(radialPosition.normalized.y)*57.295780181884766;
            return (!double.IsNaN(latitude) ? latitude : 0.0);
        }

        private void GenerateModelLists()
        {
            UrlDir.UrlConfig[] staticConfigs = GameDatabase.Instance.GetConfigs("STATIC");
            _instancedList = new Dictionary<string, List<StaticObject>>();
            _modelList = new Dictionary<string, string>();

            foreach (UrlDir.UrlConfig staticUrlConfig in staticConfigs)
            {
                if (staticUrlConfig.url.IndexOf("KerbTown/EE", StringComparison.OrdinalIgnoreCase) > -1)
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

                    if (_instancedList.ContainsKey(modelUrl))
                    {
                        _instancedList[modelUrl].Add(
                            new StaticObject(radPosition, rotAngle, radOffset, orientation,
                                visRange, modelUrl, staticUrlConfig.url, celestialBodyName));
                    }
                    else
                    {
                        _instancedList.Add(modelUrl,
                            new List<StaticObject>
                            {
                                new StaticObject(radPosition, rotAngle, radOffset, orientation,
                                    visRange, modelUrl, staticUrlConfig.url, celestialBodyName)
                            });
                    }
                }
            }
        }

        private void InstantiateStatic(PQS celestialPQS, StaticObject sObject)
        {
            float visibilityRange = sObject.VisRange;
            Vector3 orientDirection = sObject.Orientation;
            float localRotationAngle = sObject.RotAngle;
            float radiusOffset = sObject.RadOffset;
            Vector3 radialPosition = sObject.RadPosition;
            string modelUrl = sObject.ModelUrl;

            if (radialPosition == Vector3.zero)
            {
                radialPosition =
                    _currentCelestialObj.CelestialBodyComponent.transform.InverseTransformPoint(
                        FlightGlobals.ActiveVessel.transform.position);

                //radialPosition =celestialPQS.GetRelativePosition(FlightGlobals.ActiveVessel.transform.position);

                sObject.RadPosition = radialPosition;
            }

            if (orientDirection == Vector3.zero)
            {
                orientDirection = Vector3.up;
                sObject.Orientation = orientDirection;
            }

            sObject.Latitude = GetLatitude(radialPosition);
            sObject.Longitude = GetLongitude(radialPosition);

            GameObject staticGameObject = GameDatabase.Instance.GetModel(modelUrl);

            // Set objects to layer 15 so that they collide correctly with Kerbals.
            SetLayerRecursively(staticGameObject, 15);

            // Set the parent object to the celestial component's GameObject.
            staticGameObject.transform.parent = celestialPQS.transform;

            // Added not for collision support but to reduce performance cost when moving static objects around.
            if (staticGameObject.GetComponent<Rigidbody>() == null)
            {
                var rigidBody = staticGameObject.AddComponent<Rigidbody>();
                rigidBody.useGravity = false; // Todo remove redundant code and test.
                rigidBody.isKinematic = true;
            }

            _myLodRange = new PQSCity.LODRange
                          {
                              renderers = new[] {staticGameObject},
                              objects = new[] {staticGameObject},
                              visibleRange = visibilityRange
                          };

            //var myCity = staticGameObject.AddComponent<PQSCity>();
            var myCity = staticGameObject.AddComponent<PQSCityEx>();

            myCity.lod = new[] {_myLodRange};
            myCity.frameDelta = 1;
            myCity.repositionToSphere = true;
            myCity.repositionRadial = radialPosition;
            myCity.repositionRadiusOffset = radiusOffset;
            myCity.reorientFinalAngle = localRotationAngle;
            myCity.reorientToSphere = true;
            myCity.reorientInitialUp = orientDirection;
            myCity.sphere = celestialPQS;

            myCity.order = 100;

            myCity.OnSetup();
            myCity.Orientate();

            staticGameObject.SetActive(true);

            sObject.PQSCityComponent = myCity;
            sObject.StaticGameObject = staticGameObject;

            switch (sObject.ObjectID)
            {
                case "MushroomCave":
                    AddNativeComponent(staticGameObject, typeof (MushroomCave));
                    break;

                default:
                    AddModuleComponents(staticGameObject, sObject.ConfigURL);
                    break;
            }
        }

        private static Type AddModule(ConfigNode configNode)
        {
            string namespaceName = configNode.GetValue("namespace");
            string className = configNode.GetValue("name");

            if (string.IsNullOrEmpty(className))
            {
                Extensions.LogError(
                    "Could not add a module to the static object because the node has no name parameter.");
                return null;
            }

            if (string.IsNullOrEmpty(namespaceName))
            {
                Extensions.LogError(
                    "Could not add a module to the static object because the node has no namespace parameter.");
                return null;
            }

            Type moduleClass = AssemblyLoader.loadedAssemblies.SelectMany(asm => asm.assembly.GetTypes())
                .FirstOrDefault(t => t.Namespace == namespaceName && t.Name == className);

            if (moduleClass != null)
                return moduleClass;

            Extensions.LogError("Could not obtain module of type \"" + className + "\" from AssemblyLoader.");
            return null;
        }

        private void AddNativeComponent(GameObject staticGameObject, Type classType)
        {
            staticGameObject.AddComponent(classType);
        }

        private void AddModuleComponents(GameObject staticGameObject, string rootNodeUrl)
        {
            Stopwatch stopWatch = Stopwatch.StartNew();

            ConfigNode rootNode = GameDatabase.Instance.GetConfigNode(rootNodeUrl);
            IEnumerator nodeEnum = rootNode.nodes.GetEnumerator();

            while (nodeEnum.MoveNext())
            {
                var currentNode = (ConfigNode) nodeEnum.Current;
                if (currentNode == null) continue;

                name = currentNode.name;
                if (name == null) continue;

                var moduleTypes = new Dictionary<string, int> {{"MODULE", 0}, {"RESOURCE", 1}};

                int nodeType;
                if (!moduleTypes.TryGetValue(name, out nodeType))
                    continue;

                switch (nodeType)
                {
                    case 0:
                        Type sModule = AddModule(currentNode);
                        if (sModule == null)
                            continue;

                        if (staticGameObject.AddComponent(sModule) == null)
                            Extensions.LogError("Could not add module from type of \"" + sModule.Name + "\"");
                        break;

                    case 1:
                        //PartResource partResource = part.AddResource(currentNode);
                        break;
                }
            }

            stopWatch.Stop();
            Extensions.LogInfo("Modules loaded. (" + stopWatch.ElapsedMilliseconds + "ms)");
        }

        private static void SetLayerRecursively(GameObject staticGameObject, int newLayerNumber)
        {
            // Only set to layer 'newLayerNumber' if the collider is not a trigger.
            if ((staticGameObject.collider != null &&
                 staticGameObject.collider.enabled &&
                 !staticGameObject.collider.isTrigger) || staticGameObject.collider == null)
            {
                staticGameObject.layer = newLayerNumber;
            }

            foreach (Transform child in staticGameObject.transform)
            {
                SetLayerRecursively(child.gameObject, newLayerNumber);
            }
        }

        private static CelestialObject GetCelestialObject(string celestialName)
        {
            return new CelestialObject(FlightGlobals.ActiveVessel.mainBody.gameObject);
        }

        private StaticObject GetStaticObjectFromID(string objectID)
        {
            return _instancedList[_currentModelUrl].FirstOrDefault(obFind => obFind.ObjectID == objectID);
        }

        private void RemoveCurrentStaticObject(string modelURL)
        {
            _instancedList[modelURL].Remove(_currentSelectedObject);
        }

        private StaticObject GetDefaultStaticObject(string modelUrl, string configUrl)
        {
            return new StaticObject(Vector3.zero, 0, GetSurfaceRadiusOffset(), Vector3.up, 1000, modelUrl, configUrl, "");
        }

        private float GetSurfaceRadiusOffset()
        {
            //todo change to activevessel.altitude after further investigation or just to surface height..

            Vector3d relativePosition =
                _currentCelestialObj.PQSComponent.GetRelativePosition(FlightGlobals.ActiveVessel.GetWorldPos3D());
            Vector3d rpNormalized = relativePosition.normalized;

            return (float) (relativePosition.x/rpNormalized.x - _currentCelestialObj.PQSComponent.radius);
        }

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

        public class StaticObject
        {
            public readonly string ConfigURL;
            public readonly string ModelUrl;
            public readonly string NameID;
            public readonly string ObjectID;
            public readonly float VisRange;

            public string CelestialBodyName = "";

            public double Latitude;
            public double Longitude;
            public Vector3 Orientation;
            public PQSCity PQSCityComponent;

            public float RadOffset;
            public Vector3 RadPosition;

            public float RotAngle;
            public GameObject StaticGameObject;

            private List<Collider> _colliderComponents;
            private List<Renderer> _rendererComponents;

            public StaticObject(Vector3 radialPosition, float rotationAngle, float radiusOffset,
                Vector3 objectOrientation, float visibilityRange, string modelUrl, string configUrl,
                string celestialBodyName, string objectID = "")
            {
                RadPosition = radialPosition;
                RotAngle = rotationAngle;
                RadOffset = radiusOffset;
                Orientation = objectOrientation;
                VisRange = visibilityRange;

                CelestialBodyName = celestialBodyName;

                ModelUrl = modelUrl;
                ConfigURL = configUrl;

                ObjectID = objectID;

                if (string.IsNullOrEmpty(ObjectID))
                    ObjectID = (visibilityRange + rotationAngle + radiusOffset + objectOrientation.magnitude +
                                radialPosition.x + radialPosition.y + radialPosition.z +
                                Random.Range(0f, 1000000f)).ToString("N2");

                NameID = string.Format("{0} ({1})", modelUrl.Substring(modelUrl.LastIndexOf('/') + 1), ObjectID);
            }

            public void Manipulate(bool inactive)
            {
                Manipulate(inactive, XKCDColors.BlueyGrey);
            }

            public void Manipulate(bool inactive, Color highlightColor)
            {
                if (StaticGameObject == null)
                {
                    Extensions.LogWarning(NameID + " has no GameObject attached.");
                    return;
                }

                #region Colliders

                if (_colliderComponents == null || _colliderComponents.Count == 0)
                {
                    Collider[] colliderList = StaticGameObject.GetComponentsInChildren<Collider>();

                    if (colliderList.Length > 0)
                    {
                        _colliderComponents = new List<Collider>(colliderList);
                    }
                    else Extensions.LogWarning(NameID + " has no collider components.");
                }

                if (_colliderComponents != null && _colliderComponents.Count > 0)
                {
                    foreach (Collider collider in _colliderComponents)
                    {
                        collider.enabled = !inactive;
                    }
                }

                #endregion

                #region Highlight

                if ((_rendererComponents == null || _rendererComponents.Count == 0))
                {
                    Renderer[] rendererList = StaticGameObject.GetComponentsInChildren<Renderer>();
                    if (rendererList.Length == 0)
                    {
                        Extensions.LogWarning(NameID + " has no renderer components.");
                        return;
                    }
                    _rendererComponents = new List<Renderer>(rendererList);
                }

                if (!inactive)
                    highlightColor = new Color(0, 0, 0, 0);

                foreach (Renderer renderer in _rendererComponents)
                {
                    renderer.material.SetFloat("_RimFalloff", 1.8f);
                    renderer.material.SetColor("_RimColor", highlightColor);
                }

                #endregion
            }

            public void Reorientate()
            {
                if (PQSCityComponent == null) return;
                PQSCityComponent.repositionRadial = RadPosition;
                PQSCityComponent.repositionRadiusOffset = RadOffset;
                PQSCityComponent.reorientFinalAngle = RotAngle;
                PQSCityComponent.reorientInitialUp = Orientation;
                PQSCityComponent.Orientate();
            }

            public override string ToString()
            {
                return
                    string.Format(
                        "NameID: {0}, ObjectID: {1}, CelestialBodyName: {2}, ModelUrl: {3}, ConfigUrl: {4}, RPos: {5}",
                        NameID, ObjectID, CelestialBodyName, ModelUrl, ConfigURL, RadPosition);
            }
        }
    }

    public static class Extensions
    {
        public static void LogError(string message)
        {
            Debug.LogError("KerbTown: " + message);
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning("KerbTown: " + message);
        }

        public static void LogInfo(string message)
        {
            Debug.Log("KerbTown: " + message);
        }

        public static int SecondLastIndex(this string str, char searchCharacter)
        {
            int lastIndex = str.LastIndexOf(searchCharacter);

            if (lastIndex != -1)

            {
                return str.LastIndexOf(searchCharacter, lastIndex - 1);
            }

            return -1;
        }

        public static int SecondLastIndex(this string str, string searchString)
        {
            int lastIndex = str.LastIndexOf(searchString, StringComparison.Ordinal);

            if (lastIndex != -1)
            {
                return str.LastIndexOf(searchString, lastIndex - 1, StringComparison.Ordinal);
            }

            return -1;
        }
    }
}