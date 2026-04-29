using UnityEngine;

public class Swinging : MonoBehaviour
{
    [Header("Legacy Compatibility")]
    public Transform gunTip;

    private Grappling grappling;

    private void Awake()
    {
        grappling = GetComponent<Grappling>();
    }

    public void StopSwing()
    {
        if (grappling != null)
            grappling.StopSwing();
    }

    public bool IsSwinging()
    {
        return grappling != null && grappling.IsSwinging();
    }

    public Vector3 GetSwingPoint()
    {
        return grappling != null ? grappling.GetSwingPoint() : Vector3.zero;
    }
}
