using UnityEngine;
using Poligon.Network;
using Poligon.Player;
using Poligon.Weapons;
using Poligon.World;
using Poligon.UI;
using Poligon.Combat;

namespace Poligon
{
    public class GameBootstrap : MonoBehaviour
    {
        public bool buildArena = true;
        public bool createPlayer = true;
        public bool spawnBots = true;

        void Awake()
        {
            foreach (var cam in Object.FindObjectsOfType<Camera>())
            {
                if (cam.gameObject.name == "Main Camera")
                {
                    cam.enabled = false;
                    var al = cam.GetComponent<AudioListener>();
                    if (al) al.enabled = false;
                }
            }

            if (FindObjectOfType<MultiplayerManager>() == null)
            {
                var mp = new GameObject("MultiplayerManager");
                mp.AddComponent<MultiplayerManager>();
            }

            if (buildArena && FindObjectOfType<ArenaBuilder>() == null)
            {
                var arena = new GameObject("Arena");
                arena.AddComponent<ArenaBuilder>();
            }

            if (createPlayer && FindObjectOfType<PlayerController>() == null)
                CreatePlayer();

            if (spawnBots)
                SpawnTrainingBots();

            if (FindObjectOfType<HudController>() == null)
            {
                var hud = new GameObject("HUD");
                hud.AddComponent<HudController>();
            }
        }

        void CreatePlayer()
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.skinWidth = 0.05f;
            cc.minMoveDistance = 0f;
            player.transform.position = new Vector3(0f, 1.2f, 8f);

            var camGo = new GameObject("PlayerCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(player.transform);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 75f;
            camGo.AddComponent<AudioListener>();

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(camGo.transform);
            muzzle.localPosition = new Vector3(0.28f, -0.22f, 0.55f);

            var pc = player.AddComponent<PlayerController>();
            pc.PlayerCamera = cam;

            var wc = player.AddComponent<WeaponController>();
            wc.muzzle = muzzle;
            pc.weapons = wc;

            var ground = new GameObject("GroundCheck").transform;
            ground.SetParent(player.transform);
            ground.localPosition = new Vector3(0f, 0.05f, 0f);
            pc.groundCheck = ground;

            var flash = new GameObject("MuzzleFlash");
            flash.transform.SetParent(muzzle);
            flash.transform.localPosition = Vector3.zero;
            var light = flash.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4f;
            light.intensity = 3f;
            light.color = new Color(1f, 0.85f, 0.4f);
            light.enabled = false;
            wc.muzzleFlashLight = light;

            var audio = player.AddComponent<AudioSource>();
            audio.spatialBlend = 0f;
            wc.audioSource = audio;

            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "ViewGun";
            gun.transform.SetParent(muzzle);
            gun.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            gun.transform.localScale = new Vector3(0.08f, 0.1f, 0.45f);
            Object.Destroy(gun.GetComponent<Collider>());
            var gr = gun.GetComponent<Renderer>();
            var sh = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            if (sh != null && gr != null)
            {
                var mat = new Material(sh);
                if (mat.HasProperty("_Color")) mat.color = new Color(0.12f, 0.13f, 0.15f);
                gr.sharedMaterial = mat;
            }
        }

        void SpawnTrainingBots()
        {
            // Don't spawn twice
            if (GameObject.Find("Bot_1") != null) return;

            var positions = new[]
            {
                new Vector3(5, 0, -5),
                new Vector3(-8, 0, 3),
                new Vector3(12, 0, 10),
                new Vector3(-4, 0, -12)
            };
            var shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var mat = shader != null ? new Material(shader) : null;
            if (mat != null && mat.HasProperty("_Color"))
                mat.color = new Color(0.85f, 0.2f, 0.18f);

            for (int i = 0; i < positions.Length; i++)
            {
                var bot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                bot.name = "Bot_" + (i + 1);
                bot.transform.position = positions[i] + Vector3.up;
                bot.transform.localScale = new Vector3(0.9f, 1f, 0.9f);
                if (mat != null) bot.GetComponent<Renderer>().sharedMaterial = mat;
                bot.AddComponent<BotTarget>();

                // Head visual only — no custom tag (avoids "Tag Head is not defined")
                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(bot.transform);
                head.transform.localPosition = new Vector3(0, 0.75f, 0);
                head.transform.localScale = new Vector3(0.55f, 0.35f, 0.55f);
                Object.Destroy(head.GetComponent<Collider>());
                if (mat != null) head.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }
    }
}
