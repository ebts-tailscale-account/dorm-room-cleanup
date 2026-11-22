using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public sealed class DormDroneController : MonoBehaviour
{
    [SerializeField] private DormPlayerController player;
    [SerializeField] private Transform standingStart;
    [SerializeField] private Transform dockingPoint;
    [SerializeField] private AudioSource motor;
    [SerializeField] private AudioSource voice;
    [SerializeField] private Shader outlineShader;
    [SerializeField] private Shader beamShader;
    [SerializeField] private float speed = 2.1f;
    [SerializeField] private float cruiseHeight = 2.35f;
    [SerializeField] private float interactionRange = 1.3f;

    [Serializable] private sealed class Line { public string id; public string text; }
    [Serializable] private sealed class Lines { public Line[] lines; }

    private readonly List<GameObject> remaining = new();
    private readonly Dictionary<string, string> dialogue = new();
    private Vector3 centerOffset;
    private float bottomOffset;
    private Material outlineMaterial;
    private Material beamMaterial;
    private LineRenderer beam;
    private DormDroneScreen screen;

    public GameObject MarkedTarget { get; private set; }
    public bool IsDocked { get; private set; }

    private void Awake()
    {
        player.SetWakeAllowed(false);
        var bounds = BoundsOf(gameObject);
        centerOffset = bounds.center - transform.position;
        bottomOffset = bounds.min.y - transform.position.y;

        var script = Resources.Load<TextAsset>("DroneLines");
        foreach (var line in JsonUtility.FromJson<Lines>(script.text).lines)
            dialogue.Add(line.id, line.text);

        outlineMaterial = new Material(outlineShader);
        beamMaterial = new Material(beamShader);
        beamMaterial.SetColor("_BaseColor", new Color(1f, 0.82f, 0.12f));
        beam = new GameObject("Target Beam").AddComponent<LineRenderer>();
        beam.transform.SetParent(transform, false);
        beam.sharedMaterial = beamMaterial;
        beam.positionCount = 2;
        beam.startWidth = 0.013f;
        beam.endWidth = 0.022f;
        beam.shadowCastingMode = ShadowCastingMode.Off;
        beam.enabled = false;
        screen = gameObject.AddComponent<DormDroneScreen>();
    }

    private IEnumerator Start()
    {
        foreach (var item in GameObject.FindGameObjectsWithTag("Trash"))
            if (item.scene == gameObject.scene) remaining.Add(item);

        yield return new WaitUntil(() => screen.ParticipantName != null);
        yield return FlyTo(standingStart.position + Vector3.up * 1.55f - centerOffset);
        float wakeTime = Speak("wake");
        player.SetWakeAllowed(true);
        float wakeLineEnd = Time.time + wakeTime;
        yield return new WaitUntil(() => player.IsAwake && Time.time >= wakeLineEnd);
        screen.ShowSubtitle(null);

        while (true)
        {
            remaining.RemoveAll(item => !item || !item.activeInHierarchy);
            if (remaining.Count == 0) break;

            var target = FindNearest();
            Vector3 focus = BoundsOf(target).center;
            yield return FlyTo(focus + Vector3.up * 0.92f - centerOffset);
            if (!target || !target.activeInHierarchy) continue;

            MarkedTarget = target;
            AddOutline(target);
            beam.SetPosition(0, transform.position + centerOffset - Vector3.up * 0.035f);
            beam.SetPosition(1, focus);
            beam.enabled = true;
            var settings = target.GetComponent<CleanupTarget>();
            string itemName = settings ? settings.DisplayName : target.name;
            bool dirt = settings && settings.SurfaceDirt;
            string id = (dirt ? "clean-" : "") + itemName.ToLowerInvariant().Replace(' ', '-');
            if (dialogue.ContainsKey(id))
                Speak(id);
            else
                Speak(dirt ? "surface" : "pickup", $"{itemName}. {dialogue[dirt ? "surface" : "pickup"]}");

            yield return new WaitUntil(() => !target || !target.activeInHierarchy || CanCollect(focus));
            beam.enabled = false;
            MarkedTarget = null;
            remaining.Remove(target);
            if (!target || !target.activeInHierarchy) continue;
            if (dirt)
                target.SetActive(false);
            else
                Destroy(target);

            if (remaining.Exists(item => item && item.activeInHierarchy))
            {
                yield return new WaitForSeconds(Speak("next"));
                screen.ShowSubtitle(null);
            }
        }

        yield return new WaitForSeconds(Speak("complete"));
        Vector3 dock = dockingPoint.position - new Vector3(centerOffset.x, bottomOffset, centerOffset.z);
        yield return FlyTo(dock);
        IsDocked = true;
        voice.Stop();
        player.enabled = false;
        screen.ShowCompletion();
    }

    private GameObject FindNearest()
    {
        // Measure from the drone's visual center so the order follows its flight path.
        GameObject nearest = null;
        float best = float.PositiveInfinity;
        foreach (var item in remaining)
        {
            float distance = (BoundsOf(item).center - (transform.position + centerOffset)).sqrMagnitude;
            if (distance >= best) continue;
            nearest = item;
            best = distance;
        }
        return nearest;
    }

    private bool CanCollect(Vector3 focus)
    {
        var keyboard = Keyboard.current;
        Vector3 playerCenter = player.transform.position + Vector3.up * 0.65f;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame &&
            (playerCenter - focus).sqrMagnitude <= interactionRange * interactionRange;
    }

    private IEnumerator FlyTo(Vector3 goal)
    {
        if ((transform.position - goal).sqrMagnitude < 0.0001f) yield break;
        motor.Play();
        // Cross the room above the furniture, then descend to the target or dock.
        yield return MoveTo(new Vector3(transform.position.x, cruiseHeight, transform.position.z));
        yield return MoveTo(new Vector3(goal.x, cruiseHeight, goal.z));
        yield return MoveTo(goal);
        motor.Stop();
    }

    private IEnumerator MoveTo(Vector3 position)
    {
        while (transform.position != position)
        {
            transform.position = Vector3.MoveTowards(transform.position, position, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = position;
    }

    private float Speak(string id, string subtitle = null)
    {
        screen.ShowSubtitle(subtitle ?? dialogue[id]);
        voice.clip = Resources.Load<AudioClip>("DroneVoice/" + id);
        if (!voice.clip) return 2f;
        voice.Play();
        return voice.clip.length;
    }

    private void AddOutline(GameObject target)
    {
        foreach (var original in target.GetComponentsInChildren<MeshFilter>())
        {
            if (!original.sharedMesh || !original.TryGetComponent<MeshRenderer>(out var source) || !source.enabled) continue;
            var shell = new GameObject("Yellow Outline");
            shell.transform.SetParent(original.transform, false);
            shell.AddComponent<MeshFilter>().sharedMesh = original.sharedMesh;
            var renderer = shell.AddComponent<MeshRenderer>();
            var materials = new Material[original.sharedMesh.subMeshCount];
            Array.Fill(materials, outlineMaterial);
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    private static Bounds BoundsOf(GameObject target)
    {
        var renderers = target.GetComponentsInChildren<MeshRenderer>();
        var bounds = new Bounds(target.transform.position, Vector3.zero);
        if (renderers.Length > 0) bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private void OnDisable()
    {
        if (motor) motor.Stop();
        if (voice) voice.Stop();
        if (beam) beam.enabled = false;
    }

    private void OnDestroy()
    {
        Destroy(outlineMaterial);
        Destroy(beamMaterial);
    }
}
