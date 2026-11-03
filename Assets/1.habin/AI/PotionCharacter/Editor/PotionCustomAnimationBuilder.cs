using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PotionCustomAnimationBuilder
{
    const string Model="Assets/PotionCharacter/tripo_convert_7b219cfe-0b4e-4c79-bd43-81db06190188.fbx";
    const string Folder="Assets/PotionCharacter/CustomAnimations";
    const string Output="C:/Users/user1/Documents/Codex/2026-10-26/referenced-chatgpt-conversation-this-is-an/work/custom_animation_preview";
    const string Version="v4g_reference_arms";
    static PotionCustomAnimationBuilder(){EditorApplication.delayCall+=Auto;}
    static void Auto(){if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Auto;return;} if(!File.Exists(Output+"/"+Version+"_done.txt"))Build();}
    static AnimationClip referenceClip;
    static GameObject referenceObject;
    static Dictionary<string,Transform> referenceBones;
    static Dictionary<string,Quaternion> referenceBindLocal;
    public sealed class Rig
    {
        public GameObject root; public Transform[] bones; public Dictionary<Transform,Quaternion> rotations; public Dictionary<Transform,Vector3> positions;
        public Rig(GameObject go){root=go;bones=go.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("mixamorig:")).ToArray();rotations=bones.ToDictionary(t=>t,t=>t.localRotation);positions=bones.ToDictionary(t=>t,t=>t.localPosition);}
        public Transform B(string n)=>bones.Single(t=>t.name=="mixamorig:"+n);
        public void Reset(){foreach(var t in bones){t.localRotation=rotations[t];t.localPosition=positions[t];}}
        public void Turn(string n,Vector3 e){var t=B(n);t.localRotation=rotations[t]*Quaternion.Euler(e);}
        public void LegIK(string side,Vector3 target,Quaternion footRotation)
        {
            var a=B(side+"UpLeg");var b=B(side+"Leg");var c=B(side+"Foot");var origin=a.position;
            float l1=Vector3.Distance(a.position,b.position),l2=Vector3.Distance(b.position,c.position);var d=target-origin;float len=Mathf.Clamp(d.magnitude,Mathf.Abs(l1-l2)+.0001f,l1+l2-.0001f);d.Normalize();
            var pole=origin+new Vector3(side=="Right"?.05f:-.05f,-.1f,.35f);var bend=Vector3.ProjectOnPlane(pole-origin,d).normalized;
            float along=(l1*l1-l2*l2+len*len)/(2*len);float height=Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));var knee=origin+d*along+bend*height;
            a.rotation=Quaternion.FromToRotation(b.position-origin,knee-origin)*a.rotation;
            b.rotation=Quaternion.FromToRotation(c.position-b.position,origin+d*len-b.position)*b.rotation;c.rotation=footRotation;
        }
        public void IK(string side,Vector3 target,Vector3 pole)
        {
            var a=B(side+"Arm");var b=B(side+"ForeArm");var c=B(side+"Hand");var origin=a.position;
            float l1=Vector3.Distance(a.position,b.position),l2=Vector3.Distance(b.position,c.position);var d=target-origin;float len=Mathf.Clamp(d.magnitude,Mathf.Abs(l1-l2)+.001f,l1+l2-.001f);d.Normalize();
            var bend=Vector3.ProjectOnPlane(pole-origin,d).normalized;if(bend.sqrMagnitude<.1f)bend=Vector3.Cross(d,Vector3.up).normalized;
            float along=(l1*l1-l2*l2+len*len)/(2*len);float height=Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));var elbow=origin+d*along+bend*height;
            a.rotation=Quaternion.FromToRotation(b.position-origin,elbow-origin)*a.rotation;
            b.rotation=Quaternion.FromToRotation(c.position-b.position,origin+d*len-b.position)*b.rotation;
            // Aim the wrist through the middle metacarpal, preserving forearm roll.
            var middle=B(side+"HandMiddle1");var palm=middle.position-c.position;var forward=c.position-b.position;
            if(palm.sqrMagnitude>.000001f && forward.sqrMagnitude>.000001f)c.rotation=Quaternion.FromToRotation(palm,forward)*c.rotation;
        }
    }
    static readonly float[] Times={0,.18f,.36f,.49f,.59f,.78f,1.3f};
    static readonly Vector3[] Hands={Vector3.zero,new Vector3(.20f,.49f,.055f),new Vector3(.23f,.64f,-.115f),new Vector3(.17f,.63f,.10f),new Vector3(.11f,.55f,.26f),new Vector3(.105f,.435f,.16f),Vector3.zero};
    static float Smooth(float x)=>x*x*(3-2*x);
    static Vector3 Track(float t,Vector3 rest)
    {
        for(int i=0;i<Times.Length-1;i++)if(t<=Times[i+1]){float f=Smooth(Mathf.InverseLerp(Times[i],Times[i+1],t));return Vector3.Lerp(i==0?rest:Hands[i],i+1==Hands.Length-1?rest:Hands[i+1],f);}return rest;
    }
    static float TrackAngle(float t,float[] values)
    {
        for(int i=0;i<Times.Length-1;i++)if(t<=Times[i+1])return Mathf.Lerp(values[i],values[i+1],Smooth(Mathf.InverseLerp(Times[i],Times[i+1],t)));return values.Last();
    }
    static void Pose(Rig r,bool throwing,float t)
    {
        if(throwing)t*=1.3f/1.6f;
        r.Reset();var restRight=r.B("RightHand").position;var restLeft=r.B("LeftHand").position;
        if(!throwing)
        {
            float wave=Mathf.Sin(t/3f*Mathf.PI*2);float breath=(1-Mathf.Cos(t/3f*Mathf.PI*2))*.5f;
            r.Turn("Spine",new Vector3(-.8f*breath,.5f*wave,.35f*wave));r.Turn("Spine1",new Vector3(-.7f*breath,0,0));r.Turn("Spine2",new Vector3(.25f*breath,-.4f*wave,0));
            r.Turn("Neck",new Vector3(.6f*breath,.7f*wave,0));r.Turn("Head",new Vector3(-.3f*breath,-.4f*wave,0));
            r.IK("Right",restRight+new Vector3(.002f*wave,.003f*breath,.001f*wave),new Vector3(.4f,.47f,-.12f));
            r.IK("Left",restLeft+new Vector3(-.002f*wave,.003f*breath,-.001f*wave),new Vector3(-.4f,.47f,-.12f));
        }
        else
        {
            var rightFoot=r.B("RightFoot");var leftFoot=r.B("LeftFoot");var rf=rightFoot.position;var lf=leftFoot.position;var rr=rightFoot.rotation;var lr=leftFoot.rotation;
            float shiftX=TrackAngle(t,new float[]{0,.006f,.014f,-.004f,-.017f,-.009f,0});
            float shiftY=TrackAngle(t,new float[]{0,-.012f,-.028f,-.012f,-.018f,-.010f,0});
            float shiftZ=TrackAngle(t,new float[]{0,-.008f,-.022f,.008f,.032f,.018f,0});
            float hipTurn=TrackAngle(t,new float[]{0,-5,-15,0,17,9,0});
            float turn=TrackAngle(t,new float[]{0,-9,-27,2,25,13,0});
            float lean=TrackAngle(t,new float[]{0,-3,-10,4,18,10,0});
            float roll=TrackAngle(t,new float[]{0,-2,-6,0,5,3,0});
            var hips=r.B("Hips");hips.position+=new Vector3(shiftX,shiftY,shiftZ);r.Turn("Hips",new Vector3(0,hipTurn,roll*.25f));
            r.Turn("Spine",new Vector3(lean*.35f,turn*.25f,roll*.3f));r.Turn("Spine1",new Vector3(lean*.3f,turn*.35f,roll*.3f));r.Turn("Spine2",new Vector3(lean*.35f,turn*.4f,roll*.15f));
            r.Turn("Neck",new Vector3(-lean*.4f,-(turn+hipTurn)*.35f,-roll*.2f));r.Turn("Head",new Vector3(-lean*.2f,-(turn+hipTurn)*.3f,0));
            r.LegIK("Right",rf,rr);r.LegIK("Left",lf,lr);
            var hand=Track(t,restRight);r.IK("Right",hand,new Vector3(.48f,.52f,-.18f));
            var left=restLeft+new Vector3(TrackAngle(t,new float[]{0,-.035f,-.065f,-.045f,-.05f,-.02f,0}),TrackAngle(t,new float[]{0,.055f,.12f,.065f,.045f,.025f,0}),TrackAngle(t,new float[]{0,.035f,.075f,-.015f,-.105f,-.06f,0}));
            r.IK("Left",left,new Vector3(-.43f,.43f,-.16f));
            if(referenceClip!=null && referenceObject!=null)
            {
                // Source times isolate the throw after the pickup; keys retain its backswing, release and follow-through.
                float referenceTime=TrackAngle(t,new float[]{1.70f,1.93f,2.28f,2.48f,2.72f,3.30f,4.70f});
                referenceClip.SampleAnimation(referenceObject,Mathf.Min(referenceTime,referenceClip.length));
                float blend=TrackAngle(t,new float[]{0,.85f,1,1,1,1,0});
                foreach(var bone in r.bones)
                {
                    string name=bone.name.Substring("mixamorig:".Length);
                    bool arm=(name.StartsWith("Right")||name.StartsWith("Left"))&&(name.Contains("Shoulder")||name.EndsWith("Arm")||name.Contains("Hand"));
                    bool torso=name=="Hips"||name=="Spine"||name=="Spine1"||name=="Spine2"||name=="Neck"||name=="Head";
                    if((arm||torso) && referenceBones.TryGetValue(bone.name,out var source))bone.localRotation=Quaternion.Slerp(bone.localRotation,r.rotations[bone]*Quaternion.Inverse(referenceBindLocal[bone.name])*source.localRotation,blend);
                }
                // Keep both feet reachable when the reference pelvis turns above a stationary stance.
                for(int pass=0;pass<4;pass++)
                {
                    float excess=0;
                    foreach(string side in new[]{"Right","Left"}){var upper=r.B(side+"UpLeg");var knee=r.B(side+"Leg");var foot=r.B(side+"Foot");float reach=Vector3.Distance(upper.position,knee.position)+Vector3.Distance(knee.position,foot.position)-.001f;excess=Mathf.Max(excess,Vector3.Distance(upper.position,side=="Right"?rf:lf)-reach);}
                    if(excess<=0)break;hips.position+=Vector3.down*(excess+.0005f)*1.25f;
                }
                r.LegIK("Right",rf,rr);r.LegIK("Left",lf,lr);
                foreach(string side in new[]{"Right","Left"})
                {
                    var h=r.B(side+"Hand");var palm=r.B(side+"HandMiddle1").position-h.position;var forward=h.position-r.B(side+"ForeArm").position;float angle=Vector3.Angle(palm,forward);
                    if(angle>20f)h.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(palm,forward),(angle-20f)/angle)*h.rotation;
                }
            }
        }
    }
    static AnimationClip MakeClip(Rig r,string name,bool throwing,float duration)
    {
        int sampleRate=throwing?120:60;int count=Mathf.RoundToInt(duration*sampleRate)+1;var sampled=r.bones.ToDictionary(t=>t,t=>new List<Quaternion>());var hipPositions=new List<Vector3>();
        for(int i=0;i<count;i++){Pose(r,throwing,duration*i/(count-1));hipPositions.Add(r.B("Hips").localPosition);foreach(var b in r.bones){var q=b.localRotation;var list=sampled[b];if(list.Count>0&&Quaternion.Dot(q,list.Last())<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);list.Add(q);}}
        var clip=new AnimationClip{name=name,frameRate=sampleRate};string[] props={"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
        foreach(var b in r.bones)
        {
            var list=sampled[b];bool moving=list.Any(q=>Quaternion.Angle(q,list[0])>.001f);var path=AnimationUtility.CalculateTransformPath(b,r.root.transform);
            for(int axis=0;axis<4;axis++)
            {
                var keys=new List<Keyframe>();int[] indices=moving?Enumerable.Range(0,count).ToArray():new[]{0,count-1};
                foreach(int i in indices)keys.Add(new Keyframe(duration*i/(count-1),list[i][axis]));var curve=new AnimationCurve(keys.ToArray());
                for(int i=0;i<keys.Count;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),props[axis]),curve);
            }
        }
        string hipPath=AnimationUtility.CalculateTransformPath(r.B("Hips"),r.root.transform);
        for(int axis=0;axis<3;axis++)
        {
            var keys=Enumerable.Range(0,count).Select(i=>new Keyframe(duration*i/(count-1),hipPositions[i][axis])).ToArray();var curve=new AnimationCurve(keys);
            for(int i=0;i<count;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(hipPath,typeof(Transform),new[]{"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z"}[axis]),curve);
        }
        clip.EnsureQuaternionContinuity();var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=!throwing;settings.loopBlend=!throwing;settings.keepOriginalPositionXZ=true;settings.keepOriginalPositionY=true;clip.wrapMode=throwing?WrapMode.Once:WrapMode.Loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
        string assetPath=Folder+"/"+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;EditorUtility.SetDirty(clip);}else AssetDatabase.CreateAsset(clip,assetPath);
        r.Reset();return clip;
    }
    [MenuItem("Tools/Potion Character/Build Idle And Throw")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);var log=new List<string>();PreviewRenderUtility preview=null;GameObject go=null;
        try
        {
            if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/PotionCharacter","CustomAnimations");
            preview=new PreviewRenderUtility();var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Model);go=PrefabUtility.InstantiatePrefab(asset,preview.camera.gameObject.scene) as GameObject;var rig=new Rig(go);
            if(rig.bones.Length<65)throw new Exception("Expected complete character skeleton");
            const string referencePath="Assets/PotionCharacter/AnimationReferences/Throw Object.fbx";
            referenceClip=AssetDatabase.LoadAllAssetsAtPath(referencePath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).OrderByDescending(c=>c.length).First();
            referenceObject=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(referencePath),preview.camera.gameObject.scene) as GameObject;
            foreach(var renderer in referenceObject.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            foreach(var sourceAnimator in referenceObject.GetComponentsInChildren<Animator>(true))sourceAnimator.enabled=false;
            referenceBones=referenceObject.GetComponentsInChildren<Transform>(true).Where(b=>b.name.Contains("mixamorig:")).ToDictionary(b=>b.name,b=>b);
            var bindWorld=new Dictionary<string,Matrix4x4>();
            foreach(var skin in referenceObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                for(int i=0;i<skin.bones.Length;i++)if(skin.bones[i]!=null)bindWorld[skin.bones[i].name]=skin.transform.localToWorldMatrix*skin.sharedMesh.bindposes[i].inverse;
            referenceBindLocal=new Dictionary<string,Quaternion>();
            foreach(var pair in referenceBones)
            {
                var bone=pair.Value;
                if(bindWorld.TryGetValue(bone.name,out var world) && bone.parent!=null && bindWorld.TryGetValue(bone.parent.name,out var parentWorld))referenceBindLocal[pair.Key]=(parentWorld.inverse*world).rotation;
                else if(bindWorld.TryGetValue(bone.name,out var rootWorld) && bone.parent!=null)referenceBindLocal[pair.Key]=(bone.parent.worldToLocalMatrix*rootWorld).rotation;
                else referenceBindLocal[pair.Key]=bone.localRotation;
            }
            float bindDelta=rig.bones.Where(b=>referenceBindLocal.ContainsKey(b.name)&&b.name!="mixamorig:Hips").Max(b=>Quaternion.Angle(b.localRotation,referenceBindLocal[b.name]));
            log.Add("Reference clip="+referenceClip.name+" length="+referenceClip.length+" frameRate="+referenceClip.frameRate+" bind rotation error="+bindDelta);
            var idle=MakeClip(rig,"Potion_Idle",false,3f);var throwing=MakeClip(rig,"Potion_Throw",true,1.6f);
            string cp=Folder+"/PotionCharacter.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(cp);
            if(!controller)
            {
                controller=AnimatorController.CreateAnimatorControllerAtPath(cp);controller.AddParameter("Throw",AnimatorControllerParameterType.Trigger);
                var sm=controller.layers[0].stateMachine;var a=sm.AddState("Idle");a.motion=idle;sm.defaultState=a;var b=sm.AddState("Throw");b.motion=throwing;
                var enter=a.AddTransition(b);enter.hasExitTime=false;enter.duration=.08f;enter.hasFixedDuration=true;enter.AddCondition(AnimatorConditionMode.If,0,"Throw");
                var exit=b.AddTransition(a);exit.hasExitTime=true;exit.exitTime=1;exit.duration=.10f;exit.hasFixedDuration=true;
            }
            var animator=go.GetComponent<Animator>();if(!animator)animator=go.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.enabled=false;rig.Reset();
            // Save a ready-to-use prefab without replacing the user's scene or existing imported clips.
            if(!go.GetComponent<PotionCharacterAnimation>())go.AddComponent<PotionCharacterAnimation>();
            animator.enabled=true;PrefabUtility.SaveAsPrefabAsset(go,Folder+"/PotionCharacter_Animated.prefab");animator.enabled=false;
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();int attached=0;
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)!=asset)continue;
                var sceneAnimator=t.GetComponent<Animator>();if(!sceneAnimator)sceneAnimator=Undo.AddComponent<Animator>(t.gameObject);
                if(sceneAnimator.runtimeAnimatorController && sceneAnimator.runtimeAnimatorController!=controller){log.Add("Existing scene Animator controller preserved: "+t.name);continue;}
                Undo.RecordObject(sceneAnimator,"Apply potion idle and throw");sceneAnimator.runtimeAnimatorController=controller;sceneAnimator.applyRootMotion=false;sceneAnimator.enabled=true;PrefabUtility.RecordPrefabInstancePropertyModifications(sceneAnimator);if(!t.GetComponent<PotionCharacterAnimation>())Undo.AddComponent<PotionCharacterAnimation>(t.gameObject);EditorSceneManager.MarkSceneDirty(scene);attached++;
            }
            log.Add("Scene characters connected: "+attached);
            Render(preview,rig,idle,throwing,log);
            Verify(rig,idle,throwing,log);
            AssetDatabase.SaveAssets();log.Add("PASS: clips, controller, prefab and previews created.");Debug.Log("Potion idle and throw animations ready.");
        }
        catch(Exception e){log.Add("ERROR "+e);Debug.LogException(e);}
        finally{if(preview!=null)preview.Cleanup();referenceObject=null;referenceClip=null;referenceBones=null;File.WriteAllLines(Output+"/"+Version+"_done.txt",log);}
    }
    static void Verify(Rig r,AnimationClip idle,AnimationClip throwing,List<string> log)
    {
        foreach(var clip in new[]{idle,throwing})
        {
            r.Reset();clip.SampleAnimation(r.root,0);var start=r.bones.ToDictionary(b=>b,b=>b.localRotation);var feet=new[]{r.B("LeftFoot").position,r.B("RightFoot").position};float drift=0;
            for(int j=0;j<=78;j++){clip.SampleAnimation(r.root,clip.length*j/78);drift=Mathf.Max(drift,Vector3.Distance(feet[0],r.B("LeftFoot").position),Vector3.Distance(feet[1],r.B("RightFoot").position));foreach(var b in r.bones)if(float.IsNaN(b.position.x))throw new Exception("Invalid pose");}
            clip.SampleAnimation(r.root,clip.length);float seam=r.bones.Max(b=>Quaternion.Angle(start[b],b.localRotation));if(drift>.0005f||seam>.05f)throw new Exception("Foot drift or end pose mismatch: "+clip.name+" "+drift+" "+seam);
            log.Add("PASS "+clip.name+" length="+clip.length+" foot drift="+drift+" end pose delta="+seam);
        }
        foreach(var clip in new[]{idle,throwing})
        {
            float bend=0;
            for(int j=0;j<=156;j++)
            {
                r.Reset();clip.SampleAnimation(r.root,clip.length*j/156f);
                foreach(string side in new[]{"Right","Left"}){var h=r.B(side+"Hand");bend=Mathf.Max(bend,Vector3.Angle(h.position-r.B(side+"ForeArm").position,r.B(side+"HandMiddle1").position-h.position));}
            }
            if(bend>(clip==throwing?23f:3f))throw new Exception("Excess wrist bend: "+bend);
            log.Add("PASS "+clip.name+" maximum wrist bend="+bend+" degrees across both hands");
        }
        float reach=-1;float reachTime=0;
        for(int j=0;j<=156;j++){r.Reset();float time=throwing.length*j/156f;throwing.SampleAnimation(r.root,time);float distance=r.B("RightHand").position.z-r.B("Spine2").position.z;if(distance>reach){reach=distance;reachTime=time;}}
        r.Reset();throwing.SampleAnimation(r.root,throwing.length*.36f/1.3f);var windup=r.B("RightHand").position;
        throwing.SampleAnimation(r.root,throwing.length*.59f/1.3f);float swing=Vector3.Distance(windup,r.B("RightHand").position);
        if(swing<.15f)throw new Exception("Reference throwing swing too short: "+swing);
        log.Add("PASS reference arm swing distance="+swing);
        log.Add("PASS forward hand extension="+reach+" at time="+reachTime);r.Reset();
    }
    public static void Render(PreviewRenderUtility preview,Rig r,AnimationClip idle,AnimationClip throwing,List<string> log)
    {
        preview.ambientColor=new Color(.5f,.5f,.5f);preview.lights[0].intensity=1.4f;preview.lights[0].transform.rotation=Quaternion.Euler(35,35,0);preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(345,215,0);
        var cam=preview.camera;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.16f,.18f,.21f);cam.orthographic=true;cam.orthographicSize=.62f;cam.nearClipPlane=.01f;cam.farClipPlane=10;
        var baked=new Dictionary<SkinnedMeshRenderer,MeshFilter>();
        foreach(var renderer in r.root.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var obj=new GameObject(renderer.name+"_Preview");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj,preview.camera.gameObject.scene);obj.transform.SetParent(renderer.transform,false);var f=obj.AddComponent<MeshFilter>();obj.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;baked[renderer]=f;renderer.enabled=false;
        }
        foreach(var clip in new[]{idle,throwing}.Distinct())for(int frame=0;frame<24;frame++)
        {
            r.Reset();clip.SampleAnimation(r.root,clip.length*frame/24f);
            if(clip.name=="Potion_MiddleFinger")r.root.GetComponent<PotionCharacterAnimation>().SetGestureHandPreview(frame/24f>=.065f && frame/24f<.93f);
            foreach(var item in baked){if(item.Value.sharedMesh)UnityEngine.Object.DestroyImmediate(item.Value.sharedMesh);var m=new Mesh();item.Key.BakeMesh(m);item.Value.sharedMesh=m;foreach(var v in m.vertices)if(float.IsNaN(v.x)||float.IsInfinity(v.x))throw new Exception("Invalid skinned mesh");}
            foreach(string angle in new[]{"front","side"})
            {
                var target=new Vector3(0,.5f,0);
                if(clip.name=="Potion_Roll")
                {
                    bool first=true;var bounds=new Bounds();
                    foreach(var item in baked)foreach(var vertex in item.Value.sharedMesh.vertices){var world=item.Value.transform.TransformPoint(vertex);if(first){bounds=new Bounds(world,Vector3.zero);first=false;}else bounds.Encapsulate(world);}
                    target.x=bounds.center.x;target.z=bounds.center.z;cam.orthographicSize=.76f;
                }
                else if(clip.name=="Potion_Jump"){cam.orthographicSize=.80f;target.y=.62f;}
                else cam.orthographicSize=.62f;
                cam.transform.position=target+(angle=="front"?new Vector3(.55f,.16f,2.8f):new Vector3(2.8f,.16f,.6f));cam.transform.LookAt(target);
                if(clip.name=="Potion_MiddleFinger" && angle=="side") {target=r.B("RightHand").position+Vector3.up*.035f;cam.orthographicSize=.12f;cam.transform.position=target+new Vector3(.2f,.08f,2.8f);cam.transform.LookAt(target);}
                preview.BeginStaticPreview(new Rect(0,0,480,600));preview.Render(true);var image=preview.EndStaticPreview();File.WriteAllBytes(Output+"/"+clip.name+"_"+angle+"_"+frame.ToString("D2")+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
        }
        foreach(var f in baked.Values)if(f.sharedMesh)UnityEngine.Object.DestroyImmediate(f.sharedMesh);log.Add("PASS: 96 Unity rendered preview frames; all baked vertices finite.");
    }
}
