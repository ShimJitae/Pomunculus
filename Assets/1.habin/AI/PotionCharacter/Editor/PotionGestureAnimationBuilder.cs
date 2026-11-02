using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PotionGestureAnimationBuilder
{
    const string Folder="Assets/PotionCharacter/CustomAnimations";
    const string Model="Assets/PotionCharacter/tripo_convert_7b219cfe-0b4e-4c79-bd43-81db06190188.fbx";
    const string Output="C:/Users/user1/Documents/Codex/2026-10-26/referenced-chatgpt-conversation-this-is-an/work/custom_animation_preview";
    const float Duration=2.0f;
    static PotionGestureAnimationBuilder(){EditorApplication.delayCall+=Auto;}
    static void Auto()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Auto;return;}
        if(!File.Exists(Output+"/gesture_v5_done.txt"))Build();
    }
    static float Ease(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
    static void Pose(PotionCustomAnimationBuilder.Rig r,float time)
    {
        r.Reset();float blend=time<.40f?Ease(time/.40f):1-Ease(Mathf.InverseLerp(1.45f,Duration,time));
        var rest=r.bones.ToDictionary(b=>b,b=>b.localRotation);var restHip=r.B("Hips").localPosition;
        var rf=r.B("RightFoot");var lf=r.B("LeftFoot");var rp=rf.position;var lp=lf.position;var rr=rf.rotation;var lr=lf.rotation;
        var left=r.B("LeftHand").position;
        r.B("Hips").position+=new Vector3(-.008f,-.008f,-.006f);
        r.Turn("Hips",new Vector3(0,-5,0));r.Turn("Spine",new Vector3(-2,-4,0));r.Turn("Spine1",new Vector3(-2,-5,-2));r.Turn("Spine2",new Vector3(0,-5,-2));r.Turn("Head",new Vector3(0,10,3));
        r.LegIK("Right",rp,rr);r.LegIK("Left",lp,lr);
        r.IK("Left",left+new Vector3(-.012f,.014f,.015f),new Vector3(-.4f,.45f,-.1f));
        r.IK("Right",new Vector3(.22f,.64f,.19f),new Vector3(.42f,.40f,.03f));
        var hand=r.B("RightHand");var middle=r.B("RightHandMiddle1");
        var along=(r.B("RightHandMiddle2").position-middle.position).normalized;
        var across=(r.B("RightHandIndex1").position-r.B("RightHandPinky1").position).normalized;
        var normal=Vector3.Cross(across,along).normalized;
        hand.rotation=Quaternion.LookRotation(Vector3.forward,Vector3.up)*Quaternion.Inverse(Quaternion.LookRotation(normal,along))*hand.rotation;
        var targetHip=r.B("Hips").localPosition;
        foreach(var b in r.bones)b.localRotation=Quaternion.Slerp(rest[b],b.localRotation,blend);
        r.B("Hips").localPosition=Vector3.Lerp(restHip,targetHip,blend);
        // Keep the boots planted while the torso shifts through the gesture.
        if(blend>0){r.LegIK("Right",rp,rr);r.LegIK("Left",lp,lr);}
    }
    static Mesh handOriginal,handVariant;
    static SkinnedMeshRenderer handRenderer;
    static Mesh MakeGestureHand(PotionCustomAnimationBuilder.Rig rig)
    {
        handRenderer=rig.root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(x=>x.bones.Any(b=>b && b.name=="mixamorig:RightHand") && x.name=="tripo_part_2");
        handOriginal=handRenderer.sharedMesh;var src=handOriginal;var weights=src.boneWeights;
        int handIndex=Array.FindIndex(handRenderer.bones,b=>b && b.name=="mixamorig:RightHand");
        var fingerIndices=new HashSet<int>(Enumerable.Range(0,handRenderer.bones.Length).Where(i=>handRenderer.bones[i] && handRenderer.bones[i].name.StartsWith("mixamorig:RightHand")));
        float HandWeight(int i){var w=weights[i];return (fingerIndices.Contains(w.boneIndex0)?w.weight0:0)+(fingerIndices.Contains(w.boneIndex1)?w.weight1:0)+(fingerIndices.Contains(w.boneIndex2)?w.weight2:0)+(fingerIndices.Contains(w.boneIndex3)?w.weight3:0);}
        var vertices=src.vertices.ToList();var normals=src.normals.ToList();var uvs=src.uv.ToList();var ws=weights.ToList();
        var sub=new List<int>[src.subMeshCount];int removed=0;
        for(int n=0;n<sub.Length;n++){sub[n]=new List<int>();var tri=src.GetTriangles(n);for(int i=0;i<tri.Length;i+=3){if(Mathf.Max(HandWeight(tri[i]),HandWeight(tri[i+1]),HandWeight(tri[i+2]))>.35f){removed++;continue;}sub[n].AddRange(new[]{tri[i],tri[i+1],tri[i+2]});}}
        Vector2 brown=uvs[Enumerable.Range(0,weights.Length).OrderByDescending(HandWeight).First()],skin=brown;
        var texture=handRenderer.sharedMaterials[0].mainTexture;
        if(texture)
        {
            var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;Graphics.Blit(texture,rt);RenderTexture.active=rt;
            var readable=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);readable.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);readable.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);
            Vector2 Pick(Color target){float best=float.PositiveInfinity;Vector2 uv=brown;for(int i=0;i<weights.Length;i++){if(HandWeight(i)<.5f)continue;var c=readable.GetPixelBilinear(uvs[i].x,uvs[i].y);float d=(new Vector3(c.r,c.g,c.b)-new Vector3(target.r,target.g,target.b)).sqrMagnitude;if(d<best){best=d;uv=uvs[i];}}return uv;}
            brown=Pick(new Color(.25f,.16f,.13f));skin=Pick(new Color(.62f,.37f,.21f));UnityEngine.Object.DestroyImmediate(readable);
        }
        Pose(rig,.9f);var wrist=rig.B("RightHand");var origin=wrist.position;
        var toMesh=src.bindposes[handIndex].inverse*wrist.worldToLocalMatrix;
        Vector3 Map(Vector3 p)=>toMesh.MultiplyPoint3x4(origin+p);
        void Face(Vector3 a,Vector3 b,Vector3 c,Vector2 uv)
        {
            var swap=b;b=c;c=swap;a=Map(a);b=Map(b);c=Map(c);var normal=Vector3.Cross(b-a,c-a).normalized;
            foreach(var v in new[]{a,b,c}){sub[0].Add(vertices.Count);vertices.Add(v);normals.Add(normal);uvs.Add(uv);ws.Add(new BoneWeight{boneIndex0=handIndex,weight0=1});}
        }
        void Tube(Vector3[] points,float[] widths,float depth,bool finger)
        {
            const int sides=8;var rings=new Vector3[points.Length,sides];
            for(int k=0;k<points.Length;k++)
            {
                var tangent=(points[Mathf.Min(k+1,points.Length-1)]-points[Mathf.Max(k-1,0)]).normalized;
                var across=Vector3.ProjectOnPlane(Vector3.right,tangent).normalized;if(across.sqrMagnitude<.1f)across=Vector3.up;
                var other=Vector3.Cross(across,tangent).normalized;
                for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;rings[k,j]=points[k]+across*(Mathf.Cos(a)*widths[k])+other*(Mathf.Sin(a)*widths[k]*depth);}
            }
            for(int k=0;k<points.Length-1;k++)for(int j=0;j<sides;j++){int next=(j+1)%sides;var uv=finger && k>=1?skin:brown;Face(rings[k,j],rings[k+1,next],rings[k+1,j],uv);Face(rings[k,j],rings[k,next],rings[k+1,next],uv);}
            for(int j=0;j<sides;j++){Face(points[0],rings[0,(j+1)%sides],rings[0,j],brown);int last=points.Length-1;Face(points[last],rings[last,j],rings[last,(j+1)%sides],finger?skin:brown);}
        }
        Tube(new[]{new Vector3(0,-.022f,0),new Vector3(0,.013f,0),new Vector3(0,.044f,0),new Vector3(0,.054f,0)},new[]{.025f,.035f,.035f,.030f},.53f,false);
        float[] xs={.024f,.008f,-.010f,-.026f};
        for(int f=0;f<4;f++)
        {
            float x=xs[f],rad=f==3?.007f:.0082f;
            var points=f==1?new[]{new Vector3(x,.049f,0),new Vector3(x,.071f,0),new Vector3(x,.093f,0),new Vector3(x,.108f,0)}:new[]{new Vector3(x,.048f,0),new Vector3(x,.061f,.017f),new Vector3(x,.048f,.032f),new Vector3(x,.033f,.026f)};
            Tube(points,new[]{rad,rad,rad*.91f,rad*.72f},.90f,true);
        }
        Tube(new[]{new Vector3(.026f,.006f,.004f),new Vector3(.040f,.021f,.020f),new Vector3(.024f,.036f,.040f),new Vector3(.006f,.039f,.043f)},new[]{.010f,.011f,.010f,.008f},.85f,true);
        var mesh=new Mesh{name="Potion_Gesture_RightHand"};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.boneWeights=ws.ToArray();mesh.bindposes=src.bindposes;mesh.subMeshCount=sub.Length;for(int n=0;n<sub.Length;n++)mesh.SetTriangles(sub[n],n);mesh.RecalculateBounds();
        if(!AssetDatabase.IsValidFolder("Assets/PotionCharacter/Resources"))AssetDatabase.CreateFolder("Assets/PotionCharacter","Resources");string path="Assets/PotionCharacter/Resources/PotionGestureRightHand.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
        rig.Reset();return mesh;
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
        var clip=new AnimationClip{name="Potion_MiddleFinger",frameRate=rate};
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
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/Potion_MiddleFinger.anim");if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);clip=existing;EditorUtility.SetDirty(clip);}else AssetDatabase.CreateAsset(clip,Folder+"/Potion_MiddleFinger.anim");rig.Reset();return clip;
    }
    static void Connect(AnimationClip jump)
    {
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/PotionCharacter.controller");if(!controller.parameters.Any(p=>p.name=="MiddleFinger"))controller.AddParameter("MiddleFinger",AnimatorControllerParameterType.Trigger);
        var sm=controller.layers[0].stateMachine;var idle=sm.states.Single(s=>s.state.name=="Idle").state;var walk=sm.states.Single(s=>s.state.name=="Walk").state;var run=sm.states.Single(s=>s.state.name=="Run").state;
        var jumping=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="MiddleFinger");if(!jumping)jumping=sm.AddState("MiddleFinger",new Vector3(700,220));jumping.motion=jump;
        foreach(var from in new[]{idle,walk,run})
        {
            var enter=from.transitions.FirstOrDefault(t=>t.destinationState==jumping);if(!enter)enter=from.AddTransition(jumping);foreach(var condition in enter.conditions)enter.RemoveCondition(condition);enter.AddCondition(AnimatorConditionMode.If,0,"MiddleFinger");enter.hasExitTime=false;enter.hasFixedDuration=true;enter.duration=.025f;from.transitions=from.transitions.OrderBy(t=>t.destinationState==jumping?0:1).ToArray();
        }
        AnimatorStateTransition Exit(AnimatorState state)
        {
            var t=jumping.transitions.FirstOrDefault(x=>x.destinationState==state);if(!t)t=jumping.AddTransition(state);foreach(var condition in t.conditions)t.RemoveCondition(condition);t.hasExitTime=true;t.exitTime=.97f;t.hasFixedDuration=true;t.duration=.10f;return t;
        }
        var exit=Exit(idle);exit.AddCondition(AnimatorConditionMode.IfNot,0,"Walking");
        exit=Exit(walk);exit.AddCondition(AnimatorConditionMode.If,0,"Walking");exit.AddCondition(AnimatorConditionMode.IfNot,0,"Running");
        exit=Exit(run);exit.AddCondition(AnimatorConditionMode.If,0,"Walking");exit.AddCondition(AnimatorConditionMode.If,0,"Running");EditorUtility.SetDirty(controller);
    }
    static void Verify(PotionCustomAnimationBuilder.Rig rig,AnimationClip clip,List<string> log)
    {
        rig.Reset();clip.SampleAnimation(rig.root,0);var start=rig.bones.ToDictionary(b=>b,b=>b.localRotation);var foot=rig.B("RightFoot").position;float drift=0;
        for(int i=0;i<=120;i++)
        {
            rig.Reset();clip.SampleAnimation(rig.root,Duration*i/120f);
            drift=Mathf.Max(drift,Vector3.Distance(foot,rig.B("RightFoot").position));
            foreach(var b in rig.bones)if(float.IsNaN(b.position.x)||float.IsInfinity(b.position.x))throw new Exception("Invalid gesture bone");
        }
        rig.Reset();clip.SampleAnimation(rig.root,Duration);float seam=rig.bones.Max(b=>Quaternion.Angle(start[b],b.localRotation));
        if(seam>.05f||drift>.003f)throw new Exception("Gesture seam="+seam+" foot drift="+drift);
        log.Add("PASS return pose="+seam+" foot drift="+drift);
        rig.Reset();clip.SampleAnimation(rig.root,.9f);
        rig.root.GetComponent<PotionCharacterAnimation>().SetGestureHandPreview(true);if(handRenderer.sharedMesh!=handVariant)throw new Exception("Gesture hand mesh did not animate");
        clip.SampleAnimation(rig.root,Duration);rig.root.GetComponent<PotionCharacterAnimation>().SetGestureHandPreview(false);if(handRenderer.sharedMesh!=handOriginal)throw new Exception("Original hand was not restored");
        log.Add("PASS clean hand displayed during gesture and original hand restored afterwards");rig.Reset();
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
                animator.SetTrigger("MiddleFinger");animator.Update(.01f);animator.Update(.09f);if(!animator.GetCurrentAnimatorStateInfo(0).IsName("MiddleFinger"))throw new Exception("Gesture failed from "+locomotion);
                var driver=go.GetComponent<PotionCharacterAnimation>();if(!driver)driver=go.AddComponent<PotionCharacterAnimation>();animator.Update(.12f);driver.RefreshGestureHand();
                var skin=go.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="tripo_part_2");if(skin.sharedMesh!=handVariant)throw new Exception("Runtime gesture hand missing");
                for(int i=0;i<120;i++)animator.Update(.02f);if(!animator.GetCurrentAnimatorStateInfo(0).IsName(locomotion))throw new Exception("Gesture recovery failed: "+locomotion);
                driver.RefreshGestureHand();if(skin.sharedMesh!=handOriginal)throw new Exception("Runtime original hand not restored");
                UnityEngine.Object.DestroyImmediate(go);go=null;log.Add("PASS "+locomotion+" > MiddleFinger > "+locomotion);
            }
        }
        finally{if(go)UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
    }
    [MenuItem("Tools/Potion Character/Build Middle Finger Animation")]
    public static void Build()
    {
        Directory.CreateDirectory(Output);var log=new List<string>();PreviewRenderUtility preview=null;
        try
        {
            preview=new PreviewRenderUtility();var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Model);var go=PrefabUtility.InstantiatePrefab(asset,preview.camera.gameObject.scene) as GameObject;foreach(var animator in go.GetComponentsInChildren<Animator>())animator.enabled=false;
            if(!go.GetComponent<PotionCharacterAnimation>())go.AddComponent<PotionCharacterAnimation>();var rig=new PotionCustomAnimationBuilder.Rig(go);handVariant=MakeGestureHand(rig);var clip=Bake(rig);Connect(clip);PotionCustomAnimationBuilder.Render(preview,rig,clip,clip,log);Verify(rig,clip,log);VerifyController(asset,log);
            AssetDatabase.SaveAssets();log.Add("PASS Potion_MiddleFinger.anim saved and connected; middle finger gesture preview with M.");Debug.Log("Potion middle finger animation ready.");
        }
        catch(Exception e){log.Add("ERROR "+e);Debug.LogException(e);}
        finally{if(preview!=null)preview.Cleanup();File.WriteAllLines(Output+"/gesture_v5_done.txt",log);}
    }
}

