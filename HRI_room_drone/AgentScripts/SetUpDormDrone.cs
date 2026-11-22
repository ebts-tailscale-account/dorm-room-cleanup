using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetUpDormDrone
{
    public static string Install()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/DormRoom.unity");
        if (!scene.isLoaded || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open DormRoom in Edit Mode before adding the drone controller.");
        var roots = scene.GetRootGameObjects();
        var drone = roots.Single(o => o.name == "Drone");
        if (drone.GetComponent<DormDroneController>())
            throw new InvalidOperationException("The drone already has its room controller.");
        var player = roots.Single(o => o.name == "Player").GetComponent<DormPlayerController>();
        var standing = roots.Single(o => o.name == "Player Start").transform.Find("Standing Start");
        var docking = roots.Single(o => o.name == "Furniture").transform.Find("Charging Station/Docking Point");
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/drone_sfx.mp3");
        var outline = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Scripts/TrashOutline.shader");
        var beam = Shader.Find("Universal Render Pipeline/Unlit");
        if (!player || !standing || !docking || !clip || !outline || !beam)
            throw new InvalidOperationException("The controller needs the player, spawn, charging pad, motor audio, and both shaders.");
        var rightDesk = roots.Single(o => o.name == "Furniture").transform.Find("Right Desk");
        Physics.SyncTransforms();
        if (!rightDesk.GetComponent<Collider>().Raycast(new Ray(new Vector3(0.58f, 2f, 2.50f), Vector3.down), out var deskTop, 2f))
            throw new InvalidOperationException("The desk surface for the smudge could not be found.");

        var smudgeMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Desk Smudge" };
        smudgeMaterial.SetColor("_BaseColor", new Color(0.20f, 0.14f, 0.10f));
        smudgeMaterial.SetFloat("_Smoothness", 0.1f);
        AssetDatabase.CreateAsset(smudgeMaterial, "Assets/Scenes/DormRoomMaterials/Desk Smudge.mat");
        var smudge = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        smudge.name = "Desk Smudge";
        smudge.tag = "Trash";
        smudge.transform.SetParent(roots.Single(o => o.name == "Trash").transform);
        smudge.transform.position = deskTop.point + Vector3.up * 0.0015f;
        smudge.transform.localScale = new Vector3(0.18f, 0.003f, 0.10f);
        smudge.GetComponent<Renderer>().sharedMaterial = smudgeMaterial;
        var dirt = new SerializedObject(smudge.AddComponent<CleanupTarget>());
        dirt.FindProperty("surfaceDirt").boolValue = true;
        dirt.ApplyModifiedProperties();
        Undo.RegisterCreatedObjectUndo(smudge, "Add desk smudge");

        var motor = Undo.AddComponent<AudioSource>(drone);
        motor.clip = clip;
        motor.playOnAwake = false;
        motor.loop = true;
        motor.spatialBlend = 1f;
        motor.volume = 0.45f;
        motor.minDistance = 1f;
        motor.maxDistance = 9f;

        var voice = Undo.AddComponent<AudioSource>(drone);
        voice.playOnAwake = false;
        voice.spatialBlend = 0f;
        var controller = new SerializedObject(Undo.AddComponent<DormDroneController>(drone));
        controller.FindProperty("player").objectReferenceValue = player;
        controller.FindProperty("standingStart").objectReferenceValue = standing;
        controller.FindProperty("dockingPoint").objectReferenceValue = docking;
        controller.FindProperty("motor").objectReferenceValue = motor;
        controller.FindProperty("voice").objectReferenceValue = voice;
        controller.FindProperty("outlineShader").objectReferenceValue = outline;
        controller.FindProperty("beamShader").objectReferenceValue = beam;
        controller.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Unity could not save the drone controller.");
        return "Saved the drone controller and audio sources in DormRoom.";
    }
}
