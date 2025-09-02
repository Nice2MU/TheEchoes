using UnityEngine;
using UnityEditor;

public class ChangeSpriteMaterial : MonoBehaviour
{
    [MenuItem("Tools/ChangeSpriteMaterial → Sprite-Unlit-Default")]
    static void ChangeLitToUnlit()
    {
        Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
        Material litMat = null;
        Material unlitMat = null;

        foreach (var mat in allMaterials)
        {
            if (mat.name == "Sprite-Lit-Default")
                litMat = mat;

            else if (mat.name == "Sprite-Unlit-Default")
                unlitMat = mat;
        }

        if (litMat == null || unlitMat == null)
        {
            Debug.LogError("ไม่พบ Material ชื่อ Sprite-Lit-Default หรือ Sprite-Unlit-Default ในโปรเจกต์");
            return;
        }

        int count = 0;
        SpriteRenderer[] renderers = GameObject.FindObjectsOfType<SpriteRenderer>();

        foreach (var renderer in renderers)
        {
            if (renderer.sharedMaterial == litMat)
            {
                Undo.RecordObject(renderer, "Change to Sprite-Unlit-Default");
                renderer.sharedMaterial = unlitMat;
                EditorUtility.SetDirty(renderer);
                count++;
            }
        }

        Debug.Log($"✅ เปลี่ยน SpriteRenderer ทั้งหมด {count} ตัว จาก Sprite-Lit-Default → Sprite-Unlit-Default แล้วเรียบร้อย!");
    }
}