using UnityEngine;

public sealed class CleanupTarget : MonoBehaviour
{
    [SerializeField] private string displayName;
    [SerializeField] private bool surfaceDirt;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public bool SurfaceDirt => surfaceDirt;
}
