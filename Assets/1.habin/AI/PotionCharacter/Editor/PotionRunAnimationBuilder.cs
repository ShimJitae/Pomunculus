using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PotionRunAnimationBuilder
{
    const string Folder="Assets/PotionCharacter/CustomAnimations";
    const string Model="Assets/PotionCharacter/tripo_convert_7b219cfe-0b4e-4c79-bd43-81db06190188.fbx";
    const string Output="C:/Users/user1/Documents/Codex/2026-10-26/referenced-chatgpt-conversation-this-is-an/work/custom_animation_preview";
    static float Duration=.70f;
    static AnimationClip referenceClip;
    static GameObject referenceObject;
    static Dictionary<string,Transform> referenceBones;
    static Dictionary<string,Quaternion> referenceBind;
    static Vector3 referenceStart,referenceEnd,referenceBindHip;
    static float rawLoopError;
    static PotionRunAnimationBuilder(){EditorApplication.delayCall+=Auto;}
    static void Auto()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
        if(!File.Exists(Output+"/run_reference_v1_done.txt"))Build();
    }
    static void LoadReference(PreviewRenderUtility preview,List<string> log)
    {
        const string path="Assets/PotionCharacter/AnimationReferences/Running.fbx";
        referenceClip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).OrderByDescending(c=>c.length).First();Duration=referenceClip.length;
        referenceObject=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),preview.camera.gameObject.scene) as GameObject;
        foreach(var renderer in referenceObject.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
        foreach(var animator in referenceObject.GetComponentsInChildren<Animator>(true))animator.enabled=false;
        referenceBones=referenceObject.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("mixamorig:")).ToDictionary(t=>t.name,t=>t);
        var world=new Dictionary<string,Matrix4x4>();
        foreach(var renderer in referenceObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))for(int i=0;i<renderer.bones.Length;i++)if(renderer.bones[i])world[renderer.bones[i].name]=renderer.transform.localToWorldMatrix*renderer.sharedMesh.bindposes[i].inverse;
        referenceBind=new Dictionary<string,Quaternion>();
        foreach(var pair in referenceBones)
        {
            var bone=pair.Value;if(!world.ContainsKey(pair.Key))throw new Exception("Reference skin bind pose missing: "+pair.Key);
            Matrix4x4 parent=world.TryGetValue(bone.parent.name,out var parentWorld)?parentWorld:bone.parent.localToWorldMatrix;
            referenceBind[pair.Key]=(parent.inverse*world[pair.Key]).rotation;
        }
        referenceBindHip=world["mixamorig:Hips"].GetColumn(3);
        referenceClip.SampleAnimation(referenceObject,0);referenceStart=referenceBones["mixamorig:Hips"].position;
        referenceClip.SampleAnimation(referenceObject,Duration);referenceEnd=referenceBones["mixamorig:Hips"].position;
        log.Add("Reference Running.fbx duration="+Duration+" fps="+referenceClip.frameRate+" source travel="+(referenceEnd-referenceStart));
    }
    static void Pose(PotionCustomAnimationBuilder.Rig rig,float time)
    {
        rig.Reset();var hips=rig.B("Hips");var baseHip=hips.position;float u=Mathf.Clamp01(time/Duration);
        referenceClip.SampleAnimation(referenceObject,time);
        foreach(var bone in rig.bones)bone.localRotation=rig.rotations[bone]*Quaternion.Inverse(referenceBind[bone.name])*referenceBones[bone.name].localRotation;
        var source=referenceBones["mixamorig:Hips"].position;var drift=Vector3.Lerp(referenceStart,referenceEnd,u);
        hips.position=baseHip+new Vector3(source.x-drift.x,source.y-referenceBindHip.y-(referenceEnd.y-referenceStart.y)*u,source.z-drift.z);
    }
    static AnimationClip Bake(PotionCustomAnimationBuilder.Rig rig)
    {
        const int rate=120;int count=Mathf.RoundToInt(Duration*rate)+1;
        var rotation=rig.bones.ToDictionary(b=>b,b=>new List<Quaternion>());var hipPositions=new List<Vector3>();
        for(int i=0;i<count;i++)
        {
            Pose(rig,Duration*i/(count-1));hipPositions.Add(rig.B("Hips").localPosition);
            foreach(var b in rig.bones){var q=b.localRotation;var list=rotation[b];if(list.Count>0&&Quaternion.Dot(list.Last(),q)<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);list.Add(q);}
        }
        rawLoopError=0;
        foreach(var list in rotation.Values)
        {
            rawLoopError=Mathf.Max(rawLoopError,Quaternion.Angle(list[0],list.Last()));var correction=Quaternion.Inverse(list.Last())*list[0];
            for(int i=0;i<count;i++){float u=(float)i/(count-1);list[i]=list[i]*Quaternion.Slerp(Quaternion.identity,correction,u*u*(3-2*u));}
        }
        var clip=new AnimationClip{name="Potion_Run",frameRate=rate};
        void Curve(string path,string property,IEnumerable<float> values)
        {
            var keys=values.Select((v,i)=>new Keyframe(Duration*i/(count-1),v)).ToArray();var curve=new AnimationCurve(keys);
            for(int i=0;i<count;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),property),curve);
        }
        foreach(var bone in rig.bones)
        {
            string path=AnimationUtility.CalculateTransformPath(bone,rig.root.transform);var list=rotation[bone];bool moving=list.Any(q=>Quaternion.Angle(q,list[0])>.001f);
            for(int axis=0;axis<4;axis++)
            {
                int index=axis;string property=new[]{"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"}[axis];
                if(moving)Curve(path,property,list.Select(q=>q[index]));
                else AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),property),AnimationCurve.Constant(0,Duration,list[0][axis]));
            }
        }
        string hipsPath=AnimationUtility.CalculateTransformPath(rig.B("Hips"),rig.root.transform);
        for(int axis=0;axis<3;axis++){int index=axis;Curve(hipsPath,new[]{"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z"}[axis],hipPositions.Select(v=>v[index]));}
        clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.loopBlend=true;settings.keepOriginalPositionY=true;settings.keepOriginalPositionXZ=true;AnimationUtility.SetAnimationClipSettings(clip,settings);clip.wrapMode=WrapMode.Loop;
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/Potion_Run.anim");if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;EditorUtility.SetDirty(clip);}else AssetDatabase.CreateAsset(clip,Folder+"/Potion_Run.anim");rig.Reset();return clip;
    }
    static void Connect(AnimationClip run)
    {
        var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/PotionCharacter.controller");if(!c.parameters.Any(p=>p.name=="Running"))c.AddParameter("Running",AnimatorControllerParameterType.Bool);
        var sm=c.layers[0].stateMachine;var idle=sm.states.Single(s=>s.state.name=="Idle").state;var walk=sm.states.Single(s=>s.state.name=="Walk").state;var throwing=sm.states.Single(s=>s.state.name=="Throw").state;
        var running=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Run");if(!running)running=sm.AddState("Run",new Vector3(650,0));running.motion=run;
        AnimatorStateTransition Transition(AnimatorState from,AnimatorState to,bool exit=false)
        {
            var t=from.transitions.FirstOrDefault(p=>p.destinationState==to);if(!t)t=from.AddTransition(to);foreach(var condition in t.conditions)t.RemoveCondition(condition);t.hasExitTime=exit;t.exitTime=1;t.hasFixedDuration=true;t.duration=.12f;return t;
        }
        void If(AnimatorStateTransition t,string parameter,bool value)=>t.AddCondition(value?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,parameter);
        var a=Transition(idle,walk);If(a,"Walking",true);If(a,"Running",false);
        a=Transition(idle,running);If(a,"Walking",true);If(a,"Running",true);
        a=Transition(walk,running);If(a,"Walking",true);If(a,"Running",true);
        a=Transition(running,walk);If(a,"Walking",true);If(a,"Running",false);
        a=Transition(running,idle);If(a,"Walking",false);
        a=Transition(running,throwing);If(a,"Throw",true);
        a=Transition(throwing,walk,true);If(a,"Walking",true);If(a,"Running",false);
        a=Transition(throwing,running,true);If(a,"Walking",true);If(a,"Running",true);
        running.transitions=running.transitions.OrderBy(t=>t.destinationState==throwing?0:1).ToArray();EditorUtility.SetDirty(c);
    }
    static void Verify(PotionCustomAnimationBuilder.Rig rig,AnimationClip walk,List<string> log)
    {
        rig.Reset();walk.SampleAnimation(rig.root,0);var rotations=rig.bones.ToDictionary(b=>b,b=>b.localRotation);var hip=rig.B("Hips").localPosition;
        var right=new List<Vector3>();var left=new List<Vector3>();float sole=float.PositiveInfinity;
        var boots=rig.root.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.name=="tripo_part_3"||r.name=="tripo_part_4").ToArray();
        for(int j=0;j<=62;j++)
        {
            rig.Reset();walk.SampleAnimation(rig.root,Duration*j/62f);right.Add(rig.B("RightFoot").position);left.Add(rig.B("LeftFoot").position);
            foreach(var b in rig.bones)if(float.IsNaN(b.position.x))throw new Exception("Invalid reference walking pose");
            foreach(var boot in boots){var mesh=new Mesh();boot.BakeMesh(mesh);foreach(var v in mesh.vertices)sole=Mathf.Min(sole,boot.transform.TransformPoint(v).y);UnityEngine.Object.DestroyImmediate(mesh);}
        }
        walk.SampleAnimation(rig.root,Duration);float seam=rig.bones.Max(b=>Quaternion.Angle(rotations[b],b.localRotation));float hipSeam=Vector3.Distance(hip,rig.B("Hips").localPosition);
        float stride=Mathf.Min(right.Max(p=>p.z)-right.Min(p=>p.z),left.Max(p=>p.z)-left.Min(p=>p.z));float lift=Mathf.Min(right.Max(p=>p.y)-right.Min(p=>p.y),left.Max(p=>p.y)-left.Min(p=>p.y));
        if(seam>.05f||hipSeam>.0001f||stride<.06f||lift<.015f||sole<-.02f)throw new Exception("Reference walk validation: seam="+seam+" hip="+hipSeam+" stride="+stride+" lift="+lift+" sole="+sole);
        log.Add("PASS reference loop: raw rotation seam="+rawLoopError+"; corrected seam="+seam+"; hip seam="+hipSeam+"; foot stride="+stride+"; lift="+lift+"; lowest sole="+sole);rig.Reset();
    }
    static void VerifyController(GameObject asset,List<string> log)
    {
        var scene=EditorSceneManager.NewPreviewScene();GameObject go=null;
        try
        {
            go=PrefabUtility.InstantiatePrefab(asset,scene) as GameObject;var animator=go.GetComponent<Animator>();if(!animator)animator=go.AddComponent<Animator>();animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Folder+"/PotionCharacter.controller");animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);
            void Step(string state){animator.Update(.01f);animator.Update(.28f);if(!animator.GetCurrentAnimatorStateInfo(0).IsName(state))throw new Exception("Expected animation state "+state);}
            animator.SetBool("Walking",true);Step("Walk");animator.SetBool("Running",true);Step("Run");animator.SetTrigger("Throw");Step("Throw");for(int i=0;i<120;i++)animator.Update(.02f);if(!animator.GetCurrentAnimatorStateInfo(0).IsName("Run"))throw new Exception("Throw did not return to Run");
            animator.SetBool("Running",false);Step("Walk");animator.SetBool("Walking",false);Step("Idle");log.Add("PASS Idle > Walk > Run > Throw > Run > Walk > Idle.");
        }
        finally{if(go)UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
    }
    [MenuItem("Tools/Potion Character/Build Running Animation")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);var log=new List<string>();PreviewRenderUtility preview=null;
        try
        {
            preview=new PreviewRenderUtility();var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Model);var go=PrefabUtility.InstantiatePrefab(asset,preview.camera.gameObject.scene) as GameObject;foreach(var animator in go.GetComponentsInChildren<Animator>())animator.enabled=false;
            var rig=new PotionCustomAnimationBuilder.Rig(go);LoadReference(preview,log);var clip=Bake(rig);Connect(clip);Verify(rig,clip,log);VerifyController(asset,log);
            PotionCustomAnimationBuilder.Render(preview,rig,clip,clip,log);AssetDatabase.SaveAssets();log.Add("PASS Potion_Run.anim saved and connected; in-place run preview with Shift + W / Up Arrow.");Debug.Log("Potion running animation ready.");
        }
        catch(Exception e){log.Add("ERROR "+e);Debug.LogException(e);}
        finally{if(preview!=null)preview.Cleanup();referenceObject=null;referenceClip=null;referenceBones=null;referenceBind=null;File.WriteAllLines(Output+"/run_reference_v1_done.txt",log);}
    }
}
