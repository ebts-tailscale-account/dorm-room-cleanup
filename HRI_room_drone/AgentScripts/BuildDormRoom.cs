using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BuildDormRoom
{
    private const string ScenePath = "Assets/Scenes/DormRoom.unity";
    private const string FinishFolder = "Assets/Scenes/DormRoomMaterials";
    private const string Furniture = "Assets/Low Poly Furniture/Prefabs/";

    public static string Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) || AssetDatabase.IsValidFolder(FinishFolder))
            throw new InvalidOperationException("The dorm room scene or its materials already exist.");

        var bed = Load(Furniture + "Bed/Single Bed.prefab");
        var desk = Load(Furniture + "Table/Desk.prefab");
        var laptop = Load(Furniture + "Screen/Laptop.prefab");
        var chair = Load(Furniture + "Chair/Standart Chair.prefab");
        var wardrobe = Load(Furniture + "Wardrobe/Twin Wardrobe.prefab");
        var nightstand = Load(Furniture + "Nightstand/Short Nightstand.prefab");
        var plant = Load(Furniture + "Flower/Seedling.prefab");
        var rug = Load("Assets/LowPolyLivingRoomPack/Prefabs/Carpet_1.prefab");
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var skyShader = Shader.Find("Skybox/Procedural");
        if (!lit || !skyShader)
            throw new InvalidOperationException("The URP Lit or Procedural Skybox shader is unavailable.");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        AssetDatabase.CreateFolder("Assets/Scenes", "DormRoomMaterials");

        var floor = Finish("Oak Floor", lit, new Color(0.58f, 0.43f, 0.31f));
        var wall = Finish("Warm Plaster", lit, new Color(0.80f, 0.77f, 0.69f));
        var accent = Finish("Sage Plaster", lit, new Color(0.47f, 0.62f, 0.57f));
        var trim = Finish("Painted Trim", lit, new Color(0.90f, 0.87f, 0.79f));
        var fixture = Finish("Light Diffuser", lit, new Color(0.95f, 0.88f, 0.74f));
        fixture.EnableKeyword("_EMISSION");
        fixture.SetColor("_EmissionColor", new Color(1f, 0.83f, 0.63f) * 1.4f);

        var shell = new GameObject("Room Shell").transform;
        Box(shell, "Floor", new Vector3(0, -0.10f, 0), new Vector3(7.2f, 0.20f, 6.8f), floor);
        Box(shell, "Ceiling", new Vector3(0, 3.12f, 0), new Vector3(7.2f, 0.24f, 6.8f), trim);
        Box(shell, "Left Wall", new Vector3(-3.6f, 1.5f, 0), new Vector3(0.16f, 3, 6.8f), wall);
        Box(shell, "Right Wall", new Vector3(3.6f, 1.5f, 0), new Vector3(0.16f, 3, 6.8f), wall);
        Box(shell, "Entry Wall Left", new Vector3(-2.14f, 1.5f, -3.4f), new Vector3(2.92f, 3, 0.16f), wall);
        Box(shell, "Entry Wall Right", new Vector3(2.14f, 1.5f, -3.4f), new Vector3(2.92f, 3, 0.16f), wall);
        Box(shell, "Entry Header", new Vector3(0, 2.60f, -3.4f), new Vector3(1.36f, 0.80f, 0.16f), trim);

        var window = new GameObject("Window and Back Wall").transform;
        window.SetParent(shell);
        Box(window, "Left Wall Panel", new Vector3(-2.55f, 1.5f, 3.4f), new Vector3(2.10f, 3, 0.16f), accent);
        Box(window, "Right Wall Panel", new Vector3(2.55f, 1.5f, 3.4f), new Vector3(2.10f, 3, 0.16f), accent);
        Box(window, "Below Window", new Vector3(0, 0.57f, 3.4f), new Vector3(3, 1.14f, 0.16f), accent);
        Box(window, "Above Window", new Vector3(0, 2.81f, 3.4f), new Vector3(3, 0.38f, 0.16f), accent);
        Box(window, "Sill", new Vector3(0, 1.16f, 3.35f), new Vector3(3.16f, 0.10f, 0.34f), trim);
        Box(window, "Top Frame", new Vector3(0, 2.59f, 3.35f), new Vector3(3.08f, 0.08f, 0.22f), trim);
        foreach (float x in new[] { -1.46f, 0f, 1.46f })
            Box(window, "Vertical Frame", new Vector3(x, 1.88f, 3.35f), new Vector3(0.08f, 1.40f, 0.22f), trim);
        Box(shell, "Left Baseboard", new Vector3(-3.49f, 0.07f, 0), new Vector3(0.06f, 0.14f, 6.65f), trim);
        Box(shell, "Right Baseboard", new Vector3(3.49f, 0.07f, 0), new Vector3(0.06f, 0.14f, 6.65f), trim);

        var furniture = new GameObject("Furniture").transform;
        Place(furniture, rug, "Shared Rug", new Vector3(0, 0.006f, -0.34f), 2.40f);
        Place(furniture, wardrobe, "Shared Wardrobe", new Vector3(2.73f, 0, 2.63f), 1.93f, 180f);
        for (int side = -1; side <= 1; side += 2)
        {
            string label = side < 0 ? "Left" : "Right";
            Place(furniture, bed, label + " Bed", new Vector3(side * 2.73f, 0, 0.22f), 2.06f);
            var deskInstance = Place(furniture, desk, label + " Desk", new Vector3(side * 0.93f, 0, 2.80f), 1.45f, 180f);
            var aboveDesk = new Vector3(side * 0.93f, 2f, 2.60f);
            Physics.SyncTransforms();
            if (!deskInstance.GetComponent<Collider>().Raycast(new Ray(aboveDesk, Vector3.down), out var surface, 2f))
                throw new InvalidOperationException("The writing surface could not be located on " + label + " Desk.");
            Place(furniture, laptop, label + " Laptop", surface.point + Vector3.up * 0.006f, 0.38f, 180f);
            Place(furniture, chair, label + " Desk Chair", new Vector3(side * 0.93f, 0, 1.93f), 0.87f, 180f);
            var stand = Place(furniture, nightstand, label + " Nightstand", new Vector3(side * 2.85f, 0, 1.60f), 0.46f);
            float standTop = BoundsOf(stand.GetComponentsInChildren<Renderer>()).max.y;
            Place(furniture, plant, label + " Plant", new Vector3(side * 2.85f, standTop + 0.006f, 1.60f), 0.28f);
        }

        var sky = Finish("Daylight Sky", skyShader, Color.white);
        sky.SetColor("_SkyTint", new Color(0.32f, 0.54f, 0.78f));
        sky.SetFloat("_AtmosphereThickness", 1.1f);
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.58f);

        var lighting = new GameObject("Lighting").transform;
        Box(lighting, "Ceiling Light Fixture", new Vector3(0, 2.96f, 0.20f), new Vector3(1.05f, 0.10f, 0.52f), fixture);
        lighting.Find("Ceiling Light Fixture").GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var overhead = new GameObject("Main Overhead Light").AddComponent<Light>();
        overhead.transform.SetParent(lighting);
        overhead.transform.position = new Vector3(0, 2.83f, 0.20f);
        overhead.transform.rotation = Quaternion.Euler(90, 0, 0);
        overhead.type = LightType.Spot;
        overhead.spotAngle = 145f;
        overhead.innerSpotAngle = 100f;
        overhead.range = 8;
        overhead.intensity = 9f;
        overhead.color = new Color(1f, 0.89f, 0.75f);
        overhead.shadows = LightShadows.Soft;
        var sun = new GameObject("Window Daylight").AddComponent<Light>();
        sun.transform.SetParent(lighting);
        sun.transform.rotation = Quaternion.Euler(36, 196, 0);
        sun.type = LightType.Directional;
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;
        RenderSettings.sun = sun;

        var camera = new GameObject("Dorm Room Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.gameObject.AddComponent<AudioListener>();
        camera.transform.position = new Vector3(0, 2f, -3.23f);
        camera.transform.LookAt(new Vector3(0, 1.26f, 1.15f));
        camera.fieldOfView = 67;
        camera.nearClipPlane = 0.05f;
        camera.clearFlags = CameraClearFlags.Skybox;

        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Unity could not save the dorm room scene.");
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.AlignViewToObject(camera.transform);

        return ScenePath;
    }

    private static GameObject Load(string path)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(path)
            ?? throw new InvalidOperationException("Missing prefab: " + path);
    }

    private static Material Finish(string name, Shader shader, Color color)
    {
        var material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.24f);
        }
        AssetDatabase.CreateAsset(material, FinishFolder + "/" + name + ".mat");
        return material;
    }

    private static void Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent);
        box.transform.SetPositionAndRotation(position, Quaternion.identity);
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static GameObject Place(Transform parent, GameObject prefab, string name, Vector3 bottom, float size, float yaw = 0)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        var prop = instance.transform;
        prop.localRotation = Quaternion.Euler(0, yaw, 0);
        var renderers = prop.GetComponentsInChildren<Renderer>();
        var bounds = BoundsOf(renderers);
        prop.localScale *= size / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        bounds = BoundsOf(renderers);
        prop.position += bottom - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        return instance;
    }

    private static Bounds BoundsOf(Renderer[] renderers)
    {
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
