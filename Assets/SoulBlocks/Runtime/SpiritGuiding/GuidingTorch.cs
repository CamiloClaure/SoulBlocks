using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    public enum TorchState { Altar, Carried, Placed }

    // The torch that guides souls: F takes it from its altar, places it (a call that grows its
    // radius every pulse) or returns it; T lights it. While lit it calls wandering souls
    // (WanderingSpirit); returned to the altar it receives them into the reserve.
    public sealed class GuidingTorch : MonoBehaviour
    {
        public Transform altarSocket;
        public Transform model;
        public Light flameLight;
        public Renderer flame;
        public LineRenderer rangeRing;
        public Material litMaterial, unlitMaterial;
        public float attractionRadius = 12f;
        [Min(1)] public float initialRadius = 4f;
        [Min(.1f)] public float radiusStep = 2f;
        [Min(.1f)] public float pulseInterval = 3f;
        public float CurrentRadius => State == TorchState.Placed ? placedRadius : State == TorchState.Altar ? attractionRadius : CarriedRadius;
        // In the hand the reach is StatId.TorchRadius (Alcance de la antorcha), initialRadius
        // without a run.
        float CarriedRadius => GameRoot.Instance != null && GameRoot.Instance.Stats != null ? GameRoot.Instance.Stats.Get(StatId.TorchRadius) : initialRadius;
        public bool ReadyToRetrieve { get; private set; }
        public float SecondsToPulse => Mathf.Max(0, pulseInterval - pulseTimer);
        float placedRadius, pulseTimer;
        public Vector3 handOffset = new Vector3(0, .03f, .08f);
        public TorchState State { get; private set; } = TorchState.Altar;
        public bool Lit { get; private set; }
        public int GuidedSouls { get; private set; }
        public Vector3 LightPosition => transform.position + Vector3.up * .55f;
        public Vector3 AltarPosition => altarSocket.position;
        Vector3 placedPosition;
        SoulPlayer Player => GameRoot.Instance != null ? GameRoot.Instance.Player : null;
        void OnEnable() => Hud.Register(720, Draw);
        void OnDisable() => Hud.Unregister(Draw);
        void Start() { if (altarSocket != null) transform.position = altarSocket.position; SetLit(false); }
        void Update()
        {
            if (!Keys.Blocked && Time.timeScale > 0) TickCall(Time.deltaTime);
            if (Player == null || !Player.readInput || Keys.Blocked || Keys.FreeCursorHeld || !Application.isFocused || Time.timeScale <= 0) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.fKey.wasPressedThisFrame) Interact(Player);
            if (keyboard.tKey.wasPressedThisFrame && (State == TorchState.Carried || Vector3.Distance(Player.transform.position, transform.position) <= 2.8f)) SetLit(!Lit);
        }
        public void TickCall(float seconds)
        {
            if (State != TorchState.Placed || !Lit || ReadyToRetrieve || seconds <= 0) return;
            pulseTimer += seconds;
            while (pulseTimer >= Mathf.Max(.1f, pulseInterval) && !ReadyToRetrieve)
            {
                pulseTimer -= Mathf.Max(.1f, pulseInterval);
                placedRadius = Mathf.Min(attractionRadius, placedRadius + Mathf.Max(.1f, radiusStep));
                if (placedRadius >= attractionRadius)
                {
                    ReadyToRetrieve = true;
                    Hud.Toast("¡Llamado completo! Recoge la antorcha con F para guiar las almas.", 5);
                }
            }
        }
        public bool Calls(Vector3 position, bool alreadyFollowing) => Lit && Vector3.Distance(position, LightPosition)
            <= (alreadyFollowing ? attractionRadius * 1.5f : CurrentRadius);
        public bool Interact(SoulPlayer player)
        {
            if (player == null || altarSocket == null || Keys.Blocked) return false;
            if (State != TorchState.Carried)
            {
                if (Vector3.Distance(player.transform.position, transform.position) > 2.8f) return false;
                State = TorchState.Carried; ReadyToRetrieve = false; SetLit(true); return true;
            }
            if (Vector3.Distance(player.transform.position, altarSocket.position) <= 3f)
            {
                State = TorchState.Altar; transform.SetParent(null, true); transform.position = altarSocket.position; transform.localScale = Vector3.one; return true;
            }
            Vector3 forward = Quaternion.Euler(0, player.Yaw, 0) * Vector3.forward;
            Vector3 point = player.transform.position + forward * 1.5f;
            if (!Physics.Raycast(point + Vector3.up * 2, Vector3.down, out var ground, 5, (1 << 8) | (1 << 9), QueryTriggerInteraction.Ignore))
            { Hud.Toast("Coloca la antorcha sobre suelo firme"); return false; }
            State = TorchState.Placed; placedPosition = ground.point + Vector3.up * 1.1f;
            placedRadius = Mathf.Min(initialRadius, attractionRadius); pulseTimer = 0; ReadyToRetrieve = false;
            transform.SetParent(null, true); transform.localScale = Vector3.one; transform.position = placedPosition;
            return true;
        }
        public void SetLit(bool value)
        {
            Lit = value;
            if (flameLight != null) flameLight.enabled = value;
            if (flame != null) flame.sharedMaterial = value ? litMaterial : unlitMaterial;
        }
        void LateUpdate()
        {
            if (State == TorchState.Carried && Player != null)
            {
                var hand = Player.TorchHand;
                var parent = hand != null ? hand : Player.transform;
                if (transform.parent != parent) transform.SetParent(parent, true);
                var scale = parent.lossyScale;
                transform.localScale = new Vector3(1 / Mathf.Max(.0001f, Mathf.Abs(scale.x)), 1 / Mathf.Max(.0001f, Mathf.Abs(scale.y)), 1 / Mathf.Max(.0001f, Mathf.Abs(scale.z)));
                var rotation = Quaternion.Euler(0, Player.Yaw, 0);
                transform.position = (hand != null ? hand.position : Player.transform.position + rotation * new Vector3(-.4f, 1, 0)) + rotation * handOffset;
                transform.rotation = rotation;
            }
            else if (altarSocket != null)
            {
                transform.position = (State == TorchState.Altar ? altarSocket.position : placedPosition) + Vector3.up * (Mathf.Sin(Time.time * 2) * .07f);
                transform.rotation = Quaternion.Euler(0, Time.time * 20, 0);
            }
            if (rangeRing != null)
            {
                if (flameLight != null) flameLight.range = CurrentRadius;
                rangeRing.enabled = Lit && State != TorchState.Carried;
                for (int i = 0; i <= 64; i++)
                {
                    float angle = i * Mathf.PI * 2 / 64;
                    var p = transform.position + new Vector3(Mathf.Cos(angle) * CurrentRadius, 0, Mathf.Sin(angle) * CurrentRadius);
                    if (Physics.Raycast(p + Vector3.up * 5, Vector3.down, out var hit, 20, (1 << 8) | (1 << 9))) p.y = hit.point.y + .06f;
                    else p.y -= 1;
                    rangeRing.SetPosition(i, p);
                }
            }
        }
        public void ReceiveSoul()
        {
            GuidedSouls++;
            var root = GameRoot.Instance;
            if (root != null && root.Run != null)
            {
                root.Run.reserveSouls++; root.Run.totalSoulsCollected++;
                GameEvents.RaiseSoulsCollected(1);
            }
        }
        void Draw()
        {
            if (Keys.Blocked || Player == null) return;
            Hud.Line("Antorcha: " + (Lit ? "encendida" : "apagada") + " · Almas guiadas: " + GuidedSouls);
            if (GameRoot.Instance != null && GameRoot.Instance.Run != null)
                Hud.Line("Energía espiritual: " + GameRoot.Instance.Run.reserveSouls + " · Cristales: " + GameRoot.Instance.Run.collectedCrystals);
            Hud.Line("F: tomar / colocar / devolver al altar · T: luz");
            if (State == TorchState.Placed)
            {
                Hud.Line("Radio de llamado: " + CurrentRadius.ToString("0") + " / " + attractionRadius.ToString("0") + " m");
                Hud.Line(ReadyToRetrieve ? "¡LLAMADO COMPLETO! Recoge la antorcha con F." : Lit ? "Próxima expansión en " + SecondsToPulse.ToString("0.0") + " s · Puedes seguir picando." : "Llamado pausado: enciende la antorcha con T.");
                if (ReadyToRetrieve) Hud.WorldLabel(transform.position + Vector3.up * 1.5f, "¡LISTA! F: recoger");
            }
            if (State == TorchState.Carried) Hud.Line("La luz atrae almas cercanas. Devuélvela al altar para recibir energía espiritual.");
            else if (Vector3.Distance(Player.transform.position, transform.position) < 2.8f) Hud.WorldLabel(transform.position + Vector3.up, "F: tomar antorcha");
        }
    }
}
