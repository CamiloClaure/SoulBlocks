using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(MeshRenderer))]
public sealed class PathSlot : MonoBehaviour
{
    public BlockColor requiredColor = BlockColor.Yellow;
    public PathSlot previousSlot;
    public Vector3 tileSize = new Vector3(2f, .25f, 2f);
    [Range(0, 31)] public int walkableLayer = 9;
    public bool IsUnlocked { get; private set; }

    void Awake()
    {
        // A closed tile can be detected, but must never provide physical support.
        GetComponent<BoxCollider>().isTrigger = true;
        gameObject.layer = 0;
    }

    public bool CanAccept(PickableBlock block)
    {
        return block != null && !block.IsPlaced && !IsUnlocked &&
            block.blockColor == requiredColor &&
            (previousSlot == null || previousSlot.IsUnlocked);
    }

    public bool TryPlace(PickableBlock block)
    {
        if (!CanAccept(block) || !block.Place(this)) return false;
        IsUnlocked = true;
        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<BoxCollider>().enabled = false;
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
        Physics.SyncTransforms();
        return true;
    }
}

