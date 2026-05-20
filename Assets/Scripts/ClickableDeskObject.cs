using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public sealed class ClickableDeskObject : MonoBehaviour, IPointerClickHandler
{
    public static event System.Action<ClickableDeskObject> Clicked;

    [SerializeField] private string objectId;
    [SerializeField] private string displayName;
    [SerializeField] private UnityEvent<string> onClicked = new UnityEvent<string>();

    private int lastClickFrame = -1;

    public string ObjectId => objectId;
    public string DisplayName => displayName;

    public void Initialize(string id, string readableName)
    {
        objectId = id;
        displayName = readableName;
        gameObject.name = readableName;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TriggerClick();
    }

    private void OnMouseDown()
    {
        TriggerClick();
    }

    private void TriggerClick()
    {
        if (lastClickFrame == Time.frameCount)
        {
            return;
        }

        lastClickFrame = Time.frameCount;
        Debug.Log($"Clicked desk object: {displayName} ({objectId})", this);
        onClicked.Invoke(objectId);
        Clicked?.Invoke(this);
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
        {
            return;
        }

        Gizmos.color = new Color(0.1f, 0.65f, 1f, 0.85f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
