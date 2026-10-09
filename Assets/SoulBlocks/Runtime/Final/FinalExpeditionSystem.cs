using System.Linq;
using UnityEngine;

namespace SoulBlocks
{
    // Despertar staging: a toast, a beam above the Nucleo visible from afar, and a persistent
    // "Rompé el Núcleo: X m" HUD line while an expedition is active. The Nucleo itself (shape,
    // HP, unlock-gated activation) belongs to og/prisiones' PrisonSystem/PrisonView; this only
    // reacts to the unlock. Also owns Run.playTimeSeconds: ticks unscaled time whenever no modal
    // has Keys.Blocked set.
    public sealed class FinalExpeditionSystem : MonoBehaviour
    {
        PrisonLayout nucleo;
        GameObject beam;
        bool awakened;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => GameRoot.Register<FinalExpeditionSystem>();

        void OnEnable()
        {
            GameEvents.MapBuilt += OnMapBuilt;
            GameEvents.UnlockGranted += OnUnlockGranted;
            GameEvents.GameWon += OnGameWon;
            Hud.Register(760, DrawHud);
        }

        void OnDisable()
        {
            GameEvents.MapBuilt -= OnMapBuilt;
            GameEvents.UnlockGranted -= OnUnlockGranted;
            GameEvents.GameWon -= OnGameWon;
            Hud.Unregister(DrawHud);
        }

        void Update()
        {
            var root = GameRoot.Instance;
            if (root != null && root.Run != null && !Keys.Blocked)
                root.Run.playTimeSeconds += Time.unscaledDeltaTime;
        }

        void OnMapBuilt(MapLayout layout)
        {
            nucleo = layout.prisons.FirstOrDefault(p => p.tier == PrisonTier.Nucleo);
            beam = null;
            awakened = false;

            var run = GameRoot.Instance.Run;
            // A save loaded after the unlock (but before victory) must see the beam/toast again:
            // the scene (and every GameObject in it, no DontDestroyOnLoad) was just rebuilt from
            // scratch, so there is nothing left over from the first time this fired.
            if (nucleo != null && !run.victory && run.HasUnlock(UnlockIds.Despertar))
                Awaken();
        }

        void OnUnlockGranted(string id)
        {
            if (id != UnlockIds.Despertar || awakened) return;
            Awaken();
        }

        void Awaken()
        {
            awakened = true;
            Hud.Toast("El Núcleo despertó al final de una rama profunda");
            SpawnBeam();
        }

        void SpawnBeam()
        {
            if (nucleo == null || beam != null) return;
            float height = Balance.NucleoBeamHeight;
            Vector3 center = nucleo.position + Vector3.up * (height * .5f);
            // Unity's primitive cylinder is 2 world units tall at localScale 1, so half-height
            // of the desired world height is exactly the scale to apply on Y.
            var scale = new Vector3(Balance.NucleoBeamThickness, height * .5f, Balance.NucleoBeamThickness);
            beam = Placeholder.Make(PrimitiveType.Cylinder, Color.white, "Nucleo Beam", null, center, scale, false, Balance.NucleoBeamEmission);
        }

        void OnGameWon()
        {
            if (beam == null) return;
            Destroy(beam);
            beam = null;
        }

        void DrawHud()
        {
            var root = GameRoot.Instance;
            if (root == null || root.Run == null || root.Player == null || nucleo == null) return;
            if (!awakened || root.Run.victory || !root.Run.inExpedition) return;

            float distance = Vector3.Distance(root.Player.transform.position, nucleo.position);
            Hud.SideLine("Rompé el Núcleo: " + distance.ToString("0") + " m");
        }
    }
}
