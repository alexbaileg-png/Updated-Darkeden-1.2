using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class RebuildSwordMasterController
{
    [MenuItem("Tools/Rebuild SwordMaster Controller")]
    static void Rebuild()
    {
        const string controllerPath = "Assets/Characters/Slayers/Swordmaster/SwordMaster.controller";
        const string root = "Assets/Characters/Slayers/Swordmaster/";

        // Load existing controller — preserves GUID so prefab references stay valid
        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (ctrl == null)
        {
            Debug.LogError("[RebuildSwordMaster] Controller not found at: " + controllerPath);
            return;
        }

        // ── Load animation clips ──────────────────────────────────────────────
        AnimationClip idleClip         = LoadClip(root + "Meshy_AI_Slayer_Swordmaster_G_biped_Character_output@Idle.fbx");
        AnimationClip runClip          = LoadClip(root + "Meshy_AI_Slayer_Swordmaster_G_biped_Character_output@Running.fbx");
        AnimationClip deathClip        = LoadClip(root + "Meshy_AI_Slayer_Swordmaster_G_biped_Character_output@Sword And Shield Death.fbx");
        AnimationClip attackClip       = LoadClip(root + "Sword And Shield Slash.fbx");
        AnimationClip castClip         = LoadClip(root + "Meshy_AI_Slayer_Swordmaster_G_biped_Character_output@Standing 2H Cast Spell 01.fbx");
        AnimationClip consecrationClip = LoadClip(root + "Great Sword Jump Attack.fbx");

        // ── Clear existing state machine ──────────────────────────────────────
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        // Remove all states (in-place — preserves controller GUID)
        foreach (var cs in sm.states.ToArray())
            sm.RemoveState(cs.state);

        // Remove all AnyState transitions
        sm.anyStateTransitions = new AnimatorStateTransition[0];
        sm.entryTransitions    = new AnimatorTransition[0];

        // ── Clear & rebuild parameters ────────────────────────────────────────
        ctrl.parameters = new AnimatorControllerParameter[0];
        ctrl.AddParameter(new AnimatorControllerParameter { name = "MoveSpeed",    type = AnimatorControllerParameterType.Float,   defaultFloat = 1f });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "IsMoving",     type = AnimatorControllerParameterType.Bool });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "Attack",       type = AnimatorControllerParameterType.Trigger });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "Cast",         type = AnimatorControllerParameterType.Trigger });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "Spell",        type = AnimatorControllerParameterType.Trigger });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "Die",          type = AnimatorControllerParameterType.Trigger });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "Consecration", type = AnimatorControllerParameterType.Trigger });

        // ── Add states ────────────────────────────────────────────────────────
        AnimatorState idle         = sm.AddState("Idle",         new Vector3(230,  110));
        AnimatorState running      = sm.AddState("Running",      new Vector3(590,  110));
        AnimatorState death        = sm.AddState("Death",        new Vector3(170, -100));
        AnimatorState attack       = sm.AddState("Attack",       new Vector3(660, -110));
        AnimatorState spell        = sm.AddState("Spell",        new Vector3(410, -260));
        AnimatorState consecration = sm.AddState("Consecration", new Vector3(930, -260));

        if (idleClip != null)         idle.motion         = idleClip;
        if (runClip != null)          running.motion      = runClip;
        if (deathClip != null)        death.motion        = deathClip;
        if (attackClip != null)       attack.motion       = attackClip;
        if (castClip != null)         spell.motion        = castClip;
        if (consecrationClip != null) consecration.motion = consecrationClip;

        sm.defaultState = idle;

        // ── Transitions ───────────────────────────────────────────────────────
        // Idle ↔ Running
        Transition(idle,    running, "IsMoving",     AnimatorConditionMode.If,    exitTime: false);
        Transition(running, idle,    "IsMoving",     AnimatorConditionMode.IfNot, exitTime: false);

        // Idle / Running → action states (trigger-based, no exit time)
        foreach (var src in new[] { idle, running })
        {
            Transition(src, death,        "Die",          AnimatorConditionMode.If, exitTime: false);
            Transition(src, attack,       "Attack",       AnimatorConditionMode.If, exitTime: false);
            Transition(src, spell,        "Cast",         AnimatorConditionMode.If, exitTime: false);
            Transition(src, consecration, "Consecration", AnimatorConditionMode.If, exitTime: false);
        }

        // Action states return to Idle
        ReturnToIdle(attack,       idle);
        ReturnToIdle(spell,        idle);
        ReturnToIdle(consecration, idle);

        // ── Save ──────────────────────────────────────────────────────────────
        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[RebuildSwordMaster] Controller rebuilt in-place — GUID preserved. Path: " + controllerPath);
    }

    static void Transition(AnimatorState src, AnimatorState dst, string param,
                           AnimatorConditionMode mode, bool exitTime)
    {
        var t = src.AddTransition(dst);
        t.AddCondition(mode, 0, param);
        t.hasExitTime = exitTime;
        t.duration = 0.1f;
    }

    static void ReturnToIdle(AnimatorState src, AnimatorState idle)
    {
        var t = src.AddTransition(idle);
        t.hasExitTime = true;
        t.exitTime = 0.9f;
        t.duration = 0.1f;
    }

    static AnimationClip LoadClip(string path)
    {
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            if (obj is AnimationClip c && !c.name.StartsWith("__"))
                return c;
        Debug.LogWarning("[RebuildSwordMaster] No clip at: " + path);
        return null;
    }
}
