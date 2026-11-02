using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PotionJumpAnimationBuilder
{
    const string Folder="Assets/PotionCharacter/CustomAnimations";
    const string Model="Assets/PotionCharacter/tripo_convert_7b219cfe-0b4e-4c79-bd43-81db06190188.fbx";
    const string Output="C:/Users/user1/Documents/Codex/2026-10-26/referenced-chatgpt-conversation-this-is-an/work/custom_animation_preview";
    const float Duration=.96f;
    static SkinnedMeshRenderer[] floorRenderers;
    static Mesh floorMesh;
    static AnimationClip referenceClip;
    static GameObject referenceObject;
    static Dictionary<string,Transform> referenceBones;
    static Dictionary<string,Quaternion> referenceBind;
    static Dictionary<string,Quaternion> standingPose,airPose,crouchPose,takeoffPose;
    static Vector3 bindHip,standingHip,airHip,crouchHip,takeoffHip;

    static PotionJumpAnimationBuilder(){EditorApplication.delayCall+=Auto;}
    static void Auto()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
        if(!File.Exists(Output+"/jump_immediate_v1_done.txt"))Build();
    }
    static float Ease(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
    static void LoadReference(PreviewRenderUtility preview,List<string> log)
    {
        const string path="Assets/PotionCharacter/AnimationReferences/Jumping Up.fbx";
        referenceClip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).OrderByDescending(c=>c.length).First();
        referenceObject=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),preview.camera.gameObject.scene) as GameObject;
        foreach(var renderer in referenceObject.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
        foreach(var animator in referenceObject.GetComponentsInChildren<Animator>(true))animator.enabled=false;
        referenceBones=referenceObject.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("mixamorig:")).ToDictionary(t=>t.name,t=>t);
        var world=new Dictionary<string,Matrix4x4>();
        foreach(var renderer in referenceObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))for(int i=0;i<renderer.bones.Length;i++)if(renderer.bones[i])world[renderer.bones[i].name]=renderer.transform.localToWorldMatrix*renderer.sharedMesh.bindposes[i].inverse;
        referenceBind=new Dictionary<string,Quaternion>();
        foreach(var pair in referenceBones){var bone=pair.Value;Matrix4x4 parent=world.TryGetValue(bone.parent.name,out var parentWorld)?parentWorld:bone.parent.localToWorldMatrix;referenceBind[pair.Key]=(parent.inverse*world[pair.Key]).rotation;}
        bindHip=world["mixamorig:Hips"].GetColumn(3);
        Dictionary<string,Quaternion> Snapshot(float time){referenceClip.SampleAnimation(referenceObject,time);return referenceBones.ToDictionary(p=>p.Key,p=>p.Value.localRotation);}
        standingPose=Snapshot(0);standingHip=referenceBones["mixamorig:Hips"].position;
        takeoffPose=Snapshot(.5333333f);takeoffHip=referenceBones["mixamorig:Hips"].position;
        airPose=Snapshot(referenceClip.length);airHip=referenceBones["mixamorig:Hips"].position;
        crouchPose=Snapshot(referenceClip.length*.32f);crouchHip=referenceBones["mixamorig:Hips"].position;
        log.Add("Jumping Up.fbx duration="+referenceClip.length+"; source contains ascent only; landing/recovery added. Source hip rise="+(airHip.y-standingHip.y));
    }
    static void Pose(PotionCustomAnimationBuilder.Rig rig,float time)
    {
        rig.Reset();var hips=rig.B("Hips");var baseHip=hips.position;Vector3 offset;
        // Begin at standing and rise immediately; omit the reference anticipation squat.
        if(time<.05f)
        {
            float u=Ease(time/.05f);
            foreach(var bone in rig.bones)bone.localRotation=rig.rotations[bone]*Quaternion.Inverse(referenceBind[bone.name])*Quaternion.Slerp(standingPose[bone.name],takeoffPose[bone.name],u);
            offset=new Vector3(0,Mathf.Lerp(standingHip.y,takeoffHip.y,u)-bindHip.y,0);
        }
        else if(time<=.28f)
        {
            float u=Mathf.InverseLerp(.05f,.28f,time);referenceClip.SampleAnimation(referenceObject,Mathf.Lerp(.5333333f,referenceClip.length,u));
            foreach(var bone in rig.bones)bone.localRotation=rig.rotations[bone]*Quaternion.Inverse(referenceBind[bone.name])*referenceBones[bone.name].localRotation;
            var h=referenceBones["mixamorig:Hips"].position;var drift=Vector3.Lerp(takeoffHip,airHip,u);offset=new Vector3(h.x-drift.x,h.y-bindHip.y,h.z-drift.z);
        }
        else if(time<.54f)
        {
            float u=Mathf.InverseLerp(.28f,.54f,time),blend=Ease(u);
            foreach(var bone in rig.bones)bone.localRotation=rig.rotations[bone]*Quaternion.Inverse(referenceBind[bone.name])*Quaternion.Slerp(airPose[bone.name],standingPose[bone.name],blend);
            offset=new Vector3(0,Mathf.Lerp(airHip.y,standingHip.y,u*u)-bindHip.y+.025f*Mathf.Sin(Mathf.PI*u),0);
        }
        else
        {
            float crouch=time<.66f?.70f*Ease(Mathf.InverseLerp(.54f,.66f,time)):.70f*(1-Ease(Mathf.InverseLerp(.66f,Duration,time)));
            foreach(var bone in rig.bones)bone.localRotation=rig.rotations[bone]*Quaternion.Inverse(referenceBind[bone.name])*Quaternion.Slerp(standingPose[bone.name],crouchPose[bone.name],crouch);
            offset=new Vector3(0,Mathf.Lerp(standingHip.y,crouchHip.y,crouch)-bindHip.y,0);
        }
        hips.position=baseHip+offset;
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
        var clip=new AnimationClip{name="Potion_Jump",frameRate=rate};
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
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/Potion_Jump.anim");if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;EditorUtility.SetDirty(clip);}else AssetDatabase.CreateAsset(clip,Folder+"/Potion_Jump.anim");rig.Reset();return clip;
    }
    static void Connect(AnimationClip jump)
    {
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/PotionCharacter.controller");if(!controller.parameters.Any(p=>p.name=="Jump"))controller.AddParameter("Jump",AnimatorControllerParameterType.Trigger);
        var sm=controller.layers[0].stateMachine;var idle=sm.states.Single(s=>s.state.name=="Idle").state;var walk=sm.states.Single(s=>s.state.name=="Walk").state;var run=sm.states.Single(s=>s.state.name=="Run").state;
        var jumping=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Jump");if(!jumping)jumping=sm.AddState("Jump",new Vector3(700,220));jumping.motion=jump;
        foreach(var from in new[]{idle,walk,run})
        {
            var enter=from.transitions.FirstOrDefault(t=>t.destinationState==jumping);if(!enter)enter=from.AddTransition(jumping);foreach(var condition in enter.conditions)enter.RemoveCondition(condition);enter.AddCondition(AnimatorConditionMode.If,0,"Jump");enter.hasExitTime=false;enter.hasFixedDuration=true;enter.duration=.025f;from.transitions=from.transitions.OrderBy(t=>t.destinationState==jumping?0:1).ToArray();
        }
        AnimatorStateTransition Exit(AnimatorState state)
        {
            var t=jumping.transitions.FirstOrDefault(x=>x.destinationState==state);if(!t)t=jumping.AddTransition(state);foreach(var condition in t.conditions)t.RemoveCondition(condition);t.hasExitTime=true;t.exitTime=.97f;t.hasFixedDuration=true;t.duration=.10f;return t;
        }
        var exit=Exit(idle);exit.AddCondition(AnimatorConditionMode.IfNot,0,"Walking");
        exit=Exit(walk);exit.AddCondition(AnimatorConditionMode.If,0,"Walking");exit.AddCondition(AnimatorConditionMode.IfNot,0,"Running");
        exit=Exit(run);exit.AddCondition(AnimatorConditionMode.If,0,"Walking");exit.AddCondition(AnimatorConditionMode.If,0,"Running");EditorUtility.SetDirty(controller);
    }
    static void Verify(PotionCustomAnimationBuilder.Rig rig,AnimationClip jump,List<string> log)
    {
        rig.Reset();jump.SampleAnimation(rig.root,0);var start=rig.bones.ToDictionary(t=>t,t=>t.localRotation);var startHip=rig.B("Hips").localPosition;float startHeight=rig.B("Hips").position.y;float peak=0,lowest=float.PositiveInfinity,clearanceAtApex=0;
        for(int frame=0;frame<=252;frame++)
        {
            rig.Reset();float time=Duration*frame/252f;jump.SampleAnimation(rig.root,time);peak=Mathf.Max(peak,rig.B("Hips").position.y-startHeight);float current=float.PositiveInfinity;
            foreach(var bone in rig.bones)if(float.IsNaN(bone.position.x))throw new Exception("Invalid jump pose");
            foreach(var skin in floorRenderers){floorMesh.Clear();skin.BakeMesh(floorMesh);foreach(var v in floorMesh.vertices)current=Mathf.Min(current,skin.transform.TransformPoint(v).y);}
            lowest=Mathf.Min(lowest,current);clearanceAtApex=Mathf.Max(clearanceAtApex,current);
        }
        rig.Reset();jump.SampleAnimation(rig.root,.05f);float immediateRise=rig.B("Hips").position.y-startHeight;
        if(immediateRise<.01f)throw new Exception("Jump did not rise immediately: "+immediateRise);
        log.Add("PASS anticipation removed; pelvis rise at 0.05 seconds="+immediateRise);
        rig.Reset();jump.SampleAnimation(rig.root,Duration);float seam=rig.bones.Max(b=>Quaternion.Angle(start[b],b.localRotation));float hipError=Vector3.Distance(startHip,rig.B("Hips").localPosition);
        if(peak<.20f||clearanceAtApex<.18f||lowest<-.003f||seam>.05f||hipError>.001f)throw new Exception("Jump validation: peak="+peak+" clearance="+clearanceAtApex+" floor="+lowest+" end angle="+seam+" end hip="+hipError);
        log.Add("PASS jump height="+peak+"; full mesh apex clearance="+clearanceAtApex+"; minimum floor="+lowest+"; recovery pose="+seam+"; recovery hip="+hipError);rig.Reset();
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
                animator.SetTrigger("Jump");animator.Update(.01f);animator.Update(.09f);if(!animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"))throw new Exception("Jump failed from "+locomotion);
                for(int i=0;i<80;i++)animator.Update(.02f);if(!animator.GetCurrentAnimatorStateInfo(0).IsName(locomotion))throw new Exception("Jump recovery failed: "+locomotion);
                UnityEngine.Object.DestroyImmediate(go);go=null;log.Add("PASS "+locomotion+" > Jump > "+locomotion);
            }
        }
        finally{if(go)UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
    }
    [MenuItem("Tools/Potion Character/Build Jump Animation")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);var log=new List<string>();PreviewRenderUtility preview=null;
        try
        {
            preview=new PreviewRenderUtility();var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Model);var go=PrefabUtility.InstantiatePrefab(asset,preview.camera.gameObject.scene) as GameObject;foreach(var animator in go.GetComponentsInChildren<Animator>())animator.enabled=false;
            floorRenderers=go.GetComponentsInChildren<SkinnedMeshRenderer>();floorMesh=new Mesh();var rig=new PotionCustomAnimationBuilder.Rig(go);LoadReference(preview,log);var clip=Bake(rig);Connect(clip);PotionCustomAnimationBuilder.Render(preview,rig,clip,clip,log);Verify(rig,clip,log);VerifyController(asset,log);
            AssetDatabase.SaveAssets();log.Add("PASS Potion_Jump.anim saved and connected; standing jump preview with J.");Debug.Log("Potion jump animation ready.");
        }
        catch(Exception e){log.Add("ERROR "+e);Debug.LogException(e);}
        finally{if(preview!=null)preview.Cleanup();if(floorMesh)UnityEngine.Object.DestroyImmediate(floorMesh);floorMesh=null;floorRenderers=null;referenceObject=null;referenceClip=null;referenceBones=null;referenceBind=null;standingPose=null;airPose=null;crouchPose=null;File.WriteAllLines(Output+"/jump_immediate_v1_done.txt",log);}
    }
}

