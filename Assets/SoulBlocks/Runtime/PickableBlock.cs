using UnityEngine;

public enum BlockColor { Yellow, Blue, Red, Green }

[RequireComponent(typeof(BoxCollider), typeof(MeshRenderer))]
public sealed class PickableBlock : MonoBehaviour
{
    public BlockColor blockColor = BlockColor.Yellow;
    public bool IsPlaced { get; private set; }
    public bool IsHeld { get; private set; }
    public BoxCollider SolidCollider => GetComponent<BoxCollider>();

    Vector3 returnPosition;
    Quaternion returnRotation;
    MeshRenderer meshRenderer;
    MaterialPropertyBlock properties;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        properties = new MaterialPropertyBlock();
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (properties == null) properties = new MaterialPropertyBlock();
        Color color = ColorFor(blockColor);
        properties.SetColor("_BaseColor", selected ? Color.Lerp(color, Color.white, .45f) : color);
        properties.SetColor("_Color", selected ? Color.Lerp(color, Color.white, .45f) : color);
        properties.SetColor("_EmissionColor", color * (selected ? 1.8f : .3f));
        meshRenderer.SetPropertyBlock(properties);
    }

    public bool BeginHold()
    {
        if (IsPlaced || IsHeld) return false;
        returnPosition = transform.position;
        returnRotation = transform.rotation;
        IsHeld = true;
        SolidCollider.enabled = false;
        SetSelected(true);
        return true;
    }

    public void Drop(Vector3 position)
    {
        transform.position = position;
        IsHeld = false;
        SolidCollider.enabled = true;
        SetSelected(false);
        Physics.SyncTransforms();
    }

    public void ReturnToGround()
    {
        transform.rotation = returnRotation;
        Drop(returnPosition);
    }

    public bool Place(PathSlot slot)
    {
        if (IsPlaced || !slot.CanAccept(this)) return false;
        transform.SetPositionAndRotation(slot.transform.position, slot.transform.rotation);
        transform.localScale = slot.tileSize;
        SolidCollider.center = Vector3.zero;
        SolidCollider.size = Vector3.one;
        SolidCollider.isTrigger = false;
        gameObject.layer = slot.walkableLayer;
        IsPlaced = true;
        IsHeld = false;
        SolidCollider.enabled = true;
        SetSelected(false);
        Physics.SyncTransforms();
        return true;
    }

    public static Color ColorFor(BlockColor color)
    {
        switch (color)
        {
            case BlockColor.Blue: return new Color(.15f, .55f, 1f);
            case BlockColor.Red: return new Color(1f, .18f, .12f);
            case BlockColor.Green: return new Color(.25f, 1f, .35f);
            default: return new Color(1f, .75f, .06f);
        }
    }
}

