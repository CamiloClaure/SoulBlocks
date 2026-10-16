using UnityEngine;
namespace SoulBlocks
{
    public sealed class PickaxeVisual : MonoBehaviour
    {
        [Tooltip("Amarillo, azul, verde, celeste y rojo. Sustituye cada prefab por tu modelo FBX.")]
        public GameObject[] stagePrefabs = new GameObject[5];
        public Transform modelSocket;
        GameObject current;
        int stage = -1;
        public void ShowLevel(int level)
        {
            int next = MiningBalance.Stage(level);
            if (stage == next) return;
            if (current != null) { current.SetActive(false); Destroy(current); }
            stage = next;
            if (stagePrefabs != null && next < stagePrefabs.Length && stagePrefabs[next] != null)
                current = Instantiate(stagePrefabs[next], modelSocket != null ? modelSocket : transform, false);
        }
    }
}
