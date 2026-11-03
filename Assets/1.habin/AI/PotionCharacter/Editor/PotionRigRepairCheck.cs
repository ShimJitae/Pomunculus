using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PotionRigRepairCheck
{
    const string ModelPath = "Assets/PotionCharacter/tripo_convert_7b219cfe-0b4e-4c79-bd43-81db06190188.fbx";
    const string ReportPath = "C:/Users/user1/Documents/Codex/2026-10-26/referenced-chatgpt-conversation-this-is-an/work/unity_coat_under_bag_check.txt";
    static PotionRigRepairCheck() { EditorApplication.delayCall += AutoRun; }
    static void AutoRun()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoRun; return; }
        if (!File.Exists(ReportPath) || !File.ReadAllText(ReportPath).Contains("RESULT PASS")) Run();
    }
    [MenuItem("Tools/Potion Character/Check Repaired Rig")]
    public static void Run()
    {
        var lines = new List<string>();
        var scene = EditorSceneManager.NewPreviewScene();
        GameObject instance = null;
        try
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (!asset) throw new Exception("Character asset not imported");
            instance = PrefabUtility.InstantiatePrefab(asset, scene) as GameObject;
            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var transforms = instance.GetComponentsInChildren<Transform>(true);
            Transform Bone(string suffix) => transforms.Single(t => t.name == "mixamorig:" + suffix || t.name == suffix);
            Vector3[] Bake(SkinnedMeshRenderer r)
            {
                var mesh = new Mesh(); r.BakeMesh(mesh);
                var vertices = mesh.vertices.Select(v => r.transform.TransformPoint(v)).ToArray();
                UnityEngine.Object.DestroyImmediate(mesh); return vertices;
            }
            float MaxDelta(Vector3[] a, Vector3[] b)
            {
                if (a.Length != b.Length) throw new Exception("Vertex count changed");
                float d = 0f; for (int i=0; i<a.Length; ++i) d = Mathf.Max(d, Vector3.Distance(a[i], b[i])); return d;
            }
            void Require(bool ok, string label) { lines.Add((ok ? "PASS " : "FAIL ") + label); if (!ok) throw new Exception(label); }
            Require(renderers.Length == 13, "13 skinned parts imported");
            int tris = renderers.Sum(r => r.sharedMesh.triangles.Length / 3);
            lines.Add("Triangles: " + tris);
            foreach (var r in renderers)
            {
                Require(r.sharedMaterials.All(m => m && m.shader && m.shader.isSupported), r.name + " materials valid");
                var mesh = r.sharedMesh;
                float min = 1f, max = 0f;
                foreach (var w in mesh.boneWeights)
                {
                    float sum=w.weight0+w.weight1+w.weight2+w.weight3;
                    min=Mathf.Min(min,sum); max=Mathf.Max(max,sum);
                }
                Require(min > .999f && max < 1.001f, r.name + " weights normalized");
            }
            var head=Bone("Head"); var neck=Bone("Neck");
            var headParts=renderers.Where(r => r.name.StartsWith("Head_")).ToArray();
            var headBefore=headParts.ToDictionary(r=>r, r=>Bake(r).Select(v=>head.InverseTransformPoint(v)).ToArray());
            var headRot=head.localRotation; var neckRot=neck.localRotation;
            neck.localRotation=neckRot*Quaternion.Euler(25f,0f,0f);
            head.localRotation=headRot*Quaternion.Euler(0f,45f,15f);
            foreach(var r in headParts)
            {
                var after=Bake(r).Select(v=>head.InverseTransformPoint(v)).ToArray();
                float d=MaxDelta(headBefore[r],after);
                Require(d<.0001f,r.name+" rigid head turn; deviation="+d.ToString("G5"));
            }
            neck.localRotation=neckRot; head.localRotation=headRot;
            var body=renderers.Where(r=>r.name == "tripo_part_0" || r.name == "tripo_part_6").ToArray();
            var bodyBefore=body.ToDictionary(r=>r,r=>Bake(r));
            var fingers=transforms.Where(t=> t.name.Contains("Hand") && !t.name.EndsWith("Hand")).ToArray();
            var rotations=fingers.ToDictionary(t=>t,t=>t.localRotation);
            foreach(var t in fingers) t.localRotation=t.localRotation*Quaternion.Euler(0f,0f,60f);
            foreach(var r in body) Require(MaxDelta(bodyBefore[r],Bake(r))<.0001f,r.name+" unaffected by fingers");
            foreach(var kv in rotations) kv.Key.localRotation=kv.Value;
            foreach(var pair in new[]{new[]{"tripo_part_3.","LeftLeg"},new[]{"tripo_part_4.","RightLeg"},new[]{"tripo_part_8.","RightLeg"},new[]{"tripo_part_9.","LeftLeg"},new[]{"tripo_part_6.","LeftUpLeg"}})
            {
                var r=renderers.Single(x=>x.name == pair[0].TrimEnd('.') || x.name.StartsWith(pair[0])); var b=Bone(pair[1]);
                var before=Bake(r); var rot=b.localRotation;
                b.localRotation=rot*Quaternion.Euler(40f,0f,0f);
                float d=MaxDelta(before,Bake(r)); b.localRotation=rot;
                Require(d<.0001f,r.name+" unaffected by "+pair[1]+"; deviation="+d.ToString("G5"));
            }
            var oldRotations=transforms.ToDictionary(t=>t,t=>t.localRotation);
            Bone("LeftUpLeg").localRotation*=Quaternion.Euler(30f,0f,0f);
            Bone("RightUpLeg").localRotation*=Quaternion.Euler(-30f,0f,0f);
            Bone("LeftLeg").localRotation*=Quaternion.Euler(-35f,0f,0f);
            Bone("RightArm").localRotation*=Quaternion.Euler(0f,0f,-50f);
            Bone("LeftArm").localRotation*=Quaternion.Euler(0f,0f,50f);
            foreach(var r in renderers)
            {
                var vertices=Bake(r);
                Require(vertices.All(v=> !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x)),r.name+" posed mesh finite");
            }
            foreach(var kv in oldRotations) kv.Key.localRotation=kv.Value;
            lines.Add("RESULT PASS - temporary preview instance only; user scene not modified.");
            Debug.Log("Potion rig repair checks passed.");
        }
        catch(Exception ex) { lines.Add("ERROR "+ex); Debug.LogException(ex); }
        finally
        {
            if(instance) UnityEngine.Object.DestroyImmediate(instance);
            EditorSceneManager.ClosePreviewScene(scene);
            File.WriteAllLines(ReportPath,lines);
        }
    }
}

