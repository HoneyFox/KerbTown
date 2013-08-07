/* LICENSE
 * This work is licensed under the Creative Commons Attribution-NoDerivs 3.0 Unported License. 
 * To view a copy of this license, visit http://creativecommons.org/licenses/by-nd/3.0/ or send a letter to Creative Commons, 444 Castro Street, Suite 900, Mountain View, California, 94041, USA.
 */

using System.Collections;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

namespace Kerbtown.EEComponents
{
    public class PurplePathway : MonoBehaviour
    {
        // Use this for initialization
        private void Start()
        {
            Transform[] childList = GetComponentsInChildren<Transform>();

            RockFall rockScript = null;
            GameObject finalRoomGameObject = null;

            foreach (Transform child in childList)
            {
                switch (child.name)
                {
                    case "RockObs":
                        rockScript = child.gameObject.AddComponent<RockFall>();
                        break;

                    case "FinalRoom":
                        finalRoomGameObject = child.gameObject;
                        break;

                    case "MovingPlatform":
                        child.gameObject.AddComponent<MovingPlatform>();
                        break;

                    case "letter":
                        child.gameObject.AddComponent<LetterObject>();
                        break;

                    case "EntranceColliderEnter":
                        child.gameObject.AddComponent<EnterCaveScript>();
                        break;

                    case "EntranceColliderExit":
                        child.gameObject.AddComponent<ExitCaveScript>();
                        break;

                    default:
                        continue;
                }
            }

            if (rockScript == null || finalRoomGameObject == null)
            {
                Extensions.LogError("Couldn't find necessary components.");
                return;
            }

            rockScript.FinalRoomObject = finalRoomGameObject;

            finalRoomGameObject.SetActive(false);
        }
    }

    public class MusicAttachment : MonoBehaviour
    {
        private AudioClip[] _audioClips;
        private float _originalMusicSound;

        private void Start()
        {
            StartCoroutine(AttachSoundAssets());
            
            _originalMusicSound = GameSettings.MUSIC_VOLUME;
            MusicLogic.SetVolume(0);
        }

        private IEnumerator AttachSoundAssets()
        {
            //string soundDir = "file://" + KSPUtil.ApplicationRootPath + "GameData/Hubs/Static/KerbTown/sounds/";
            //var mus1 = new WWW(soundDir + "bg1.ogg").GetAudioClip(true, true);

            //while (!mus1.isReadyToPlay)
            //    yield return mus1;

            _audioClips = new[] {GameDatabase.Instance.GetAudioClip("Hubs/Static/KerbTown/sounds/bg1")};

            AddMusicAudio();

            Extensions.LogInfo("KerbTown: PurplePathway sounds are loaded.");
            yield return null;
        }

        private void OnDestroy()
        {
            audio.Stop();
            MusicLogic.SetVolume(_originalMusicSound);
        }

        private void AddMusicAudio()
        {
            var audioSource = gameObject.AddComponent<AudioSource>();
            var audClip = _audioClips[0];
            audioSource.clip = audClip;
            audioSource.loop = true;
            audioSource.volume = 1f;
            audioSource.Play();
        }
    }

    public class ExitCaveScript : MonoBehaviour
    {
        private void OnTriggerEnter(Collider otherCollider)
        {
            if (otherCollider.name != "capsuleCollider") return;

            var musicScript = otherCollider.gameObject.GetComponent<MusicAttachment>();
            if (musicScript != null)
            {
                musicScript.audio.Stop();
                Destroy(musicScript);
            }

        }
    }

    public class EnterCaveScript : MonoBehaviour
    {
        private void OnTriggerEnter(Collider otherCollider)
        {
            if (otherCollider.name != "capsuleCollider") return;

            if (otherCollider.gameObject.GetComponent<MusicAttachment>() == null)
                otherCollider.gameObject.AddComponent<MusicAttachment>();
        }
    }

    public class LetterObject : MonoBehaviour
    {
        private readonly Rect _boxRect = new Rect(Screen.width/2 - 150, Screen.height/2 - 125, 300, 250);

        private readonly string[] _messages =
        {
            "Good job finding this cave, you are a good friend of Jeb's after all.\n\n" +
            "Looks like this isn't the end of your journey yet.\n\n" +
            "We've gone to southern Tylo - see you there!\n\nP.S. Bill Snr. broke the TV.. it's stuck on the screensaver now, sorry!\n\n" +
            "   -Jebediah Snr.",
            "I knew you would find this cave Jeb, you ARE my son after all! Just like your dad.\n\n" +
            "We maybe kinda crashed into the wrong planet - we've gone to southern Tylo now.\n\n" +
            "Anyway before we left Bill Sr. broke the TV! Now it's stuck on the screensaver. Not that we got reception here anyway!\n\n" +
            "We took the rest of our equipment with us.\n\n" +
            "   -Jebediah Snr."
        };

        private bool _enableGUI;
        private bool _isJeb = false; 

        private void OnMouseEnter()
        {
            renderer.material.SetFloat("_RimFalloff", 0.5f);
            renderer.material.SetColor("_RimColor", Color.white);
        }

        private void OnMouseExit()
        {
            renderer.material.SetColor("_RimColor", Color.clear);
        }

        private void OnMouseDown()
        {
            var kerbal = FlightGlobals.ActiveVessel.GetComponent<KerbalEVA>();
            if (kerbal != null)
            {
                if (kerbal.name.Contains("Jebediah"))
                {
                    _isJeb = true;
                }
            }

            _enableGUI = true;
        }

        private void OnGUI()
        {
            if (!_enableGUI) return;
            
            GUI.skin = HighLogic.Skin;
            
            GUI.Box(_boxRect, "");
            GUI.BeginGroup(_boxRect);

            GUI.Label(new Rect(10, 10, 280, 230), _messages[_isJeb ? 1 : 0]);
            if (GUI.Button(new Rect(160, 210, 130, 30), "Close")) _enableGUI = false;

            GUI.EndGroup();
        }
    }

    public class RockFall : MonoBehaviour
    {
        public GameObject FinalRoomObject;
        public GameObject RockObject;
        private float _distance;
        private Vector3 _endLocation;

        private bool _lowerRock;
        private bool _played;

        private Vector3 _startLocation;
        private float _startTime;

        private void Start()
        {
            _startLocation = transform.localPosition;
            _endLocation = new Vector3(_startLocation.x, _startLocation.y - 6f, _startLocation.z);
            _distance = Vector3.Distance(_startLocation, _endLocation);
        }

        private void Update()
        {
            if (!_lowerRock) return;

            float currentTime = Time.time - _startTime;

            transform.localPosition = Vector3.Lerp(_startLocation, _endLocation, currentTime/_distance);

            if (!(currentTime > _distance))
                return;

            _lowerRock = false;

            renderer.enabled = false;
        }

        private void OnMouseDown()
        {
            if (_played) return;
            _played = true;

            FinalRoomObject.SetActive(true);
            StartCoroutine(PlayScript());
        }

        private IEnumerator PlayScript()
        {
            renderer.material.SetColor("_RimColor", Color.clear);

            yield return new WaitForSeconds(0.1f);

            _startTime = Time.time;
            _lowerRock = true;
        }

        private void OnMouseEnter()
        {
            if (_played) return;
            renderer.material.SetFloat("_RimFalloff", 5f);
            renderer.material.SetColor("_RimColor", Color.white);
        }

        private void OnMouseExit()
        {
            if (_played) return;
            renderer.material.SetColor("_RimColor", Color.clear);
        }
    }

    public class MovingPlatform : MonoBehaviour
    {
        // Adapted from: http://answers.unity3d.com/questions/8207/charactercontroller-falls-through-or-slips-off-mov.html

        // Store all rigidbody capsule colliders currently on platform.
        private const float VerticalOffset = 0.5f; // Height above the center of object the char must be kept
        private readonly Hashtable _kerbalsOnPlatform = new Hashtable();

        // Used to calculate horizontal movement
        private Vector3 _lastPos;

        private void OnTriggerEnter(Collider other)
        {
            if (other.name != "capsuleCollider")
                return;

            var otherRigidbody = other.transform.root.GetComponentInChildren<Rigidbody>();

            if (otherRigidbody == null)
            {
                Extensions.LogWarning("Kerbal did nay have a rigidbody!");
                return;
            }

            Transform kerbalTransform = other.transform.root;

            float yOffset = other.bounds.size.y/2f - other.bounds.center.y + VerticalOffset;

            var kData = new Data(otherRigidbody, kerbalTransform, yOffset);

            _kerbalsOnPlatform.Add(other.transform, kData);
        }

        private void OnTriggerExit(Collider other)
        {
            _kerbalsOnPlatform.Remove(other.transform);
        }

        private void Start()
        {
            _lastPos = transform.position;
        }

        private void LateUpdate()
        {
            Vector3 curPos = transform.position;
            Vector3 deltaVec = curPos - _lastPos;

            _lastPos = curPos;

            foreach (DictionaryEntry d in _kerbalsOnPlatform)
            {
                var kerbalData = (Data) d.Value;
                Vector3 newPos = kerbalData.KTransform.position;
                newPos += deltaVec;
                kerbalData.KTransform.position = newPos;
            }
        }

        public struct Data
        {
            public Rigidbody KRigidBody;
            public Transform KTransform;
            public float YOffset;

            public Data(Rigidbody rigidBody, Transform transform, float yOffset)
            {
                KRigidBody = rigidBody;
                KTransform = transform;
                YOffset = yOffset;
            }
        };
    }
}