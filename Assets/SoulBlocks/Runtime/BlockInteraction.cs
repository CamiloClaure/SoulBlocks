using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public sealed class BlockInteraction : MonoBehaviour
{
    public Camera viewCamera;
    public float grabRange = 2.5f;
    public float holdDistance = 1.2f;
    public float holdHeight = .55f;
    public float placementRange = 1.8f;
    public bool readInput = true;
    public PickableBlock SelectedBlock { get; private set; }
    public PickableBlock HeldBlock { get; private set; }
    SoulBlocks.SoulPlayer soulPlayer;
    PathSlot[] slots;

    void Start()
    {
        soulPlayer = GetComponent<SoulBlocks.SoulPlayer>();
        if (viewCamera == null) viewCamera = Camera.main;
        slots = FindObjectsByType<PathSlot>();
    }

    void Update()
    {
        if (HeldBlock == null) UpdateSelection();
        else UpdateHeldPosition();
        if (!readInput) return;
        if (!Application.isFocused) { if (HeldBlock != null) CancelHold(); return; }
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.eKey.wasPressedThisFrame && HeldBlock == null) BeginHold();
        if (HeldBlock != null && keyboard.eKey.wasReleasedThisFrame) ReleaseHeldBlock();
    }

    void UpdateSelection()
    {
        PickableBlock nearest = null;
        float best = grabRange * grabRange;
        foreach (Collider nearby in Physics.OverlapSphere(transform.position, grabRange,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            PickableBlock block = nearby.GetComponent<PickableBlock>();
            if (block == null || block.IsPlaced || block.IsHeld) continue;
            float distance = (block.transform.position - transform.position).sqrMagnitude;
            if (distance >= best) continue;
            Vector3 eye = transform.position + Vector3.up * .65f;
            if (Physics.Linecast(eye, block.transform.position, out RaycastHit obstruction,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                obstruction.collider != block.SolidCollider) continue;
            nearest = block;
            best = distance;
        }
        if (SelectedBlock == nearest) return;
        if (SelectedBlock != null) SelectedBlock.SetSelected(false);
        SelectedBlock = nearest;
        if (SelectedBlock != null) SelectedBlock.SetSelected(true);
    }

    public bool BeginHold()
    {
        UpdateSelection();
        if (SelectedBlock == null || !SelectedBlock.BeginHold()) return false;
        HeldBlock = SelectedBlock;
        SelectedBlock = null;
        UpdateHeldPosition();
        return true;
    }

    public void UpdateHeldPosition()
    {
        if (HeldBlock == null) return;
        Vector3 forward = soulPlayer != null && soulPlayer.visual != null
            ? soulPlayer.visual.forward : transform.forward;
        forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
        HeldBlock.transform.position = transform.position + forward * holdDistance + Vector3.up * holdHeight;
    }

    public bool ReleaseHeldBlock()
    {
        if (HeldBlock == null) return false;
        UpdateHeldPosition();
        PathSlot nearestSlot = FindNearbySlot();
        PickableBlock block = HeldBlock;
        HeldBlock = null;
        if (nearestSlot != null)
        {
            if (nearestSlot.TryPlace(block))
            { return true; }
            block.ReturnToGround();
            return false;
        }
        if (TryFindDropPosition(block, out Vector3 position)) block.Drop(position);
        else block.ReturnToGround();
        return false;
    }

    PathSlot FindNearbySlot()
    {
        if (slots == null) slots = FindObjectsByType<PathSlot>();
        PathSlot nearest = null;
        float best = placementRange * placementRange;
        foreach (PathSlot slot in slots)
        {
            if (slot == null || slot.IsUnlocked) continue;
            float distance = Vector3.ProjectOnPlane(slot.transform.position - HeldBlock.transform.position,
                Vector3.up).sqrMagnitude;
            if (distance >= best || Vector3.Distance(transform.position, slot.transform.position) > grabRange + 1f) continue;
            best = distance;
            nearest = slot;
        }
        return nearest;
    }

    bool TryFindDropPosition(PickableBlock block, out Vector3 position)
    {
        position = Vector3.zero;
        Vector3 halfSize = Vector3.Scale(block.SolidCollider.size, block.transform.localScale) * .5f;
        Vector3 origin = block.transform.position + Vector3.up * 5;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f, 1 << 8,
            QueryTriggerInteraction.Ignore) || hit.normal.y < .95f) return false;
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 corner = origin + new Vector3(x * halfSize.x, 0, z * halfSize.z);
            if (!Physics.Raycast(corner, Vector3.down, out RaycastHit support, 10f, 1 << 8,
                QueryTriggerInteraction.Ignore) || Mathf.Abs(support.point.y - hit.point.y) > .08f) return false;
        }
        position = hit.point + Vector3.up * (halfSize.y + .02f);
        return !Physics.CheckBox(position, halfSize, block.transform.rotation, ~(1 << 8),
            QueryTriggerInteraction.Ignore);
    }

    public void CancelHold()
    {
        if (HeldBlock == null) return;
        HeldBlock.ReturnToGround();
        HeldBlock = null;
    }

    void OnDisable()
    {
        CancelHold();
        if (SelectedBlock != null) SelectedBlock.SetSelected(false);
        SelectedBlock = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, grabRange);
    }
}


