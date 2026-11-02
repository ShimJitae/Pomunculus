using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PotionRollAnimationBuilder
{
    const string Folder="Assets/PotionCharacter/CustomAnimations";
    const string Model="Assets/PotionCharacter/tripo_convert_7b219cfe-0b4e-4c79-bd43-81db06190188.fbx";
    const string Output="C:/Users/user1/Documents/Codex/2026-10-26/referenced-chatgpt-conversation-this-is-an/work/custom_animation_preview";
    const float Duration=1.10f;
    const float SourceStart=.60f;
    static float SourceEnd=2.133333f;
    static SkinnedMeshRenderer[] floorRenderers;
    static Mesh floorMesh;
    static AnimationClip referenceClip;
    static GameObject referenceObject;
    static Dictionary<string,Transform> referenceBones;
    static Dictionary<string,Quaternion> referenceBind;
    static Vector3 referenceStart,referenceEnd,referenceBindHip;

    static PotionRollAnimationBuilder(){EditorApplication.delayCall+=Auto;}
    static void Auto()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
        if(!File.Exists(Output+"/roll_reference_v2_done.txt"))Build();
    }
    static void LoadReference(PreviewRenderUtility preview,List<string> log)
    {
        const string path="Assets/PotionCharacter/AnimationReferences/Stand To Roll.fbx";
        referenceClip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).OrderByDescending(c=>c.length).First();SourceEnd=Mathf.Min(2.133333f,referenceClip.length);
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
        referenceClip.SampleAnimation(referenceObject,SourceStart);referenceStart=referenceBones["mixamorig:Hips"].position;
        referenceClip.SampleAnimation(referenceObject,SourceEnd);referenceEnd=referenceBones["mixamorig:Hips"].position;
        log.Add("Reference Stand To Roll.fbx duration="+Duration+" fps="+referenceClip.frameRate+" source travel="+(referenceEnd-referenceStart));
    }
    static void Pose(PotionCustomAnimationBuilder.Rig rig,float time)
    {
        rig.Reset();var hips=rig.B("Hips");var baseHip=hips.position;float u=Mathf.Clamp01(time/Duration);
        referenceClip.SampleAnimation(referenceObject,Mathf.Lerp(SourceStart,SourceEnd,u));
        foreach(var bone in rig.bones)bone.localRotation=rig.rotations[bone]*Quaternion.Inverse(referenceBind[bone.name])*referenceBones[bone.name].localRotation;
        var source=referenceBones["mixamorig:Hips"].position;var drift=Vector3.Lerp(referenceStart,referenceEnd,u);
        hips.position=baseHip+new Vector3(source.x-drift.x,source.y-referenceBindHip.y,source.z-drift.z);
        // The large bottle needs more floor clearance than a human head during the somersault.
        float lowest=float.PositiveInfinity;
        foreach(var skin in floorRenderers){floorMesh.Clear();skin.BakeMesh(floorMesh);foreach(var v in floorMesh.vertices)lowest=Mathf.Min(lowest,skin.transform.TransformPoint(v).y);}
        if(lowest<.002f)hips.position+=Vector3.up*(.002f-lowest);
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
        var clip=new AnimationClip{name="Potion_Roll",frameRate=rate};
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
        clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.loopBlend=false;settings.keepOriginalPositionY=true;settings.keepOriginalPositionXZ=true;AnimationUtility.SetAnimationClipSettings(clip,settings);clip.wrapMode=WrapMode.Once;
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/Potion_Roll.anim");if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;EditorUtility.SetDirty(clip);}else AssetDatabase.CreateAsset(clip,Folder+"/Potion_Roll.anim");rig.Reset();return clip;
    }
    static void Connect(AnimationClip roll)
    {
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/PotionCharacter.controller");if(!controller.parameters.Any(p=>p.name=="Roll"))controller.AddParameter("Roll",AnimatorControllerParameterType.Trigger);
        var sm=controller.layers[0].stateMachine;var idle=sm.states.Single(s=>s.state.name=="Idle").state;var walk=sm.states.Single(s=>s.state.name=="Walk").state;var run=sm.states.Single(s=>s.state.name=="Run").state;
        var rolling=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Roll");if(!rolling)rolling=sm.AddState("Roll",new Vector3(450,220));rolling.motion=roll;
        var enter=sm.anyStateTransitions.FirstOrDefault(t=>t.destinationState==rolling);if(!enter)enter=sm.AddAnyStateTransition(rolling);enter.hasExitTime=false;enter.hasFixedDuration=true;enter.duration=.045f;enter.canTransitionToSelf=false;
        foreach(var condition in enter.conditions)enter.RemoveCondition(condition);enter.AddCondition(AnimatorConditionMode.If,0,"Roll");
        AnimatorStateTransition Exit(AnimatorState state)
        {
            var t=rolling.transitions.FirstOrDefault(x=>x.destinationState==state);if(!t)t=rolling.AddTransition(state);foreach(var condition in t.conditions)t.RemoveCondition(condition);t.hasExitTime=true;t.exitTime=.97f;t.hasFixedDuration=true;t.duration=.10f;return t;
        }
        var transition=Exit(idle);transition.AddCondition(AnimatorConditionMode.IfNot,0,"Walking");
        transition=Exit(walk);transition.AddCondition(AnimatorConditionMode.If,0,"Walking");transition.AddCondition(AnimatorConditionMode.IfNot,0,"Running");
        transition=Exit(run);transition.AddCondition(AnimatorConditionMode.If,0,"Walking");transition.AddCondition(AnimatorConditionMode.If,0,"Running");EditorUtility.SetDirty(controller);
    }
    static void Verify(PotionCustomAnimationBuilder.Rig rig,AnimationClip roll,List<string> log)
    {
        rig.Reset();roll.SampleAnimation(rig.root,0);var start=rig.B("Hips").rotation;float turn=0,lowest=float.PositiveInfinity;
        for(int frame=0;frame<=264;frame++)
        {
            rig.Reset();roll.SampleAnimation(rig.root,Duration*frame/264f);turn=Mathf.Max(turn,Quaternion.Angle(start,rig.B("Hips").rotation));
            foreach(var bone in rig.bones)if(float.IsNaN(bone.position.x))throw new Exception("Invalid rolling pose");
            foreach(var skin in floorRenderers){floorMesh.Clear();skin.BakeMesh(floorMesh);foreach(var v in floorMesh.vertices)lowest=Mathf.Min(lowest,skin.transform.TransformPoint(v).y);}
        }
        rig.Reset();roll.SampleAnimation(rig.root,Duration);float head=rig.B("Head").position.y;
        if(turn<150f||lowest<-.003f||head<.5f)throw new Exception("Roll validation: turn="+turn+" floor="+lowest+" end head height="+head);
        log.Add("PASS preparation removed: source starts at "+SourceStart+" seconds; full roll turn="+turn+"; lowest mesh="+lowest+"; recovery head height="+head);rig.Reset();
    }
    static void VerifyController(GameObject asset,List<string> log)
    {
        var scene=EditorSceneManager.NewPreviewScene();GameObject go=null;
        try
        {
            foreach(string locomotion in new[]{"Idle","Walk","Run"})
            {
                go=PrefabUtility.InstantiatePrefab(asset,scene) as GameObject;var animator=go.GetComponent<Animator>();if(!animator)animator=go.AddComponent<Animator>();animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Folder+"/PotionCharacter.controller");animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);
                animator.SetBool("Walking",locomotion!="Idle");animator.SetBool("Running",locomotion=="Run");animator.Update(.01f);animator.Update(.25f);
                animator.SetTrigger("Roll");animator.Update(.01f);animator.Update(.06f);if(!animator.GetCurrentAnimatorStateInfo(0).IsName("Roll"))throw new Exception("Immediate roll failed from "+locomotion);
                for(int i=0;i<80;i++)animator.Update(.02f);if(!animator.GetCurrentAnimatorStateInfo(0).IsName(locomotion))throw new Exception("Roll return failed: "+locomotion);
                UnityEngine.Object.DestroyImmediate(go);go=null;log.Add("PASS "+locomotion+" > immediate Roll > "+locomotion);
            }
        }
        finally{if(go)UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
    }
    [MenuItem("Tools/Potion Character/Build Immediate Forward Roll")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);var log=new List<string>();PreviewRenderUtility preview=null;
        try
        {
            preview=new PreviewRenderUtility();var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Model);var go=PrefabUtility.InstantiatePrefab(asset,preview.camera.gameObject.scene) as GameObject;foreach(var animator in go.GetComponentsInChildren<Animator>())animator.enabled=false;
            floorRenderers=go.GetComponentsInChildren<SkinnedMeshRenderer>();floorMesh=new Mesh();var rig=new PotionCustomAnimationBuilder.Rig(go);LoadReference(preview,log);var clip=Bake(rig);Connect(clip);Verify(rig,clip,log);VerifyController(asset,log);
            PotionCustomAnimationBuilder.Render(preview,rig,clip,clip,log);AssetDatabase.SaveAssets();log.Add("PASS Potion_Roll.anim saved and connected; preparation-free forward roll preview with Space.");Debug.Log("Potion roll animation ready.");
        }
        catch(Exception e){log.Add("ERROR "+e);Debug.LogException(e);}
        finally{if(preview!=null)preview.Cleanup();if(floorMesh)UnityEngine.Object.DestroyImmediate(floorMesh);floorMesh=null;floorRenderers=null;referenceObject=null;referenceClip=null;referenceBones=null;referenceBind=null;File.WriteAllLines(Output+"/roll_reference_v2_done.txt",log);}
    }
}

