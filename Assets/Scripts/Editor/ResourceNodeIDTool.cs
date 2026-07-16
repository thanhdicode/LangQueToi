// ──────────────────────────────────────────────
// TheSprouty | Scripts/Editor/ResourceNodeIDTool.cs
// Editor utilities for The Sprouty.
// Menu: TheSprouty → Tools → ...
// ──────────────────────────────────────────────
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ResourceNodeIDTool
{
    [MenuItem("TheSprouty/Tools/Generate All Resource Node IDs")]
    private static void GenerateAllNodeIDs()
    {
        ResourceNode[] allNodes = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);

        if (allNodes.Length == 0)
        {
            Debug.Log("[NodeIDTool] No ResourceNodes found in scene.");
            return;
        }

        // ── Generate IDs ──────────────────────────────────────
        int updated   = 0;
        int unchanged = 0;

        foreach (ResourceNode node in allNodes)
        {
            SerializedObject   so   = new SerializedObject(node);
            SerializedProperty prop = so.FindProperty("nodeID");

            if (prop == null)
            {
                Debug.LogWarning($"[NodeIDTool] Could not find 'nodeID' property on '{node.name}'.");
                continue;
            }

            string newID = GetHierarchyPath(node.transform);

            if (prop.stringValue == newID)
            {
                unchanged++;
                continue;
            }

            so.Update();
            prop.stringValue = newID;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(node.gameObject);
            updated++;
        }

        // ── Check for duplicates ───────────────────────────────
        HashSet<string> seen = new HashSet<string>();
        int duplicates = 0;

        foreach (ResourceNode node in allNodes)
        {
            string id = node.NodeID;
            if (string.IsNullOrEmpty(id)) continue;

            if (!seen.Add(id))
            {
                Debug.LogWarning($"[NodeIDTool] Duplicate NodeID: \"{id}\" — rename the GameObject to make it unique.", node.gameObject);
                duplicates++;
            }
        }

        // ── Summary ───────────────────────────────────────────
        if (duplicates > 0)
            Debug.LogWarning($"[NodeIDTool] Done — {updated} updated, {unchanged} unchanged. " +
                             $"⚠ {duplicates} duplicate(s) found — fix them before saving!");
        else
            Debug.Log($"[NodeIDTool] Done — {updated} updated, {unchanged} unchanged. No duplicates ✓");
    }

    private static string GetHierarchyPath(Transform t)
    {
        // Use world position to make duplicate-named objects unique.
        // ResourceNodes are map-placed and never move, so position is a stable key.
        return $"{t.name}@({t.position.x:F1},{t.position.y:F1})";
    }

    // ──────────────────────────────────────────────────────────
    // Save file utilities
    // ──────────────────────────────────────────────────────────

    // ──────────────────────────────────────────────────────────
    // NPC ID generation
    // ──────────────────────────────────────────────────────────

    [MenuItem("TheSprouty/Tools/Generate All NPC IDs")]
    private static void GenerateAllNPCIDs()
    {
        BaseAnimalNPC[] allNPCs = Object.FindObjectsByType<BaseAnimalNPC>(FindObjectsSortMode.None);

        if (allNPCs.Length == 0)
        {
            Debug.Log("[NPCIDTool] No BaseAnimalNPC found in scene.");
            return;
        }

        int updated = 0, unchanged = 0;

        foreach (BaseAnimalNPC npc in allNPCs)
        {
            SerializedObject   so   = new SerializedObject(npc);
            SerializedProperty prop = so.FindProperty("npcID");

            if (prop == null)
            {
                Debug.LogWarning($"[NPCIDTool] Could not find 'npcID' on '{npc.name}'.");
                continue;
            }

            string newID = GetHierarchyPath(npc.transform);
            if (prop.stringValue == newID) { unchanged++; continue; }

            so.Update();
            prop.stringValue = newID;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(npc.gameObject);
            updated++;
        }

        // Duplicate check
        HashSet<string> seen = new HashSet<string>();
        int duplicates = 0;
        foreach (BaseAnimalNPC npc in allNPCs)
        {
            if (string.IsNullOrEmpty(npc.NPCID)) continue;
            if (!seen.Add(npc.NPCID))
            {
                Debug.LogWarning($"[NPCIDTool] Duplicate NPCID: \"{npc.NPCID}\"", npc.gameObject);
                duplicates++;
            }
        }

        if (duplicates > 0)
            Debug.LogWarning($"[NPCIDTool] Done — {updated} updated, {unchanged} unchanged. ⚠ {duplicates} duplicate(s)!");
        else
            Debug.Log($"[NPCIDTool] Done — {updated} updated, {unchanged} unchanged. No duplicates ✓");
    }

    [MenuItem("TheSprouty/Tools/Generate All IDs (Nodes + NPCs)")]
    private static void GenerateAllIDs()
    {
        GenerateAllNodeIDs();
        GenerateAllNPCIDs();
    }

    [MenuItem("TheSprouty/Tools/Delete Save File (Reset to Day 1)")]
    private static void DeleteSaveFile()
    {
        string path = Path.Combine(Application.persistentDataPath, "sprouty_save.json");

        if (!File.Exists(path))
        {
            Debug.Log($"[SaveTool] No save file found at:\n{path}");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Delete Save File",
            $"This will delete:\n{path}\n\nGame will start fresh from Day 1 next Play.",
            "Delete", "Cancel");

        if (!confirm) return;

        File.Delete(path);
        Debug.Log($"[SaveTool] Save file deleted. Next Play will start from Day 1.");
    }

    [MenuItem("TheSprouty/Tools/Show Save File Path")]
    private static void ShowSaveFilePath()
    {
        string path = Path.Combine(Application.persistentDataPath, "sprouty_save.json");
        bool   exists = File.Exists(path);
        Debug.Log($"[SaveTool] Save file path:\n{path}\nExists: {exists}");
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
