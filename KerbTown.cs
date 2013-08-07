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
using System.Reflection;
using Kerbtown.EEComponents;
using Kerbtown.NativeModules;
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

        private Dictionary<string, List<StaticObject>> _eeInstances;
        private Dictionary<string, List<StaticObject>> _instancedList;
        private Dictionary<string, string> _modelList;

        private PQSCity.LODRange _myLodRange;
        private float _prevRotationAngle;

        private void Awake()
        {
            GenerateModelLists();
            InstantiateEasterEggs();
            InstantiateStaticsFromInstanceList();

            if (FlightGlobals.currentMainBody != null)
                _currentBodyName = FlightGlobals.currentMainBody.bodyName; //todo remove redundant code

            GameEvents.onDominantBodyChange.Add(BodyChangedCallback);
            GameEvents.onFlightReady.Add(FlightReadyCallBack);
        }

        private void OnDestroy()
        {
            Extensions.LogInfo("Removing script references.");
            GameEvents.onDominantBodyChange.Remove(BodyChangedCallback);
            GameEvents.onFlightReady.Remove(FlightReadyCallBack);

            foreach (var i in _instancedList.SelectMany(ins => ins.Value))
                DestroyPQS(i.PQSCityComponent);
            

            foreach (var i in _eeInstances.SelectMany(ins => ins.Value))
                DestroyPQS(i.PQSCityComponent);
            
            KtCamera.RestoreCameraParent();
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
                    string launchSiteName = ins.GetValue("LaunchSiteName") ?? "";

                    if (_instancedList.ContainsKey(modelUrl))
                    {
                        _instancedList[modelUrl].Add(
                            new StaticObject(radPosition, rotAngle, radOffset, orientation,
                                visRange, modelUrl, staticUrlConfig.url, celestialBodyName, "", launchSiteName));
                    }
                    else
                    {
                        _instancedList.Add(modelUrl,
                            new List<StaticObject>
                            {
                                new StaticObject(radPosition, rotAngle, radOffset, orientation,
                                    visRange, modelUrl, staticUrlConfig.url, celestialBodyName, "", launchSiteName)
                            });
                    }
                }
            }
        }

        private void InstantiateStatic(PQS celestialPQS, StaticObject sObject, bool freshObject = false)
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

                sObject.RadPosition = radialPosition;
            }

            if (orientDirection == Vector3.zero)
            {
                orientDirection = Vector3.up;
                sObject.Orientation = orientDirection;
            }

            sObject.Latitude = GetLatitude(radialPosition);
            sObject.Longitude = GetLongitude(radialPosition);

            GameObject staticGameObject = GameDatabase.Instance.GetModel(modelUrl); // Instantiate
            staticGameObject.SetActive(true);
            // Set objects to layer 15 so that they collide correctly with Kerbals.
            SetLayerRecursively(staticGameObject, 15);

            // Set the parent object to the celestial component's GameObject.
            staticGameObject.transform.parent = celestialPQS.transform;

            Transform[] gameObjectList = staticGameObject.GetComponentsInChildren<Transform>();

            List<GameObject> rendererList =
                (from t in gameObjectList where t.gameObject.renderer != null select t.gameObject).ToList();


            _myLodRange = new PQSCity.LODRange
            {
                renderers = rendererList.ToArray(),
                objects = new GameObject[0],
                //new[] {staticGameObject},  // Change to GameObject children.
                visibleRange = visibilityRange
            };

            //var myCity = staticGameObject.AddComponent<PQSCity>();
            var myCity = staticGameObject.AddComponent<PQSCityEx>();

            myCity.lod = new[] { _myLodRange };

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

            myCity.OnSetup();
            myCity.Orientate();

            if (freshObject)
            {
                foreach (var renObj in rendererList)
                    renObj.renderer.enabled = true;
            }

            sObject.PQSCityComponent = myCity;
            sObject.StaticGameObject = staticGameObject;

            switch (sObject.ObjectID)
            {
                case "MushroomCave":
                    AddNativeComponent(staticGameObject, typeof(MushroomCave));
                    break;

                case "PurplePathway":
                    AddNativeComponent(staticGameObject, typeof(PurplePathway));
                    break;

                default:
                    AddModuleComponents(staticGameObject, sObject.ConfigURL);
                    break;
            }
        }

        private void InstantiateEasterEggs()
        {
            var configDict = new Dictionary<string, string>
                             {
                                 {"Hubs/Static/KerbTown/EE01/EE01", "MushroomCave"},
                                 {"Hubs/Static/KerbTown/EE02/EE02", "PurplePathway"}
                             };

            _eeInstances = new Dictionary<string, List<StaticObject>>(configDict.Count);

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

                    if (_eeInstances.ContainsKey(modelUrl))
                        _instancedList[modelUrl].Add(staticObject);
                    else
                        _eeInstances.Add(modelUrl, new List<StaticObject> {staticObject});

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
                    instanceNode.AddValue("LaunchSiteName", inst.LaunchSiteName);

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

/*
        private static Vector3 GetLocalPosition(CelestialBody celestialObject, double latitude, double longitude)
        {
            return Vector3.zero;
        }
*/

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

            return (from PQS gameObjectInScene in FindSceneObjectsOfType(typeof (PQS))
                where gameObjectInScene.name == celestialName
                select new CelestialObject(gameObjectInScene.transform.parent.gameObject)).FirstOrDefault();
        }

/*
        private StaticObject GetStaticObjectFromID(string objectID)
        {
            return _instancedList[_currentModelUrl].FirstOrDefault(obFind => obFind.ObjectID == objectID);
        }
*/

        private void RemoveCurrentStaticObject(string modelURL)
        {
            _instancedList[modelURL].Remove(_currentSelectedObject);
        }

        private StaticObject GetDefaultStaticObject(string modelUrl, string configUrl)
        {
            // 150000f is flightcamera max distance
            // Space Center clips at about 80-90km
            return new StaticObject(Vector3.zero, 0, GetSurfaceRadiusOffset(), Vector3.up, 100000, modelUrl, configUrl,
                "");
        }

        private float GetSurfaceRadiusOffset()
        {
            //todo change to activevessel.altitude after further investigation or just to surface height..

            Vector3d relativePosition =
                _currentCelestialObj.PQSComponent.GetRelativePosition(FlightGlobals.ActiveVessel.GetWorldPos3D());
            Vector3d rpNormalized = relativePosition.normalized;

            return (float) (relativePosition.x/rpNormalized.x - _currentCelestialObj.PQSComponent.radius);
        }

        private static void DestroyPQS(PQSCity pqsCityComponent)
        {
            foreach (PQSCity.LODRange lod in pqsCityComponent.lod)
            {
                lod.SetActive(false);

                foreach (GameObject lodren in lod.renderers)
                {
                    Destroy(lodren);
                }
                foreach (GameObject lodobj in lod.objects)
                {
                    Destroy(lodobj);
                }
            }

            pqsCityComponent.modEnabled = false;
            //pqsCityComponent.RebuildSphere();

            GameObject gobj = pqsCityComponent.gameObject;
            
            gobj.transform.parent = null;

            Destroy(pqsCityComponent);
            DestroyImmediate(gobj);
        }

        #region Static Components

        private static void AddModule(ConfigNode configNode, GameObject staticGameObject)
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
                AddNativeComponent(staticGameObject, configNode, className);
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

            var moduleComponent = staticGameObject.AddComponent(moduleClass) as MonoBehaviour;

            if (moduleComponent == null)
            {
                Extensions.LogError("Could not add the obtained module \"" + moduleClass.Name +
                                    "\" to the static game object.");
                return;
            }

            AssignVariables(configNode, moduleComponent);
        }

        private static void AssignVariables(ConfigNode configNode, MonoBehaviour moduleComponent)
        {
            IEnumerator enumerator = configNode.values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var current = (ConfigNode.Value) enumerator.Current;

                if (current == null)
                    continue;

                FieldInfo field = moduleComponent.GetType().GetField(current.name);
                if (field == null)
                    continue;

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

        private static void AddNativeComponent(GameObject staticGameObject, Type classType)
        {
            staticGameObject.AddComponent(classType);
        }

        private static void AddNativeComponent(GameObject staticGameObject, ConfigNode configNode, string className)
        {
            string objectName = configNode.GetValue("collider");
            string animName = configNode.GetValue("animationName");

            switch (className)
            {
                case "AnimateOnCollision":
                case "AnimateOnClick":
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

                var moduleTypes = new Dictionary<string, int> {{"MODULE", 0}, {"RESOURCE", 1}, {"RIGIDBODY", 2}};

                int nodeType;
                if (!moduleTypes.TryGetValue(name, out nodeType))
                    continue;

                switch (nodeType)
                {
                    case 0: // Module
                        AddModule(currentNode, staticGameObject);
                        break;

                    case 1: // Resource
                        //PartResource partResource = part.AddResource(currentNode);
                        break;

                    case 2: // Rigidbody
                        string objectName = currentNode.GetValue("name");
                        if (string.IsNullOrEmpty(objectName))
                        {
                            Extensions.LogError(
                                "The 'name' parameter is empty. You must specify the GameObject name for a Rigidbody component to be added.");
                            continue;
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

                        AddRigidBodies(staticGameObject, objectName, rbMass, rbDrag, rbAngularDrag, rbUseGravity,
                            rbIsKinematic, rbInterpolation, rbCollisionDetectionMode);
                        break;
                }
            }

            stopWatch.Stop();
            Extensions.LogInfo("Modules loaded. (" + stopWatch.ElapsedMilliseconds + "ms)");
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

        public class StaticObject
        {
            public readonly string ConfigURL;
            public readonly string ModelUrl;
            public readonly string NameID;
            public readonly string ObjectID;
            public readonly float VisRange;

            public string CelestialBodyName = "";

            public double Latitude;
            public string LaunchSiteName = "";
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
                string celestialBodyName, string objectID = "", string launchSiteName = "")
            {
                RadPosition = radialPosition;
                RotAngle = rotationAngle;
                RadOffset = radiusOffset;
                Orientation = objectOrientation;
                VisRange = visibilityRange;

                CelestialBodyName = celestialBodyName;

                ModelUrl = modelUrl;
                ConfigURL = configUrl;

                LaunchSiteName = launchSiteName;

                ObjectID = objectID;

                if (string.IsNullOrEmpty(ObjectID))
                    ObjectID = (visibilityRange + rotationAngle + radiusOffset + objectOrientation.magnitude +
                                radialPosition.x + radialPosition.y + radialPosition.z +
                                Random.Range(0f, 1000000f)).ToString("N2");

                NameID = string.Format("{0} ({1})", modelUrl.Substring(modelUrl.LastIndexOf('/') + 1), ObjectID);
            }

            public void Manipulate(bool objectInactive)
            {
                Manipulate(objectInactive, XKCDColors.BlueyGrey);
            }

            public void Manipulate(bool objectInactive, Color highlightColor)
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
                        collider.enabled = !objectInactive;
                    }
                }

                #endregion

                #region Highlight

                if ((_rendererComponents == null || _rendererComponents.Count == 0))
                {
                    Renderer[] rendererList = StaticGameObject.GetComponentsInChildren<Renderer>();
                    if (rendererList.Length == 0)
                    {
                        Extensions.PostScreenMessage("[KerbTown] Active Vessel not within visibility range.");
                        Extensions.LogWarning(NameID + " has no renderer components.");
                        return;
                    }
                    _rendererComponents = new List<Renderer>(rendererList);
                }

                if (!objectInactive) // Deactivate.
                {
                    highlightColor = new Color(0, 0, 0, 0);

                    KtCamera.RestoreCameraParent();
                }
                else // Activate
                {
                    if (
                        Vector3.Distance(PQSCityComponent.sphere.transform.position, PQSCityComponent.transform.position) >=
                        PQSCityComponent.lod[0].visibleRange)
                        KtCamera.SetCameraParent(StaticGameObject.transform);
                    else
                        Extensions.PostScreenMessage(
                            "[KerbTown] Ignoring camera switch. Static object is not within the visible range of your active vessel.");
                }

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

            public bool MakeLaunchSite(bool isLaunchSite)
            {
                if (!isLaunchSite)
                {
                    LaunchSiteName = "";
                    return true;
                }

                foreach (Transform t in StaticGameObject.transform)
                {
                    if (!t.name.EndsWith("_spawn"))
                        continue;

                    LaunchSiteName = t.name.Replace("_spawn", "");
                    return true;
                }

                Extensions.LogError("Unable to find the launch spawning transform.");
                return false;
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
}