using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class PotionRigVisualCheck
{
    const string Folder = "C:/Users/user1/Documents/Codex/2026-10-26/referenced-chatgpt-conversation-this-is-an/work/pose_check";
    static PotionRigVisualCheck() { EditorApplication.delayCall += AutoRun; }
    static void AutoRun()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoRun; return; }
        if(!File.Exists(Folder+"/coat_direct_closed_done.txt")) Run();
    }
    [MenuItem("Tools/Potion Character/Render Pose Check")]
    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        var log=new List<string>();
        PreviewRenderUtility preview=null;
        try
        {
            preview=new PreviewRenderUtility();
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PotionCharacter/tripo_convert_7b219cfe-0b4e-4c79-bd43-81db06190188.fbx");
            var go=PrefabUtility.InstantiatePrefab(asset,preview.camera.gameObject.scene) as GameObject;
            var all=go.GetComponentsInChildren<Transform>(true);
            var baseline=all.ToDictionary(t=>t,t=>t.localRotation);
            Transform Bone(string suffix)=>all.Single(t=>t.name=="mixamorig:"+suffix || t.name==suffix);
            void Reset(){foreach(var kv in baseline)kv.Key.localRotation=kv.Value;}
            void Rotate(string suffix,Vector3 e){var t=Bone(suffix);t.localRotation=baseline[t]*Quaternion.Euler(e);}
            var renderers=go.GetComponentsInChildren<SkinnedMeshRenderer>();
            var bounds=new Bounds(); bool first=true;
            foreach(var r in renderers)
            {
                r.updateWhenOffscreen=true;
                var mesh=new Mesh();r.BakeMesh(mesh);
                foreach(var v in mesh.vertices){var w=r.transform.TransformPoint(v);if(first){bounds=new Bounds(w,Vector3.zero);first=false;}else bounds.Encapsulate(w);}
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            log.Add("Bounds "+bounds);
            foreach(var r in renderers) {
                var m=new Mesh();r.BakeMesh(m);
                var vs=m.vertices.Select(v=>r.transform.TransformPoint(v)).ToArray();
                var b=new Bounds(vs[0],Vector3.zero);foreach(var v in vs)b.Encapsulate(v);
                log.Add(r.name+" world bounds "+b);UnityEngine.Object.DestroyImmediate(m);
            }
            foreach(var t in all.Where(t=>t.name.Contains("mixamorig")))log.Add("BONE "+t.name+" "+t.position.ToString("F4"));
            preview.ambientColor=new Color(.5f,.5f,.5f);
            preview.lights[0].intensity=1.4f;
            preview.lights[0].transform.rotation=Quaternion.Euler(35f,35f,0f);
            preview.lights[1].intensity=.8f;
            preview.lights[1].transform.rotation=Quaternion.Euler(345f,215f,0f);
            var cam=preview.camera;
            cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color(.16f,.18f,.21f);
            cam.orthographic=true;cam.orthographicSize=bounds.size.y*.62f;
            cam.nearClipPlane=.01f;cam.farClipPlane=100f;
            var bakedFilters=new Dictionary<SkinnedMeshRenderer,MeshFilter>();
            foreach(var r in renderers)
            {
                var staticGo=new GameObject(r.name+"_BakedCheck");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(staticGo,preview.camera.gameObject.scene);
                staticGo.transform.SetParent(r.transform,false);
                var f=staticGo.AddComponent<MeshFilter>();
                staticGo.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;
                bakedFilters[r]=f;
                r.enabled=false;
            }
            void Capture(string label,Vector3 direction)
            {
                foreach(var r in renderers)
                {
                    var f=bakedFilters[r];if(f.sharedMesh)UnityEngine.Object.DestroyImmediate(f.sharedMesh);
                    var mesh=new Mesh();r.BakeMesh(mesh);f.sharedMesh=mesh;
                }
                cam.transform.position=bounds.center+direction.normalized*bounds.size.y*3f;
                cam.transform.LookAt(bounds.center);
                preview.BeginStaticPreview(new Rect(0,0,720,900));
                preview.Render(true);
                var image=preview.EndStaticPreview();
                File.WriteAllBytes(Folder+"/"+label+".png",image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
                log.Add("Rendered "+label);
            }
            Capture("01_rest_front",new Vector3(0,.12f,1)); Capture("11_bag_side",new Vector3(-1,.12f,.65f)); Capture("12_bag_other_side",new Vector3(1,.12f,.65f));
            Capture("02_rest_back",new Vector3(.2f,.12f,-1)); Capture("09_below_front",new Vector3(0,-.9f,1)); Capture("10_below_back",new Vector3(0,-.9f,-1));
            var bagRenderer=renderers.Single(r=>r.name=="tripo_part_6");
            bakedFilters[bagRenderer].gameObject.SetActive(false);
            Capture("13_coat_no_bag_front",new Vector3(0,.12f,1));
            Capture("14_coat_no_bag_back",new Vector3(.2f,.12f,-1));
            Capture("15_coat_no_bag_high_back",new Vector3(.5f,.65f,-1));
            Capture("16_coat_no_bag_side",new Vector3(-1,.12f,.65f));
            bakedFilters[bagRenderer].gameObject.SetActive(true);
            Reset();Rotate("Neck",new Vector3(15,0,0));Rotate("Head",new Vector3(0,45,10));
            Capture("03_head_turn",new Vector3(0,.12f,1));
            Reset();Rotate("LeftUpLeg",new Vector3(-28,0,0));Rotate("RightUpLeg",new Vector3(28,0,0));Rotate("LeftLeg",new Vector3(35,0,0));
            Rotate("RightArm",new Vector3(-18,0,-15));Rotate("LeftArm",new Vector3(18,0,15));
            Capture("04_stride_front",new Vector3(0,.12f,1));
            Capture("05_stride_side",new Vector3(-1,.12f,-.25f));
            Capture("06_stride_back",new Vector3(.2f,.12f,-1));
            Reset();Rotate("LeftUpLeg",new Vector3(28,0,0));Rotate("RightUpLeg",new Vector3(-28,0,0));Rotate("RightLeg",new Vector3(35,0,0));
            Capture("07_opposite_stride",new Vector3(0,.12f,1));
            Reset();Rotate("RightArm",new Vector3(-60,0,-45));Rotate("RightForeArm",new Vector3(0,0,-65));Rotate("Spine2",new Vector3(0,-15,0));
            Capture("08_throw_pose",new Vector3(0,.12f,1));
            Reset();
            log.Add("Completed on temporary preview model; active user scene untouched.");
        }
        catch(Exception e){log.Add("ERROR "+e);Debug.LogException(e);}
        finally{if(preview!=null)preview.Cleanup();File.WriteAllLines(Folder+"/coat_direct_closed_done.txt",log);}
    }
}






