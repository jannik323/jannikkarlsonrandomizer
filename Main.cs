using System;
using System.Collections.Generic;
using System.Text;
using Loadson;
using LoadsonAPI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



namespace Jannik_Randomizer {
    public class Main : Mod {
        private readonly string version = "v1.2.1";
        private readonly string settingsFormatVersion = "v1.1";


        private System.Random random;
        
        private float lastTimeScale = 1;
        private CursorLockMode lastlockState= CursorLockMode.None;
        private bool lastvisible= true;
        private bool lastSlowMo = false;

        private bool windowOpen= false;
        private Rect winrect = new Rect(80, 80, 600, 530);
        private GameObject blockPanel;

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
        private bool legacyBug = false;
        private bool compass = false;

        private LineRenderer compassLineRenderer;

        private readonly float minGoalDistance = 25;
        private int maxTries = 1000;

        private Camera freeCam;
        private bool freeCamOn = false;


        private int windowID;

        private Material groundMaterial;
        private GameObject player;
        private GameObject milk;

        private GameObject debugConsole;

        public override void OnEnable() {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            seed = Environment.TickCount;
            windowID = ImGUI_WID.GetWindowId();

            LoadPlayerPrefSettingsString();
        }

        private void LoadPlayerPrefSettingsString() {
            // for making sure all the settingstrings have the same format
            // like en-US using "." for decimals, but de-DE uses "," for example.
            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentCulture = ci;
            System.Threading.Thread.CurrentThread.CurrentUICulture = ci;

            string loadedsettingsString = PlayerPrefs.GetString("jannik_randomizer_setting", "none");
            if (loadedsettingsString == "none") {
                Loadson.Console.Log("loading from PlayerPrefs failed!: nothin in there");
                CreateSettingsString();
            } else if (!TryParseSettingsString(loadedsettingsString)) {
                Loadson.Console.Log("loading from PlayerPrefs failed!: this shit aint right");
                Loadson.Console.Log("take a lookie: " + loadedsettingsString);
                CreateSettingsString();
            } else {
                settingsString = loadedsettingsString;
            }
        }

        public override void OnDisable() {
            PlayerPrefs.SetString("jannik_randomizer_setting", settingsString);
            PlayerPrefs.Save();
        }

        public override void Update(float deltaTime) {
            if(!freeCamOn && !debugConsole.activeSelf &&Input.GetKeyDown(KeyCode.M)){
                ChangeWindowVisibility(!windowOpen);
            }
            if (freeCamOn && !debugConsole.activeSelf && Input.GetKeyDown(KeyCode.Escape)) {
                TurnOffFreeCam();
            }
            if (windowOpen) {
                Time.timeScale = 0;
            }
            if (compass) {
                compassLineRenderer.SetPosition(0, player.transform.position);
            }

            if (debugConsole.activeSelf) {
                return;
            }

            if (freeCamOn&&!windowOpen) {
                UpdateFreeCam();
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) {
                StartLevel(2);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2)) {
                StartLevel(3);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3)) {
                StartLevel(4);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4)) {
                StartLevel(5);
            }
            if (Input.GetKeyDown(KeyCode.Alpha5)) {
                StartLevel(6);
            }
            if (Input.GetKeyDown(KeyCode.Alpha6)) {
                StartLevel(7);
            }
            if (Input.GetKeyDown(KeyCode.Alpha7)) {
                StartLevel(8);
            }
            if (Input.GetKeyDown(KeyCode.Alpha8)) {
                StartLevel(9);
            }
            if (Input.GetKeyDown(KeyCode.Alpha9)) {
                StartLevel(10);
            }
            if (Input.GetKeyDown(KeyCode.Alpha0)) {
                StartLevel(11);
            }
            if (Input.GetKeyDown(KeyCode.Minus)) {
                StartLevel(12);
            }
            if (Input.GetKeyDown(KeyCode.R)) {
                if (SceneManager.GetActiveScene().buildIndex == 1) return;
                StartLevel(SceneManager.GetActiveScene().buildIndex);
            }
            if (Input.GetKeyDown(KeyCode.K)) {
                Reseed();
                ChangeWindowVisibility(false);
                if (SceneManager.GetActiveScene().buildIndex == 1) return;
                StartLevel(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void Reseed() {
            seed = UnityEngine.Random.Range(0, int.MaxValue);
            CreateSettingsString();
        }

        private void StartLevel(int index) {
            SceneManager.LoadScene(index);
            Game.Instance.StartGame();
        }

        private void TurnOffFreeCam() {
            freeCamOn = false;
            freeCam.gameObject.SetActive(false);
            Time.timeScale = lastTimeScale;
            Game.Instance.playing = true;
            Game.Instance.done = false;
            ChangeWindowVisibility(true);
        }

        private void TurnOnFreeCam() {
            freeCamOn = true;
            freeCam.gameObject.SetActive(true);
            freeCam.gameObject.transform.position = PlayerMovement.Instance.playerCam.transform.position;
            freeCam.gameObject.transform.rotation = PlayerMovement.Instance.playerCam.transform.rotation;
            freeCam.fieldOfView = SaveManager.Instance.state.fov;
            ChangeWindowVisibility(false);
            lastTimeScale = Time.timeScale;
            Time.timeScale = 0;
            Game.Instance.playing = false;
            Game.Instance.done = true;
        }
        private void UpdateFreeCam() {
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
            FreeCamLook();
        }
        private void FreeCamLook() {
            float mouseX = Input.GetAxis("Mouse X") * 50f * Time.fixedDeltaTime ;
            float mouseY = Input.GetAxis("Mouse Y") * 50f * Time.fixedDeltaTime ;
            freeCam.transform.localRotation = Quaternion.Euler(freeCam.transform.localRotation.eulerAngles.x - mouseY, freeCam.transform.localRotation.eulerAngles.y + mouseX, 0);
        }
        private void CreateFreeCam() {
            GameObject cgm = new GameObject();
            freeCam = cgm.AddComponent<Camera>();
            freeCam.gameObject.SetActive(false);
        }
        private void CreateSettingsString() {
            string rawString = $"{settingsFormatVersion}|{seed}|{chaos}|{(snapToGrid?"1":"0")}|{(randomPosWalls?"1":"0")}|{(randomRotWalls ? "1":"0")}|{(randomScaleWalls ? "1":"0")}|{(randomSwapWalls ? "1":"0")}|{(randomPosProps ? "1":"0")}|{(spawnRandomWalls ? "1":"0")}|{(randomStartWeapon ? "1":"0")}|{(randomSpawn ? "1":"0")}|{(randomGoal ? "1":"0")}|{(compass ? "1":"0")}|{(legacyBug ? "1":"0")}";
            settingsString = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawString));
        }

        private bool TryParseSettingsString(string sString) {
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
                    legacyBug = true;
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
                legacyBug = split[14] == "1";
                return true;
            } catch (Exception e) {
                Loadson.Console.Log("settings parsing error: " + e.Message);
                return false;
            }
        }

        private void ChangeWindowVisibility(bool state) {
            windowOpen = state;
            if (windowOpen) {
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
                blockPanel.SetActive(true);
            } else {
                Cursor.visible = lastvisible;
                Cursor.lockState = lastlockState;
                Time.timeScale = lastTimeScale;
                GameState.Instance.slowmo = lastSlowMo;
                Game.Instance.playing = true;
                Game.Instance.done = false;
                blockPanel.SetActive(false);
            }
        }
        private void CreateBlockPanel() {
            DefaultControls.Resources uiResources = new DefaultControls.Resources();
            uiResources.background = uiResources.standard;
            blockPanel = DefaultControls.CreatePanel(uiResources);
            blockPanel.SetActive(false);
            blockPanel.transform.SetParent(UIManger.Instance.transform);
            RectTransform rect = blockPanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.localPosition = Vector3.zero;
            rect.localScale = Vector3.one;
            rect.offsetMin = new Vector2(0, 0);
            rect.offsetMax = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(0, 0);
        }
        private void SceneManager_sceneLoaded(Scene scene,LoadSceneMode loadSceneMode) {
            freeCamOn = false;
            
            // fetch default ground material for random spawned walls to use
            if (scene.buildIndex == 1) {
                if(groundMaterial == null) {
                    groundMaterial = GameObject.FindObjectOfType<MeshRenderer>().material;
                }
                if (blockPanel==null) {
                    CreateBlockPanel();
                }
                return;
            }
            blockPanel.SetActive(false);

            debugConsole = GameObject.FindObjectOfType<Debug>().console.gameObject;

            if (scene.buildIndex < 2) return;

            player = PlayerMovement.Instance.gameObject;
            milk = Object.FindObjectOfType<Milk>().gameObject;

            random = new System.Random(seed);
            CreateFreeCam();

            //tutorial fixing
            if (scene.buildIndex == 2) {
                GameObject.Find("RespawnZone").SetActive(false); // deactivate respawn zones
                GameObject.Find("Cube (22)").SetActive(false);   // deactivate duplicate cube
            }

            List<BoxCollider> groundColliders = getGroundColliders();

            if (randomPosWalls || randomRotWalls || randomScaleWalls || randomSwapWalls) {
                foreach (BoxCollider boxcollider in groundColliders) {
                    RandomizeWall(boxcollider, groundColliders);
                }
            }


            if (spawnRandomWalls) {
                for (int i = 0; i < random.Next((int)chaos / 2, (int)chaos * 2); i++) {
                    createRandomWall(groundColliders);
                }
            }

            if (randomPosProps) {

                Object[] props = Object.FindObjectsOfType<global::Object>();
                foreach (global::Object prop in props) {
                    if (prop.transform.parent == null || !prop.transform.parent.gameObject.name.Contains("Table")) {
                        TryDoRandomPosForObject(groundColliders, prop.gameObject.transform);
                    };

                }

                Pickup[] pickups = Object.FindObjectsOfType<Pickup>();
                foreach (Pickup pickup in pickups) {
                    if (pickup.transform.parent == null || pickup.transform.parent.gameObject.name != "WeaponPos") {
                        TryDoRandomPosForObject(groundColliders, pickup.gameObject.transform);
                    };
                }

            }

            if (randomSpawn) {
                TryDoRandomPosForObject(groundColliders, player.transform, () => {
                    return Vector3.Distance(player.transform.position, milk.transform.position) < minGoalDistance;
                });
            }

            if (randomGoal) {
                TryDoRandomPosForObject(groundColliders, milk.transform, () => {
                    return Vector3.Distance(player.transform.position, milk.transform.position) < minGoalDistance;
                });
            }

            if (compass) {
                CreateCompass();
            }

            if (randomStartWeapon) {
                PickRandomStartWeapon();
            }

        }

        private static List<BoxCollider> getGroundColliders() {
            BoxCollider[] boxcolliders = Object.FindObjectsOfType<BoxCollider>();
            List<BoxCollider> groundColliders = new List<BoxCollider>();
            foreach (BoxCollider boxcollider in boxcolliders) {
                if (!boxcollider.isTrigger && boxcollider.gameObject.layer == 9 && boxcollider.GetComponent<Lava>() == null) {
                    groundColliders.Add(boxcollider);
                }
            }

            return groundColliders;
        }

        private void createRandomWall(List<BoxCollider> groundColliders) {
            BoxCollider selSpawnBoxCollider = groundColliders[random.Next(0, groundColliders.Count)];
            GameObject gm = LoadsonAPI.PrefabManager.NewCube();
            gm.GetComponent<MeshRenderer>().material = groundMaterial;
            gm.transform.position = selSpawnBoxCollider.transform.position;

            gm.gameObject.transform.Translate(random.Next(-40, 40), random.Next(-40, 40), random.Next(-40, 40));
            gm.gameObject.transform.Rotate(random.Next(-4, 4) * 90f, (random.Next(-4, 4)) * 90f, (random.Next(-4, 4)) * 90f);
            gm.gameObject.transform.localScale = new Vector3(random.Next(1, 30), random.Next(1, 30), random.Next(1, 30));
            groundColliders.Add(gm.GetComponent<BoxCollider>());
        }

        private void RandomizeWall(BoxCollider boxcollider, List<BoxCollider> groundColliders) {
            if (randomPosWalls && RandomChanceWithChaos()) {
                if (snapToGrid) {
                    boxcollider.gameObject.transform.Translate(
                        random.Next(-2 * (int)chaos, 2 * (int)chaos), 
                        random.Next(-2 * (int)chaos, 2 * (int)chaos), 
                        random.Next(-2 * (int)chaos, 2 * (int)chaos));
                } else {
                    boxcollider.gameObject.transform.Translate(
                        (float)(random.NextDouble() - 0.5) * 15f * chaos, 
                        (float)(random.NextDouble() - 0.5) * 15f * chaos, 
                        (float)(random.NextDouble() - 0.5) * 15f * chaos);
                }
            }
            if (randomRotWalls && RandomChanceWithChaos()) {
                if (snapToGrid) {
                    boxcollider.gameObject.transform.Rotate(
                        random.Next(-4, 4) * 90f, 
                        random.Next(-4, 4) * 90f, 
                        random.Next(-4, 4) * 90f);
                } else {
                    boxcollider.gameObject.transform.Rotate(
                        (float)(random.NextDouble() - 0.5) * 10f * chaos, 
                        (float)(random.NextDouble() - 0.5) * 10f * chaos,
                        (float)(random.NextDouble() - 0.5) * 10f * chaos);
                }
            }
            if (randomScaleWalls && RandomChanceWithChaos()) {
                Vector3 scale = boxcollider.gameObject.transform.localScale;
                if (legacyBug) {
                    boxcollider.gameObject.transform.localScale = new Vector3(
                        Mathf.Max(0, 5f, scale.x + (random.Next(-1, 1) * chaos)), 
                        Mathf.Max(0, 5f, scale.x + (random.Next(-1, 1) * chaos)), 
                        Mathf.Max(0, 5f, scale.x + (random.Next(-1, 1) * chaos)));
                } else {
                    boxcollider.gameObject.transform.localScale = new Vector3(
                        Mathf.Max(1, scale.x + random.Next(-1 * (int)chaos, 1 * (int)chaos)),
                        Mathf.Max(1, scale.y + random.Next(-1 * (int)chaos, 1 * (int)chaos)), 
                        Mathf.Max(1, scale.z + random.Next(-1 * (int)chaos, 1 * (int)chaos)));
                }
            }
            if (randomSwapWalls && RandomChanceWithChaos(2)) {
                BoxCollider selSpawnBoxCollider = groundColliders[random.Next(0, groundColliders.Count)];
                Vector3 temp = boxcollider.transform.position;
                boxcollider.transform.position = selSpawnBoxCollider.transform.position;
                selSpawnBoxCollider.transform.position = temp;
                if (!legacyBug) {
                    selSpawnBoxCollider.enabled = false;
                    selSpawnBoxCollider.enabled = true;
                }
            }
            if (!legacyBug) {
                boxcollider.enabled = false;
                boxcollider.enabled = true;
            }
        }

        private void CreateCompass() {
            GameObject gm = new GameObject();
            compassLineRenderer = gm.AddComponent<LineRenderer>();
            compassLineRenderer.useWorldSpace = true;
            compassLineRenderer.startColor = Color.green;
            compassLineRenderer.endColor = Color.blue;
            compassLineRenderer.SetPosition(0, player.transform.position);
            compassLineRenderer.SetPosition(1, milk.transform.position);
            compassLineRenderer.startWidth = 0.1f;
            compassLineRenderer.endWidth = 0.2f;
            compassLineRenderer.receiveShadows = false;
            compassLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            compassLineRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            compassLineRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            compassLineRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            compassLineRenderer.material.SetInt("_ZWrite", 0);
            compassLineRenderer.material.DisableKeyword("_ALPHATEST_ON");
            compassLineRenderer.material.EnableKeyword("_ALPHABLEND_ON");
            compassLineRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            compassLineRenderer.material.renderQueue = 3000;
            compassLineRenderer.material.SetColor("_Color", new Color(1f, 1f, 1f, 0.3f));
            compassLineRenderer.numCapVertices = 4;
        }

        private void PickRandomStartWeapon() {
            if (PlayerMovement.Instance.spawnWeapon != null) {
                Object.DestroyImmediate(PlayerMovement.Instance.spawnWeapon);
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


        private bool RandomChanceWithChaos(float div=1) {
            return random.NextDouble() < Mathf.Lerp(0.1f, 1f, Mathf.Min(1, chaos / 25f))/div;
        }

        private void TryDoRandomPosForObject(List<BoxCollider> groundColliders, Transform t, Func<bool> f) {
            bool hasWorked;
            int tries = 0;
            do {
                hasWorked = DoRandomPosForObject(groundColliders, t);
                if (++tries > maxTries) break;
            } while (!hasWorked|| f());
        }

        private void TryDoRandomPosForObject(List<BoxCollider> groundColliders,Transform t) {
            bool hasWorked;
            int tries = 0;
            do {
                hasWorked = DoRandomPosForObject(groundColliders, t);
                if (++tries > maxTries) break;
            } while (!hasWorked);
        }



        private bool DoRandomPosForObject(List<BoxCollider> groundColliders,Transform t) {
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


        private void CreateMarker(Vector3 pos) {
            GameObject gm = LoadsonAPI.PrefabManager.NewCube();
            gm.transform.position = pos;
            gm.transform.localScale = Vector3.one * 0.5f;
            GameObject.DestroyImmediate(gm.GetComponent<BoxCollider>());
        }

        private void CreateLine(Vector3 pos1,Vector3 pos2) {
            GameObject gm = new GameObject();
            compassLineRenderer = gm.AddComponent<LineRenderer>();
            compassLineRenderer.useWorldSpace = true;
            compassLineRenderer.startColor = Color.white;
            compassLineRenderer.endColor = Color.black;
            compassLineRenderer.SetPosition(0, pos1);
            compassLineRenderer.SetPosition(1, pos2);
            compassLineRenderer.startWidth = 0.1f;
            compassLineRenderer.endWidth = 0.1f;
            compassLineRenderer.receiveShadows = false;
            compassLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            compassLineRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            compassLineRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            compassLineRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            compassLineRenderer.material.SetInt("_ZWrite", 0);
            compassLineRenderer.material.DisableKeyword("_ALPHATEST_ON");
            compassLineRenderer.material.EnableKeyword("_ALPHABLEND_ON");
            compassLineRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            compassLineRenderer.material.renderQueue = 3000;
            compassLineRenderer.numCapVertices = 4;
        }
        public override void OnGUI() {
            if (windowOpen) {
                winrect = GUI.Window(windowID, winrect, MainWindow, "Randomizer Menu ("+version+")");
            }
            GUI.Label(new Rect(0, Screen.height - 35, 400, 20), "Seed: "+seed);
            if (freeCamOn) {
                GUI.Label(new Rect(10,Screen.height-60,400,20), "Free Cam Mode (Press 'Escape' to Exit)");
            }
        }
        private void MainWindow(int id) {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
            Rect r = new Rect(0, 0, 20, 20);

            r.Set(10, 30, 100, r.height);
            GUI.Label(r, "Settings Code");

            r.Set(10, 50, 200, r.height);
            string newSettingsString = GUI.TextField(r, settingsString);
            if(newSettingsString!= settingsString) {
                if (TryParseSettingsString(newSettingsString)) {
                    settingsString = newSettingsString;
                } else {
                    CreateSettingsString();
                }
            }

            r.Set(220, r.y, 150, r.height);
            if (GUI.Button(r, "Copy Settings Code")) {
                GUIUtility.systemCopyBuffer = settingsString;
            }





            r.Set(10, r.y+60, 80, r.height);
            if (GUI.Button(r, "Reseed")) {
                Reseed();
            }

            r.Set(100, r.y, 80, r.height);
            string t = GUI.TextField(r, seed.ToString());
            if (int.TryParse(t, out int ti)) {
                seed = ti;
                CreateSettingsString();
            }





            r.Set(10, r.y + 50, 240, r.height);
            bool newrandomPosObjects = GUI.Toggle(r, randomPosWalls, "Random Position for Walls");
            if(newrandomPosObjects!= randomPosWalls) {
                randomPosWalls = newrandomPosObjects;
                CreateSettingsString();
            }

            r.Set(260, r.y - 3, 140, r.height);
            float newchaos = GUI.HorizontalSlider(r, chaos, 1f, 25.0f);
            if (newchaos != chaos) {
                chaos = (float)Math.Round(newchaos);
                CreateSettingsString();
            }
            r.Set(300, r.y + 6, 100, r.height);
            GUI.Label(r, "Chaos: " + chaos);


            r.Set(10, r.y + 30-3, 240, r.height);
            bool newrandomRotObjects = GUI.Toggle(r, randomRotWalls, "Random Rotation for Walls");
            if (newrandomRotObjects != randomRotWalls) {
                randomRotWalls = newrandomRotObjects;
                CreateSettingsString();
            }

            r.Set(260, r.y, 150, r.height);
            bool newSnapToGrid = GUI.Toggle(r, snapToGrid, "Grid Like Behaviour");
            if (newSnapToGrid != snapToGrid) {
                snapToGrid = newSnapToGrid;
                CreateSettingsString();
            }

            r.Set(10, r.y + 30, 240, r.height);
            bool newrandomScaleObjects = GUI.Toggle(r, randomScaleWalls, "Random Scale for Walls");
            if (newrandomScaleObjects != randomScaleWalls) {
                randomScaleWalls = newrandomScaleObjects;
                CreateSettingsString();
            }

            r.Set(260, r.y, 150, r.height);
            bool newrandomSwapObjects = GUI.Toggle(r, randomSwapWalls, "Swap Random Walls");
            if (newrandomSwapObjects != randomSwapWalls) {
                randomSwapWalls = newrandomSwapObjects;
                CreateSettingsString();
            }




            r.Set(10, r.y + 50, 240, r.height);
            bool newrandomPosProps = GUI.Toggle(r, randomPosProps, "Random Props Position");
            if (newrandomPosProps != randomPosProps) {
                randomPosProps = newrandomPosProps;
                CreateSettingsString();
            }

            r.Set(260, r.y, 240, r.height);
            bool newspawnRandomWalls = GUI.Toggle(r, spawnRandomWalls, "Spawn Random Walls");
            if (newspawnRandomWalls != spawnRandomWalls) {
                spawnRandomWalls = newspawnRandomWalls;
                CreateSettingsString();
            }

            r.Set(10, r.y + 30, 180, r.height);
            bool newrandomSpawn = GUI.Toggle(r, randomSpawn, "Random Spawn Position");
            if (newrandomSpawn != randomSpawn) {
                randomSpawn = newrandomSpawn;
                CreateSettingsString();
            }

            r.Set(10, r.y + 30, 150, r.height);
            bool newrandomGoal = GUI.Toggle(r, randomGoal, "Random Goal Position");
            if (newrandomGoal != randomGoal) {
                randomGoal = newrandomGoal;
                CreateSettingsString();
            }

            r.Set(10, r.y + 30, 240, r.height);
            bool newrandomStartWeapon = GUI.Toggle(r, randomStartWeapon, "Random Starter Weapon");
            if (newrandomStartWeapon != randomStartWeapon) {
                randomStartWeapon = newrandomStartWeapon;
                CreateSettingsString();
            }



            r.Set(10, r.y +50, 80, r.height);
            bool newcompass = GUI.Toggle(r, compass, "Compass");
            if (newcompass != compass) {
                compass = newcompass;
                CreateSettingsString();
            }

            r.Set(260, r.y, 140, r.height);
            bool newdemoncubed = GUI.Toggle(r, legacyBug, "Legacy Bug Toggle");
            if (newdemoncubed != legacyBug) {
                legacyBug = newdemoncubed;
                maxTries = legacyBug ? 50 : 1000;
                CreateSettingsString();
            }

            r.Set(10, r.y+30, 80, r.height);
            if (GUI.Button(r, "Free Cam")) {
                TurnOnFreeCam();
            }

            



            r.Set(10, r.y+60, 100, r.height);
            if (GUI.Button(r, "Restart (R)")) {
                ChangeWindowVisibility(false);
                if (SceneManager.GetActiveScene().buildIndex != 1) {
                    StartLevel(SceneManager.GetActiveScene().buildIndex);
                };
            }

            r.Set(120, r.y, 180, r.height);
            if (GUI.Button(r, "Reseed and Restart (K)")) {
                Reseed();
                ChangeWindowVisibility(false);
                if (SceneManager.GetActiveScene().buildIndex != 1) {
                    StartLevel(SceneManager.GetActiveScene().buildIndex);
                };
            }

        }

    }
}
