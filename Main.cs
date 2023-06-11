using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Loadson;
using LoadsonAPI;
using UnityEngine;
using UnityEngine.SceneManagement;

 
namespace Jannik_Randomizer {
    public class Main : Mod {
        private System.Random random;
        
        private float lastTimeScale = 1;
        private CursorLockMode lastlockState= CursorLockMode.None;
        private bool lastvisible= true;
        private bool lastSlowMo = false;

        private bool windowOpen= false;

        private string settingsString; 

        private int seed = 0;
        private float chaos = 1f;
        private bool snapToGrid = true;
        private bool randomPosWalls = false;
        private bool randomRotWalls = false;
        private bool randomScaleWalls = false;
        private bool randomSwapWalls = false;
        private bool randomPosProps = false;
        private bool spawnRandomWalls = false;
        private bool randomStartWeapon = false;
        private bool randomSpawn = false;
        private bool randomGoal = true;
        private bool demoncubed = false;
        private readonly float minGoalDistance = 25;
        private int maxTries = 1000;
        private bool compass = false;
        private LineRenderer lineRenderer;
        private Camera freeCam;
        private bool freeCamOn = false;


        private int windowID;
        private Material groundMaterial;


        private GameObject player;
        private GameObject milk;
        public override void OnEnable() {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            
            seed = Environment.TickCount;
            windowID = ImGUI_WID.GetWindowId();
            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentCulture = ci;
            System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            string loadedsettingsString = PlayerPrefs.GetString("jannik_randomizer_setting", "none");
            if(loadedsettingsString == "none") {
                Loadson.Console.Log("loading from PlayerPrefs failed!: nothin in there");
                createSettingsString();
            }else if (!tryParseSettingsString(loadedsettingsString)) {
                Loadson.Console.Log("loading from PlayerPrefs failed!: this shit aint right");
                Loadson.Console.Log("take a lookie: "+loadedsettingsString);
                createSettingsString();
            } else {
                settingsString = loadedsettingsString;
            }
        }

        public override void OnDisable() {
            PlayerPrefs.SetString("jannik_randomizer_setting", settingsString);
            PlayerPrefs.Save();
        }

        public override void Update(float deltaTime) {
            if(Input.GetKeyDown(KeyCode.M)){
                changeWindowOpen(!windowOpen);
            }
            if (freeCamOn && Input.GetKeyDown(KeyCode.Escape)) {
                turnOffFreeCam();
            }
            if (windowOpen) {
                Time.timeScale = 0;
            }
            if (compass) {
                lineRenderer.SetPosition(0, player.transform.position);
            }

            if (freeCamOn&&!windowOpen) {
                if (Input.GetKey(KeyCode.W)) {
                    freeCam.transform.position += freeCam.transform.forward;
                }
                if (Input.GetKey(KeyCode.S)) {
                    freeCam.transform.position -= freeCam.transform.forward;
                }
                if (Input.GetKey(KeyCode.D)) {
                    freeCam.transform.position += freeCam.transform.right;
                }
                if (Input.GetKey(KeyCode.A)) {
                    freeCam.transform.position -= freeCam.transform.right;
                }
                if (Input.GetKey(KeyCode.E)) {
                    freeCam.transform.position += freeCam.transform.up;
                }
                if (Input.GetKey(KeyCode.Q)) {
                    freeCam.transform.position -= freeCam.transform.up;
                }
                Look();
            }
        }

        private void turnOffFreeCam() {
            freeCamOn = false;
            freeCam.gameObject.SetActive(false);
            Time.timeScale = lastTimeScale;
            Game.Instance.playing = true;
            Game.Instance.done = false;
            changeWindowOpen(true);
        }

        private void turnOnFreeCam() {
            freeCamOn = true;
            freeCam.gameObject.SetActive(true);
            freeCam.gameObject.transform.position = player.transform.position;
            changeWindowOpen(false);
            lastTimeScale = Time.timeScale;
            Time.timeScale = 0;
            Game.Instance.playing = false;
            Game.Instance.done = true;
        }

        private void Look() {
            float mouseX = Input.GetAxis("Mouse X") * 50f * Time.fixedDeltaTime ;
            float mouseY = Input.GetAxis("Mouse Y") * 50f * Time.fixedDeltaTime ;
            freeCam.transform.localRotation = Quaternion.Euler(freeCam.transform.localRotation.eulerAngles.x - mouseY, freeCam.transform.localRotation.eulerAngles.y + mouseX, 0);
        }

        private string settingsFormatVersion = "v1.1";
        private void createSettingsString() {
            string rawString = $"{settingsFormatVersion}|{seed}|{chaos}|{(snapToGrid?"1":"0")}|{(randomPosWalls?"1":"0")}|{(randomRotWalls ? "1":"0")}|{(randomScaleWalls ? "1":"0")}|{(randomSwapWalls ? "1":"0")}|{(randomPosProps ? "1":"0")}|{(spawnRandomWalls ? "1":"0")}|{(randomStartWeapon ? "1":"0")}|{(randomSpawn ? "1":"0")}|{(randomGoal ? "1":"0")}|{(compass ? "1":"0")}|{(demoncubed ? "1":"0")}";
            settingsString = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawString));
        }

        private bool tryParseSettingsString(string sString) {
            try {
                string nstring = Encoding.UTF8.GetString(Convert.FromBase64String(sString));
                string[] split = nstring.Split('|');

                if(split[0] == "v1.0") {
                    seed = int.Parse(split[1]);
                    chaos = float.Parse(split[2]);
                    snapToGrid = bool.Parse(split[3]);
                    randomPosWalls = bool.Parse(split[4]);
                    randomRotWalls = bool.Parse(split[5]);
                    randomScaleWalls = bool.Parse(split[6]);
                    randomSwapWalls = bool.Parse(split[7]);
                    randomPosProps = bool.Parse(split[8]);
                    spawnRandomWalls = bool.Parse(split[9]);
                    randomStartWeapon = bool.Parse(split[10]);
                    randomSpawn = bool.Parse(split[11]);
                    randomGoal = bool.Parse(split[12]);
                    compass = bool.Parse(split[13]);
                    demoncubed = true;
                    maxTries = 50;
                    return true;
                } else if (split.Length != 15 || split[0] != settingsFormatVersion) {
                    Loadson.Console.Log("settings parsing error: wrong version");
                    return false;
                };

                seed = int.Parse(split[1]);
                chaos = float.Parse(split[2]);
                snapToGrid = split[3]=="1";
                randomPosWalls = split[4] == "1";
                randomRotWalls = split[5] == "1";
                randomScaleWalls = split[6] == "1";
                randomSwapWalls = split[7] == "1";
                randomPosProps = split[8] == "1";
                spawnRandomWalls = split[9] == "1";
                randomStartWeapon = split[10] == "1";
                randomSpawn = split[11] == "1";
                randomGoal = split[12] == "1";
                compass = split[13] == "1";
                demoncubed = split[14] == "1";
                return true;
            } catch (Exception e) {
                Loadson.Console.Log("settings parsing error: " + e.Message);
                return false;
            }
        }

        private void changeWindowOpen(bool state) {
            windowOpen = state;
            if (windowOpen) {
                if (!Game.Instance.done) {
                    if (UIManger.Instance.deadUI.activeSelf) {
                        UIManger.Instance.deadUI.SetActive(false);
                        UIManger.Instance.DeadUI(false);
                    }
                    lastTimeScale = Time.timeScale;
                    lastlockState = Cursor.lockState;
                    lastvisible = Cursor.visible;
                    lastSlowMo = GameState.Instance.slowmo;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    Time.timeScale = 0;
                    Game.Instance.playing = false;
                    Game.Instance.done = true;
                    GameState.Instance.slowmo = false;
                } else {
                    windowOpen = false;
                }
            } else {
                Cursor.visible = lastvisible;
                Cursor.lockState = lastlockState;
                Time.timeScale = lastTimeScale;
                GameState.Instance.slowmo = lastSlowMo;
                Game.Instance.playing = true;
                Game.Instance.done = false;
            }
        }

        private void SceneManager_sceneLoaded(Scene scene,LoadSceneMode loadSceneMode) {
            if (scene.buildIndex ==  1&&groundMaterial==null) {
                groundMaterial = GameObject.FindObjectOfType<MeshRenderer>().material;
                return;
            }
            if (scene.buildIndex < 2) return;

            player = PlayerMovement.Instance.gameObject;
            milk = Object.FindObjectOfType<Milk>().gameObject;

            random = new System.Random(seed);

            if (scene.buildIndex == 2) {
                GameObject.Find("RespawnZone").SetActive(false);
                GameObject.Find("Cube (22)").SetActive(false);
            }

            GameObject cgm = new GameObject();
            freeCam = cgm.AddComponent<Camera>();
            freeCam.gameObject.SetActive(false);

            BoxCollider[] boxcolliders = Object.FindObjectsOfType<BoxCollider>();
            List<BoxCollider> groundColliders = new List<BoxCollider>();
            foreach (BoxCollider boxcollider in boxcolliders) {
                if (!boxcollider.isTrigger && boxcollider.gameObject.layer == 9 && boxcollider.GetComponent<Lava>() == null) {
                    groundColliders.Add(boxcollider);
                }
            }


            if (randomPosWalls|| randomRotWalls||randomScaleWalls|| randomSwapWalls) {
                foreach (BoxCollider boxcollider in groundColliders) {
                    if (randomPosWalls && randomChanceWithChaos()) {
                        if (snapToGrid) {
                            boxcollider.gameObject.transform.Translate(   random.Next(-2 * (int)chaos, 2 * (int)chaos)
                                                                        , random.Next(-2 * (int)chaos, 2 * (int)chaos)
                                                                        , random.Next(-2 * (int)chaos, 2 * (int)chaos));
                        } else {
                            boxcollider.gameObject.transform.Translate(   (float)(random.NextDouble() - 0.5) * 15f * chaos
                                                                        , (float)(random.NextDouble() - 0.5) * 15f * chaos
                                                                        , (float)(random.NextDouble() - 0.5) * 15f * chaos);
                        }
                    }
                    if (randomRotWalls && randomChanceWithChaos()) {
                        if (snapToGrid) {
                            boxcollider.gameObject.transform.Rotate(  random.Next(-4, 4) * 90f
                                                                    , random.Next(-4, 4) * 90f
                                                                    , random.Next(-4, 4) * 90f);
                        } else {
                            boxcollider.gameObject.transform.Rotate(  (float)(random.NextDouble() - 0.5) * 10f * chaos
                                                                    , (float)(random.NextDouble() - 0.5) * 10f * chaos
                                                                    , (float)(random.NextDouble() - 0.5) * 10f * chaos);
                        }
                    }
                    if (randomScaleWalls && randomChanceWithChaos()) {
                        Vector3 scale = boxcollider.gameObject.transform.localScale;
                        if (demoncubed) {
                            boxcollider.gameObject.transform.localScale = new Vector3(Mathf.Max(0, 5f, scale.x + (random.Next(-1, 1) * chaos))
                                                                                    , Mathf.Max(0, 5f, scale.x + (random.Next(-1, 1) * chaos))
                                                                                    , Mathf.Max(0, 5f, scale.x + (random.Next(-1, 1) * chaos)));
                        } else {
                            boxcollider.gameObject.transform.localScale = new Vector3(Mathf.Max(1, scale.x + random.Next(-1 * (int)chaos, 1 * (int)chaos))
                                                                                    , Mathf.Max(1, scale.y + random.Next(-1 * (int)chaos, 1 * (int)chaos))
                                                                                    , Mathf.Max(1, scale.z + random.Next(-1 * (int)chaos, 1 * (int)chaos)));
                        }
                    }
                    if (randomSwapWalls && randomChanceWithChaos(2)) {
                        BoxCollider selSpawnBoxCollider = groundColliders[random.Next(0, groundColliders.Count)];
                        Vector3 temp = boxcollider.transform.position;
                        boxcollider.transform.position = selSpawnBoxCollider.transform.position;
                        selSpawnBoxCollider.transform.position = temp;
                        if (!demoncubed) {
                            selSpawnBoxCollider.enabled = false;
                            selSpawnBoxCollider.enabled = true;
                        }
                    }
                    if (!demoncubed) {
                        boxcollider.enabled = false;
                        boxcollider.enabled = true;
                    }

                }
            }



            if (spawnRandomWalls) {
                for (int i = 0; i < random.Next((int)chaos / 2, (int)chaos*2); i++) {
                    BoxCollider selSpawnBoxCollider = groundColliders[random.Next(0, groundColliders.Count)];
                    GameObject gm = LoadsonAPI.PrefabManager.NewCube();
                    gm.GetComponent<MeshRenderer>().material = groundMaterial;
                    gm.transform.position = selSpawnBoxCollider.transform.position;

                    gm.gameObject.transform.Translate(random.Next(-40, 40) , random.Next(-40, 40), random.Next(-40, 40));
                    gm.gameObject.transform.Rotate(random.Next(-4, 4) * 90f, (random.Next(-4, 4)) * 90f, (random.Next(-4, 4)) * 90f);
                    gm.gameObject.transform.localScale = new Vector3(random.Next(1, 30), random.Next(1, 30), random.Next(1, 30));
                    groundColliders.Add(gm.GetComponent<BoxCollider>());
                }
            }

            if (randomPosProps) {

                Object[] props = Object.FindObjectsOfType<global::Object>();
                foreach (global::Object prop in props) {
                    if (prop.transform.parent == null || !prop.transform.parent.gameObject.name.Contains("Table")) {
                        tryDoRandomPosForObject(groundColliders, prop.gameObject.transform);
                    };

                }

                Pickup[] pickups = Object.FindObjectsOfType<Pickup>();
                foreach (Pickup pickup in pickups) {
                    if (pickup.transform.parent == null || pickup.transform.parent.gameObject.name != "WeaponPos") {
                        tryDoRandomPosForObject(groundColliders, pickup.gameObject.transform);
                    };
                }

            }

            if (randomSpawn) {
                tryDoRandomPosForObject(groundColliders, player.transform, () => {
                    return Vector3.Distance(player.transform.position, milk.transform.position) < minGoalDistance;
                });
                PlayerMovement.Instance.rb.velocity = Vector3.zero;
            }

            if (randomGoal) {
                tryDoRandomPosForObject(groundColliders, milk.transform, () => {
                    return Vector3.Distance(player.transform.position, milk.transform.position) < minGoalDistance;
                });
            }

            if (compass) {
                GameObject gm = new GameObject();
                lineRenderer = gm.AddComponent<LineRenderer>();
                lineRenderer.useWorldSpace = true;
                lineRenderer.startColor = Color.green;
                lineRenderer.endColor = Color.blue;
                lineRenderer.SetPosition(0, player.transform.position);
                lineRenderer.SetPosition(1, milk.transform.position);
                lineRenderer.startWidth = 0.1f;
                lineRenderer.endWidth = 0.2f;
                lineRenderer.receiveShadows = false;
                lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lineRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
                lineRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                lineRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                lineRenderer.material.SetInt("_ZWrite", 0);
                lineRenderer.material.DisableKeyword("_ALPHATEST_ON");
                lineRenderer.material.EnableKeyword("_ALPHABLEND_ON");
                lineRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                lineRenderer.material.renderQueue = 3000;
                lineRenderer.material.SetColor("_Color", new Color(1f, 1f, 1f, 0.3f));
                lineRenderer.numCapVertices = 4;
            }

            if (randomStartWeapon) {
                if (PlayerMovement.Instance.spawnWeapon != null) {
                    Object.Destroy(PlayerMovement.Instance.spawnWeapon);
                }
                switch (random.Next(18)) {
                    case 0:
                    case 1:
                    case 2:
                    case 4:
                    case 5:
                        PlayerMovement.Instance.spawnWeapon = null;
                        break;
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                        PlayerMovement.Instance.spawnWeapon = LoadsonAPI.PrefabManager.NewPistol();
                        break;
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                        PlayerMovement.Instance.spawnWeapon = LoadsonAPI.PrefabManager.NewAk47();
                        break;
                    case 14:
                    case 15:
                        PlayerMovement.Instance.spawnWeapon = LoadsonAPI.PrefabManager.NewShotgun();
                        break;
                    case 16:
                        PlayerMovement.Instance.spawnWeapon = LoadsonAPI.PrefabManager.NewGrappler();
                        break;
                    case 17:
                        PlayerMovement.Instance.spawnWeapon = LoadsonAPI.PrefabManager.NewBoomer();
                        break;
                }
            }

        }


        private bool randomChanceWithChaos(float div=1) {
            return random.NextDouble() < Mathf.Lerp(0.1f, 1f, Mathf.Min(1, chaos / 25f))/div;
        }

        private void tryDoRandomPosForObject(List<BoxCollider> groundColliders, Transform t, Func<bool> f) {
            bool hasWorked;
            int tries = 0;
            do {
                hasWorked = doRandomPosForObject(groundColliders, t);
                if (++tries > maxTries) break;
            } while (!hasWorked|| f());
        }

        private void tryDoRandomPosForObject(List<BoxCollider> groundColliders,Transform t) {
            bool hasWorked;
            int tries = 0;
            do {
                hasWorked = doRandomPosForObject(groundColliders, t);
                if (++tries > maxTries) break;
            } while (!hasWorked);
        }



        private bool doRandomPosForObject(List<BoxCollider> groundColliders,Transform t) {
            BoxCollider selSpawnBoxCollider = groundColliders[random.Next(0, groundColliders.Count)];
            Vector3 newpos = selSpawnBoxCollider.gameObject.transform.position;
            float mag = selSpawnBoxCollider.gameObject.transform.localScale.magnitude;
            newpos.y += mag;
            newpos.x += (float)(random.NextDouble() - 0.5) * mag;
            newpos.z += (float)(random.NextDouble() - 0.5) * mag;
            if (selSpawnBoxCollider.Raycast(new Ray(newpos, Vector3.down), out RaycastHit hitInfo, mag * 10)) {
                Vector3 hitPoint = hitInfo.point;
                hitPoint.y += 2;

                Collider[] hitColliders = new Collider[1];
                if (Physics.OverlapSphereNonAlloc(hitPoint, 1, hitColliders, 1 << 9) > 0) {
                    return false;
                } else {
                    t.transform.position = hitPoint;

                    /*hitPoint.y -= 2;
                    createMarker(hitPoint);
                    createMarker(newpos);
                    createLine(hitPoint, newpos);*/
                    return true;
                }
            } else {
                return false;
            }
        }


        private void createMarker(Vector3 pos) {
            GameObject gm = LoadsonAPI.PrefabManager.NewCube();
            gm.transform.position = pos;
            gm.transform.localScale = Vector3.one * 0.5f;
            GameObject.DestroyImmediate(gm.GetComponent<BoxCollider>());
        }

        private void createLine(Vector3 pos1,Vector3 pos2) {
            GameObject gm = new GameObject();
            lineRenderer = gm.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = Color.black;
            lineRenderer.SetPosition(0, pos1);
            lineRenderer.SetPosition(1, pos2);
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.receiveShadows = false;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            lineRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineRenderer.material.SetInt("_ZWrite", 0);
            lineRenderer.material.DisableKeyword("_ALPHATEST_ON");
            lineRenderer.material.EnableKeyword("_ALPHABLEND_ON");
            lineRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            lineRenderer.material.renderQueue = 3000;
            lineRenderer.numCapVertices = 4;
        }

        Rect winrect = new Rect(80, 80, 600, 530);
        public override void OnGUI() {
            if (windowOpen) {
                winrect = GUI.Window(windowID, winrect, mainWindow, "Randomizer Menu");
            }
            if (freeCamOn) {
                GUI.Label(new Rect(10,Screen.height-60,400,20), "Free Cam Mode (Press 'Escape' to Exit)");
            }
        }

        public void mainWindow(int id) {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
            Rect r = new Rect(0, 0, 20, 20);

            r.Set(10, 30, 100, r.height);
            GUI.Label(r, "Settings Code");

            r.Set(10, 50, 200, r.height);
            string newSettingsString = GUI.TextField(r, settingsString);
            if(newSettingsString!= settingsString) {
                if (tryParseSettingsString(newSettingsString)) {
                    settingsString = newSettingsString;
                } else {
                    createSettingsString();
                }
            }

            r.Set(220, r.y, 150, r.height);
            if (GUI.Button(r, "Copy Settings Code")) {
                GUIUtility.systemCopyBuffer = settingsString;
            }





            r.Set(10, r.y+60, 80, r.height);
            if (GUI.Button(r, "New Seed")) {
                seed = UnityEngine.Random.Range(0, Int32.MaxValue);
                createSettingsString();
            }

            r.Set(100, r.y, 80, r.height);
            string t = GUI.TextField(r, seed.ToString());
            if (int.TryParse(t, out int ti)) {
                seed = ti;
                createSettingsString();
            }





            r.Set(10, r.y + 50, 240, r.height);
            bool newrandomPosObjects = GUI.Toggle(r, randomPosWalls, "Random Position for Walls");
            if(newrandomPosObjects!= randomPosWalls) {
                randomPosWalls = newrandomPosObjects;
                createSettingsString();
            }

            r.Set(260, r.y - 3, 140, r.height);
            float newchaos = GUI.HorizontalSlider(r, chaos, 1f, 25.0f);
            if (newchaos != chaos) {
                chaos = (float)Math.Round(newchaos);
                createSettingsString();
            }
            r.Set(300, r.y + 6, 100, r.height);
            GUI.Label(r, "Chaos: " + chaos);


            r.Set(10, r.y + 30-3, 240, r.height);
            bool newrandomRotObjects = GUI.Toggle(r, randomRotWalls, "Random Rotation for Walls");
            if (newrandomRotObjects != randomRotWalls) {
                randomRotWalls = newrandomRotObjects;
                createSettingsString();
            }

            r.Set(260, r.y, 150, r.height);
            bool newSnapToGrid = GUI.Toggle(r, snapToGrid, "Grid Like Behaviour");
            if (newSnapToGrid != snapToGrid) {
                snapToGrid = newSnapToGrid;
                createSettingsString();
            }

            r.Set(10, r.y + 30, 240, r.height);
            bool newrandomScaleObjects = GUI.Toggle(r, randomScaleWalls, "Random Scale for Walls");
            if (newrandomScaleObjects != randomScaleWalls) {
                randomScaleWalls = newrandomScaleObjects;
                createSettingsString();
            }

            r.Set(260, r.y, 150, r.height);
            bool newrandomSwapObjects = GUI.Toggle(r, randomSwapWalls, "Swap Random Walls");
            if (newrandomSwapObjects != randomSwapWalls) {
                randomSwapWalls = newrandomSwapObjects;
                createSettingsString();
            }




            r.Set(10, r.y + 50, 240, r.height);
            bool newrandomPosProps = GUI.Toggle(r, randomPosProps, "Random Props Position");
            if (newrandomPosProps != randomPosProps) {
                randomPosProps = newrandomPosProps;
                createSettingsString();
            }

            r.Set(260, r.y, 240, r.height);
            bool newspawnRandomWalls = GUI.Toggle(r, spawnRandomWalls, "Spawn Random Walls");
            if (newspawnRandomWalls != spawnRandomWalls) {
                spawnRandomWalls = newspawnRandomWalls;
                createSettingsString();
            }

            r.Set(10, r.y + 30, 180, r.height);
            bool newrandomSpawn = GUI.Toggle(r, randomSpawn, "Random Spawn Position");
            if (newrandomSpawn != randomSpawn) {
                randomSpawn = newrandomSpawn;
                createSettingsString();
            }

            r.Set(10, r.y + 30, 150, r.height);
            bool newrandomGoal = GUI.Toggle(r, randomGoal, "Random Goal Position");
            if (newrandomGoal != randomGoal) {
                randomGoal = newrandomGoal;
                createSettingsString();
            }

            r.Set(10, r.y + 30, 240, r.height);
            bool newrandomStartWeapon = GUI.Toggle(r, randomStartWeapon, "Random Starter Weapon");
            if (newrandomStartWeapon != randomStartWeapon) {
                randomStartWeapon = newrandomStartWeapon;
                createSettingsString();
            }



            r.Set(10, r.y +50, 80, r.height);
            bool newcompass = GUI.Toggle(r, compass, "Compass");
            if (newcompass != compass) {
                compass = newcompass;
                createSettingsString();
            }

            r.Set(260, r.y, 140, r.height);
            bool newdemoncubed = GUI.Toggle(r, demoncubed, "Legacy Bug Toggle");
            if (newdemoncubed != demoncubed) {
                demoncubed = newdemoncubed;
                maxTries = demoncubed ? 50 : 1000;
                createSettingsString();
            }

            r.Set(10, r.y+30, 80, r.height);
            if (GUI.Button(r, "Free Cam")) {
                turnOnFreeCam();
            }

            



            r.Set(10, r.y+60, 120, r.height);
            if (GUI.Button(r, "Restart and Apply")) {
                changeWindowOpen(false);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            r.Set(140, r.y, 220, r.height);
            if (GUI.Button(r, "Reseed, Restart and Apply")) {
                seed = UnityEngine.Random.Range(0, Int32.MaxValue);
                createSettingsString();
                changeWindowOpen(false);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

        }

    }
}
