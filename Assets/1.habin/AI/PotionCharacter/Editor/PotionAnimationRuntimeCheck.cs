using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
[InitializeOnLoad]
public static class PotionAnimationRuntimeCheck
{
    const string Report="C:/Users/user1/Documents/Codex/2026-10-26/referenced-chatgpt-conversation-this-is-an/work/custom_animation_preview/runtime_reference_f_check.txt";
    static PotionAnimationRuntimeCheck(){EditorApplication.delayCall+=Auto;}
    static void Auto(){if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Auto;return;}if(!File.Exists(Report))Run();}
    static void Run()
    {
        var scene=EditorSceneManager.NewPreviewScene();GameObject go=null;string result="";
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PotionCharacter/CustomAnimations/PotionCharacter_Animated.prefab");if(!prefab){EditorApplication.delayCall+=Auto;return;}
            go=PrefabUtility.InstantiatePrefab(prefab,scene) as GameObject;var animator=go.GetComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);
            if(!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))throw new Exception("Idle is not the initial state");
            var hand=go.GetComponentsInChildren<Transform>().Single(t=>t.name=="mixamorig:RightHand");var start=hand.position;
            animator.Update(.7f);float idleTravel=Vector3.Distance(start,hand.position);if(idleTravel<.0001f)throw new Exception("Idle curves did not animate the character");
            animator.SetTrigger("Throw");animator.Update(.01f);animator.Update(.1f);animator.Update(.25f);
            if(!animator.GetCurrentAnimatorStateInfo(0).IsName("Throw"))throw new Exception("Throw trigger did not enter Throw state");
            float throwTravel=Vector3.Distance(start,hand.position);if(throwTravel<.05f)throw new Exception("Throw curves did not animate the character");
            for(int i=0;i<100;i++)animator.Update(.02f);
            if(!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))throw new Exception("Throw did not return to Idle");
            result="PASS: Animator evaluates Idle, Throw trigger, and automatic return to Idle.\nIdle hand travel="+idleTravel+"; Throw hand travel="+throwTravel;
        }
        catch(Exception e){result="FAIL: "+e;Debug.LogException(e);}
        finally{if(go)UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);if(result.Length>0)File.WriteAllText(Report,result);}
    }
}
