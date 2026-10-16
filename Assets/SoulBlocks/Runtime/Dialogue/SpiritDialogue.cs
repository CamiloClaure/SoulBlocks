using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SoulBlocks
{
    [Serializable]
    public sealed class SpiritDialogueLine
    {
        [TextArea(2, 6)] public string texto = "Hola.";
    }

    public sealed class SpiritDialogue : MonoBehaviour
    {
        [Header("Identidad del espiritu")]
        public string nombre = "Espíritu";
        public Color color = Color.yellow;
        public Transform visual;
        [Header("Dialogo — edita cada frase en esta instancia")]
        public SpiritDialogueLine[] dialogo = { new SpiritDialogueLine() };
        [Min(.5f)] public float distanciaParaHablar = 2.8f;
        public static SpiritDialogue Active { get; private set; }
        public static bool HandledInputThisFrame => handledFrame == Time.frameCount;
        public int CurrentLine => line;
        public string CurrentText => lines != null && line < lines.Length ? lines[line] : "";
        static readonly List<SpiritDialogue> all = new List<SpiritDialogue>();
        static int handledFrame = -1;
        int line;
        string[] lines;
        float previousTimeScale;
        bool previousMovementLock;
        SoulPlayer talkingPlayer;
        Vector3 visualOrigin;
        GUIStyle titleStyle, bodyStyle;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { all.Clear(); Active = null; handledFrame = -1; }
        void OnEnable() { all.Add(this); Hud.Register(790, Draw); }
        void Start() { if (visual != null) visualOrigin = visual.localPosition; }
        void OnDisable() { if (Active == this) Close(); all.Remove(this); Hud.Unregister(Draw); }
        public static SpiritDialogue Nearest(SoulPlayer player)
        {
            if (player == null) return null;
            SpiritDialogue result = null; float best = float.MaxValue;
            foreach (var spirit in all)
            {
                if (spirit == null) continue;
                float distance = Vector3.Distance(player.transform.position, spirit.transform.position);
                if (distance <= spirit.distanciaParaHablar && distance < best) { best = distance; result = spirit; }
            }
            return result;
        }
        void Update()
        {
            if (visual != null) visual.localPosition = visualOrigin + Vector3.up * (Mathf.Sin(Time.time * 1.8f + transform.position.x) * .08f);
            var player = GameRoot.Instance != null ? GameRoot.Instance.Player : null;
            var keyboard = Keyboard.current;
            if (keyboard == null || !Application.isFocused || Keys.FreeCursorHeld || HandledInputThisFrame) return;
            if (Active == this)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) Close();
                else if (keyboard.rKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) Advance();
            }
            else if (Active == null && !Keys.Blocked && player != null && player.readInput && Nearest(player) == this && keyboard.rKey.wasPressedThisFrame) TryBegin(player);
        }
        public bool TryBegin(SoulPlayer player)
        {
            if (Active != null || Keys.Blocked || player == null || !isActiveAndEnabled || Vector3.Distance(player.transform.position, transform.position) > distanciaParaHablar) return false;
            var texts = new List<string>();
            if (dialogo != null) foreach (var item in dialogo) if (item != null && !string.IsNullOrWhiteSpace(item.texto)) texts.Add(item.texto);
            if (texts.Count == 0) { Hud.Toast("Este espíritu todavía no tiene diálogo."); return false; }
            lines = texts.ToArray(); line = 0; textScroll = Vector2.zero; Active = this; handledFrame = Time.frameCount;
            talkingPlayer = player; previousMovementLock = player.MovementLocked; previousTimeScale = Time.timeScale;
            Keys.Blocked = true; player.MovementLocked = true; Time.timeScale = 0;
            return true;
        }
        public void Advance()
        {
            if (Active != this) return;
            handledFrame = Time.frameCount;
            textScroll = Vector2.zero;
            if (line + 1 < lines.Length) line++; else Close();
        }
        public void Close()
        {
            if (Active != this) return;
            Active = null; handledFrame = Time.frameCount;
            Time.timeScale = previousTimeScale; Keys.Blocked = false;
            if (talkingPlayer != null) talkingPlayer.MovementLocked = previousMovementLock;
            talkingPlayer = null;
        }
        void Draw()
        {
            if (Active != this)
            {
                if (Active == null && !Keys.Blocked && GameRoot.Instance != null && Nearest(GameRoot.Instance.Player) == this)
                    Hud.WorldLabel(transform.position + Vector3.up * 1.8f, "R: hablar con " + nombre);
                return;
            }
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
            }
            float width = Mathf.Min(760, Screen.width - 32), height = Mathf.Min(250, Screen.height - 32);
            var rect = new Rect((Screen.width - width) / 2, Screen.height - height - 16, width, height);
            GUI.Box(rect, "");
            var previous = GUI.color; GUI.color = color;
            GUI.Label(new Rect(rect.x + 20, rect.y + 14, width - 40, 32), nombre, titleStyle); GUI.color = previous;
            GUI.Label(new Rect(rect.x + width - 90, rect.y + 20, 70, 24), (line + 1) + " / " + lines.Length);
            var area = new Rect(rect.x + 20, rect.y + 55, width - 40, height - 115);
            textScroll = GUI.BeginScrollView(area, textScroll, new Rect(0, 0, area.width - 20, Mathf.Max(area.height, bodyStyle.CalcHeight(new GUIContent(CurrentText), area.width - 20))));
            GUI.Label(new Rect(0, 0, area.width - 20, Mathf.Max(area.height, bodyStyle.CalcHeight(new GUIContent(CurrentText), area.width - 20))), CurrentText, bodyStyle);
            GUI.EndScrollView();
            GUI.Label(new Rect(rect.x + 20, rect.yMax - 40, width - 300, 24), "R / Enter: continuar · Esc: cerrar");
            if (GUI.Button(new Rect(rect.xMax - 250, rect.yMax - 45, 100, 30), "Cerrar")) Close();
            if (GUI.Button(new Rect(rect.xMax - 140, rect.yMax - 45, 120, 30), line + 1 < lines.Length ? "Continuar" : "Terminar")) Advance();
        }
        Vector2 textScroll;
    }
}
