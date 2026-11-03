using UnityEngine;

public class TestInteract : MonoBehaviour, IInteractable
{
    MeshRenderer rend;
    Color originColor;

    private bool isActive;
    public bool IsActive => isActive;

    void Awake()
    {
        rend = GetComponent<MeshRenderer>();
        originColor = rend.material.color;
    }

    public void Activate()
    {
        rend.material.color = isActive ? originColor : Color.black;
        isActive = !isActive;
    }
}
