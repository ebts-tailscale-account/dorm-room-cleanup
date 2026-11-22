using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PopulateDormRoom
{
    private const string ScenePath = "Assets/Scenes/DormRoom.unity";
    private const string MaterialFolder = "Assets/Scenes/DormRoomMaterials/";
    private const string DoorPath = "Assets/Free Wood Door Pack/Prefab/Wood/Door_1/Door_1_Brown.prefab";
    private const string Clutter = "Assets/Mess Maker Free/Prefabs/Low Poly/";
    private const string TrashTag = "Trash";

    public static string Inspect()
    {
        return string.Join("\n", Enumerable.Range(0, SceneManager.sceneCount).Select(i =>
        {
            var scene = SceneManager.GetSceneAt(i);
            var roots = scene.GetRootGameObjects();
            var droneObjects = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name.IndexOf("drone", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(t => $"{t.name} (parent {t.parent?.name}, position {t.position}, scale {t.localScale}, prefab {PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t)})");
            return $"{scene.path} dirty={scene.isDirty}; roots: {string.Join(", ", roots.Select(r => r.name))}; drone: {string.Join("; ", droneObjects)}";
        }));
    }

    public static string Verify()
    {
        var room = SceneManager.GetSceneByPath(ScenePath);
        var roots = room.GetRootGameObjects();
        var shell = roots.Single(o => o.name == "Room Shell").transform;
        var furniture = roots.Single(o => o.name == "Furniture").transform;
        var door = shell.Find("Room Door").gameObject;
        var pad = furniture.Find("Charging Station/Black Pad").gameObject;
        var drone = roots.Single(o => o.name == "Drone");
        var trash = roots.Single(o => o.name == "Trash").transform;
        var droneBounds = BoundsOf(drone);
        var padBounds = BoundsOf(pad);
        if (trash.childCount != 6 || trash.Cast<Transform>().Any(t => !t.CompareTag(TrashTag) || !t.GetComponentInChildren<Collider>()))
            throw new InvalidOperationException("Every trash item must have the Trash tag and a collider.");
        var doorBounds = BoundsOf(door);
        if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(door) != DoorPath ||
            Mathf.Abs(doorBounds.center.x) > 0.02f || Mathf.Abs(doorBounds.center.z + 3.39f) > 0.02f ||
            Mathf.Abs(doorBounds.min.y) > 0.02f || doorBounds.size.x < 1.36f || doorBounds.size.y < 2.20f)
            throw new InvalidOperationException("The door does not fill the entrance.");
        if (droneBounds.min.x < padBounds.min.x || droneBounds.max.x > padBounds.max.x ||
            droneBounds.min.z < padBounds.min.z || droneBounds.max.z > padBounds.max.z)
            throw new InvalidOperationException("The drone footprint exceeds the pad.");
        return "Verified door fit, drone footprint, and six tagged trash items with colliders.";
    }

    public static string Populate()
    {
        var room = SceneManager.GetSceneByPath(ScenePath);
        if (!room.isLoaded || room.isDirty)
            throw new InvalidOperationException("Open and save DormRoom before populating it.");
        var roots = room.GetRootGameObjects();
        var shell = roots.Single(o => o.name == "Room Shell").transform;
        var furniture = roots.Single(o => o.name == "Furniture").transform;
        if (shell.Find("Room Door") || furniture.Find("Charging Station") || roots.Any(o => o.name == "Trash" || o.name == "Drone"))
            throw new InvalidOperationException("The room already contains part of this update.");

        var source = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity").GetRootGameObjects().Single(o => o.name == "Drone");
        var dronePrefab = PrefabUtility.GetCorrespondingObjectFromSource(source);
        var doorPrefab = Load(DoorPath);
        var desk = furniture.Find("Left Desk").GetComponent<Collider>();
        var deskTop = Surface(desk, -1.34f, 2.60f);
        var clutter = new[]
        {
            ("Red Soda Can", Load(Clutter + "Cans/Soda Can Red.prefab"), new Vector3(-1.43f, 0f, -0.87f), 0.13f, 18f),
            ("Crushed Green Can", Load(Clutter + "Cans/Soda Can Green Crushed.prefab"), new Vector3(1.36f, 0f, -1.15f), 0.13f, 42f),
            ("Apple Core", Load(Clutter + "Food/Apple Core Red.prefab"), new Vector3(1.20f, 0f, 0.30f), 0.10f, 25f),
            ("Discarded Newspaper", Load("Assets/LowPolyLivingRoomPack/Prefabs/Newspaper.prefab"), new Vector3(-1.32f, 0f, 0.50f), 0.30f, 26f),
            ("Half-Eaten Burger", Load(Clutter + "Food/Half Eaten Cheeseburger.prefab"), new Vector3(-1.62f, 0f, 1.59f), 0.13f, 40f),
            ("Orange Soda Can", Load(Clutter + "Cans/Soda Can Orange.prefab"), Surface(furniture.Find("Right Desk").GetComponent<Collider>(), 1.34f, 2.60f), 0.13f, -30f),
        };
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (!dronePrefab || !lit || !unlit)
            throw new InvalidOperationException("The source drone or a URP shader is unavailable.");

        SceneManager.SetActiveScene(room);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Populate dorm room");
        var convertedMaterials = new Dictionary<Material, Material>();
        var door = Spawn(doorPrefab, "Room Door", shell, room);
        var doorBounds = BoundsOf(door);
        var doorScale = new Vector3(1.39f / doorBounds.size.x, 2.23f / doorBounds.size.y, 1f);
        door.transform.localScale = Vector3.Scale(door.transform.localScale, doorScale);
        AlignBottom(door, new Vector3(0f, 0f, -3.39f));
        UseUrp(door, lit, convertedMaterials);

        var station = new GameObject("Charging Station");
        station.transform.SetParent(furniture);
        station.transform.position = deskTop;
        Undo.RegisterCreatedObjectUndo(station, "Add charging station");
        var padMaterial = CreateMaterial("Charging Pad", lit, new Color(0.035f, 0.037f, 0.043f));
        var boltMaterial = CreateMaterial("Charging Symbol", unlit, new Color(0.65f, 1f, 0.30f));
        const float padWidth = 0.37f;
        const float padDepth = 0.27f;
        const float padHeight = 0.024f;
        Block(station.transform, "Black Pad", new Vector3(0, padHeight / 2f, 0), new Vector3(padWidth, padHeight, padDepth), padMaterial);
        const float boltHeight = padHeight + 0.001f;
        Bolt(station.transform, boltMaterial, new Vector3(0.026f, boltHeight, 0.052f), new Vector3(-0.032f, boltHeight, 0.003f));
        Bolt(station.transform, boltMaterial, new Vector3(-0.032f, boltHeight, 0.003f), new Vector3(0.025f, boltHeight, 0.003f));
        Bolt(station.transform, boltMaterial, new Vector3(0.025f, boltHeight, 0.003f), new Vector3(-0.026f, boltHeight, -0.052f));
        var dockingPoint = new GameObject("Docking Point").transform;
        dockingPoint.SetParent(station.transform, false);
        dockingPoint.localPosition = Vector3.up * padHeight;

        var drone = Spawn(dronePrefab, "Drone", null, room);
        var droneSize = BoundsOf(drone).size;
        drone.transform.localScale *= Mathf.Min((padWidth - 0.035f) / droneSize.x, (padDepth - 0.035f) / droneSize.z);
        AlignBottom(drone, dockingPoint.position + Vector3.up * 0.29f);
        foreach (var behaviour in drone.GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour) behaviour.enabled = false;
        foreach (var body in drone.GetComponentsInChildren<Rigidbody>(true))
            body.isKinematic = true;
        UseUrp(drone, lit, convertedMaterials);

        EnsureTrashTag();
        var trash = new GameObject("Trash");
        Undo.RegisterCreatedObjectUndo(trash, "Add trash group");
        foreach (var (name, prefab, bottom, size, yaw) in clutter)
        {
            var item = Spawn(prefab, name, trash.transform, room);
            item.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var bounds = BoundsOf(item).size;
            item.transform.localScale *= size / Mathf.Max(bounds.x, bounds.y, bounds.z);
            AlignBottom(item, bottom + Vector3.up * 0.002f);
            item.tag = TrashTag;
            if (!item.GetComponentInChildren<Collider>())
                item.AddComponent<BoxCollider>();
            foreach (var body in item.GetComponentsInChildren<Rigidbody>())
                body.isKinematic = true;
            UseUrp(item, lit, convertedMaterials);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(room);
        if (!EditorSceneManager.SaveScene(room))
            throw new InvalidOperationException("The updated room could not be saved.");
        Undo.CollapseUndoOperations(undoGroup);
        var finalSize = BoundsOf(drone).size;
        return $"Saved {room.path}; {trash.transform.childCount} {TrashTag} targets; drone footprint {finalSize.x:F2} x {finalSize.z:F2} m on a {padWidth:F2} x {padDepth:F2} m pad.";
    }

    private static Vector3 Surface(Collider collider, float x, float z)
    {
        Physics.SyncTransforms();
        if (!collider.Raycast(new Ray(new Vector3(x, 2f, z), Vector3.down), out var hit, 2f))
            throw new InvalidOperationException($"The tabletop was not found at ({x}, {z}).");
        return hit.point;
    }

    private static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path)
        ?? throw new InvalidOperationException("Missing prefab: " + path);

    private static GameObject Spawn(GameObject asset, string name, Transform parent, Scene scene)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(instance, "Add " + name);
        return instance;
    }

    private static Bounds BoundsOf(GameObject instance)
    {
        var renderers = instance.GetComponentsInChildren<MeshRenderer>();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    private static void AlignBottom(GameObject instance, Vector3 position)
    {
        var bounds = BoundsOf(instance);
        instance.transform.position += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    private static Material CreateMaterial(string name, Shader shader, Color color)
    {
        var material = new Material(shader) { name = name };
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(MaterialFolder + name + ".mat"));
        return material;
    }

    private static void UseUrp(GameObject instance, Shader shader, Dictionary<Material, Material> replacements)
    {
        foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                var source = materials[i];
                if (!source || source.GetTag("RenderPipeline", false) == "UniversalPipeline")
                    continue;
                if (!replacements.TryGetValue(source, out var material))
                {
                    var color = source.HasProperty("_Color") ? source.color : Color.white;
                    color.a = 1f;
                    material = CreateMaterial(instance.name + " - " + source.name, shader, color);
                    if (source.HasProperty("_MainTex"))
                        material.SetTexture("_BaseMap", source.mainTexture);
                    replacements.Add(source, material);
                }
                materials[i] = material;
                changed = true;
            }
            if (changed) renderer.sharedMaterials = materials;
        }
    }

    private static GameObject Block(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = position;
        block.transform.localScale = size;
        block.GetComponent<Renderer>().sharedMaterial = material;
        return block;
    }

    private static void Bolt(Transform parent, Material material, Vector3 start, Vector3 end)
    {
        var stroke = Block(parent, "Bolt Stroke", (start + end) / 2f, new Vector3(0.012f, 0.002f, (end - start).magnitude + 0.008f), material);
        stroke.transform.localRotation = Quaternion.LookRotation(end - start, Vector3.up);
        UnityEngine.Object.DestroyImmediate(stroke.GetComponent<Collider>());
    }

    private static void EnsureTrashTag()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == TrashTag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = TrashTag;
        tagManager.ApplyModifiedProperties();
    }
}
