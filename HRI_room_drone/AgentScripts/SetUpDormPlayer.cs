using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetUpDormPlayer
{
    private const string RoomPath = "Assets/Scenes/DormRoom.unity";
    private const float PlayerHeight = 1.75f;
    private const float PlayerRadius = 0.28f;

    public static string Install()
    {
        var room = SceneManager.GetSceneByPath(RoomPath);
        if (!room.isLoaded || room.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("The dorm room must be saved and open in Edit Mode.");
        var roots = room.GetRootGameObjects();
        if (roots.Any(o => o.name == "Player"))
            throw new InvalidOperationException("DormRoom already has a player.");

        var bed = roots.Single(o => o.name == "Furniture").transform.Find("Left Bed");
        var bedRenderers = bed.GetComponentsInChildren<Renderer>();
        var bedBounds = bedRenderers[0].bounds;
        foreach (var renderer in bedRenderers)
            bedBounds.Encapsulate(renderer.bounds);
        var floor = roots.Single(o => o.name == "Room Shell").transform.Find("Floor").GetComponent<Collider>();
        var camera = roots.Single(o => o.name == "Dorm Room Camera").GetComponent<Camera>();
        var stand = new Vector3(bedBounds.max.x + 0.52f, floor.bounds.max.y + 0.02f, bedBounds.center.z);
        Physics.SyncTransforms();
        if (Physics.CheckCapsule(stand + Vector3.up * PlayerRadius, stand + Vector3.up * (PlayerHeight - PlayerRadius), PlayerRadius, ~0, QueryTriggerInteraction.Ignore))
            throw new InvalidOperationException($"The wake-up spot beside the bed is blocked at {stand}.");

        SceneManager.SetActiveScene(room);
        var start = new GameObject("Player Start").transform;
        var sleeping = new GameObject("Bed Start").transform;
        sleeping.SetParent(start);
        sleeping.position = new Vector3(bedBounds.center.x, bedBounds.min.y + bedBounds.size.y * 0.64f, bedBounds.center.z);
        var standing = new GameObject("Standing Start").transform;
        standing.SetParent(start);
        standing.SetPositionAndRotation(stand, Quaternion.LookRotation(new Vector3(1f, 0f, 0.5f)));
        sleeping.rotation = standing.rotation;
        Undo.RegisterCreatedObjectUndo(start.gameObject, "Create player spawn markers");

        var player = new GameObject("Player");
        player.transform.SetPositionAndRotation(stand, standing.rotation);
        Undo.RegisterCreatedObjectUndo(player, "Create dorm player");
        var character = Undo.AddComponent<CharacterController>(player);
        character.height = PlayerHeight;
        character.radius = PlayerRadius;
        character.center = Vector3.up * (PlayerHeight / 2f);
        character.stepOffset = 0.15f;
        character.minMoveDistance = 0f;
        Undo.SetTransformParent(camera.transform, player.transform, "Attach the room camera to the player");
        camera.transform.localPosition = Vector3.up * 1.62f;
        camera.transform.localRotation = Quaternion.identity;
        var controls = new SerializedObject(Undo.AddComponent<DormPlayerController>(player));
        controls.FindProperty("view").objectReferenceValue = camera;
        controls.FindProperty("bedStart").objectReferenceValue = sleeping;
        controls.FindProperty("standingStart").objectReferenceValue = standing;
        controls.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(room);
        if (!EditorSceneManager.SaveScene(room))
            throw new InvalidOperationException("Unity could not save the player in DormRoom.");
        return $"Player saved in {room.path}, with a standing start at {stand}.";
    }

    public static string OpenRoomForPlaytest()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save open scene changes before running a room-only playtest.");
        return EditorSceneManager.OpenScene(RoomPath, OpenSceneMode.Single).path;
    }
}
