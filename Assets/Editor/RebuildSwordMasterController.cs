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

        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (ctrl == null) { Debug.LogError("[RebuildSwordMaster] Controller not found at: " + controllerPath); return; }

        AnimationClip idleClip         = LoadClip(root + "Meshy_AI_Slayer_Swordmaster_G_biped_Character_output@Idle.fbx");
        AnimationClip runClip          = LoadClip(root + "Meshy_AI_Slayer_Swordmaster_G_biped_Character_output@Running.fbx");
        AnimationClip deathClip        = LoadClip(root + "Meshy_AI_Slayer_Swordmaster_G_biped_Character_output@Sword And Shield Death.fbx");
        AnimationClip attackClip       = LoadClip(root + "Sword And Shield Slash.fbx");
        AnimationClip castClip         = LoadClip(root + "Meshy_AI_Slayer_Swordmaster_G_biped_Character_output@Standing 2H Cast Spell 01.fbx");
        AnimationClip consecrationClip = LoadClip(root + "Great Sword Jump Attack.fbx");

        ClearController(ctrl, controllerPath);

        ctrl.AddParameter(new AnimatorControllerParameter { name = "MoveSpeed",    type = AnimatorControllerParameterType.Float,   defaultFloat = 1f });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "IsMoving",     type = AnimatorControllerParameterType.Bool });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "Attack",       type = AnimatorControllerParameterType.Trigger });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "Cast",         type = AnimatorControllerParameterType.Trigger });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "Spell",        type = AnimatorControllerParameterType.Trigger });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "Die",          type = AnimatorControllerParameterType.Trigger });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "Consecration", type = AnimatorControllerParameterType.Trigger });

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
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

        Transition(idle,    running, "IsMoving", AnimatorConditionMode.If,    exitTime: false);
        Transition(running, idle,    "IsMoving", AnimatorConditionMode.IfNot, exitTime: false);

        foreach (var src in new[] { idle, running })
        {
            Transition(src, death,        "Die",          AnimatorConditionMode.If, exitTime: false);
            Transition(src, attack,       "Attack",       AnimatorConditionMode.If, exitTime: false);
            Transition(src, spell,        "Cast",         AnimatorConditionMode.If, exitTime: false);
            Transition(src, consecration, "Consecration", AnimatorConditionMode.If, exitTime: false);
        }

        ReturnToIdle(attack,       idle);
        ReturnToIdle(spell,        idle);
        ReturnToIdle(consecration, idle);

        Save(ctrl, controllerPath);
    }

    // ── SlayerFemale ──────────────────────────────────────────────────────────

    [MenuItem("Tools/Rebuild SlayerFemale Controller")]
    static void RebuildSlayerFemale()
    {
        const string controllerPath = "Assets/Characters/Slayers/SlayerFemaleAnimator.controller";
        const string healerNew = "Assets/Characters/Slayers/Healer/New Healer/Meshy_AI_Divine_Healer_Knight_biped (3)/Meshy_AI_Divine_Healer_Knight_biped/";
        const string healerOld = "Assets/Characters/Slayers/Healer/Meshy_AI_Divine_Healer_Knight_biped (4)/Meshy_AI_Divine_Healer_Knight_biped/";

        AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (ctrl == null) { Debug.LogError("[RebuildSlayerFemale] Controller not found: " + controllerPath); return; }

        AnimationClip idleClip      = LoadClip(healerOld + "Meshy_AI_Divine_Healer_Knight_biped_Animation_Idle_11_withSkin.fbx");
        AnimationClip runClip       = LoadClip(healerNew + "Meshy_AI_Divine_Healer_Knight_biped_Animation_Running_withSkin.fbx");
        AnimationClip deathClip     = LoadClip(healerNew + "Meshy_AI_Divine_Healer_Knight_biped_Animation_Dead_withSkin.fbx");
        AnimationClip buffCastClip  = LoadClip("Assets/Characters/Slayers/Swordmaster/Meshy_AI_Slayer_Swordmaster_G_biped_Character_output@Standing 2H Cast Spell 01.fbx");
        AnimationClip spellCastClip = LoadClip(healerNew + "Meshy_AI_Divine_Healer_Knight_biped_Animation_mage_soell_cast_4_withSkin.fbx");

        ClearController(ctrl, controllerPath);

        ctrl.AddParameter(new AnimatorControllerParameter { name = "IsMoving",  type = AnimatorControllerParameterType.Bool });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "MoveSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "Cast",      type = AnimatorControllerParameterType.Trigger });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "Spell",     type = AnimatorControllerParameterType.Trigger });
        ctrl.AddParameter(new AnimatorControllerParameter { name = "Die",       type = AnimatorControllerParameterType.Trigger });

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState idle      = sm.AddState("Idle",       new Vector3(230,  110));
        AnimatorState running   = sm.AddState("Running",    new Vector3(590,  110));
        AnimatorState death     = sm.AddState("Die",        new Vector3(170, -100));
        AnimatorState buffCast  = sm.AddState("Buff Cast",  new Vector3(590, -110));
        AnimatorState spellCast = sm.AddState("Spell Cast", new Vector3(410, -260));

        if (idleClip != null)      idle.motion      = idleClip;
        if (runClip != null)       running.motion   = runClip;
        if (deathClip != null)     death.motion     = deathClip;
        if (buffCastClip != null)  buffCast.motion  = buffCastClip;
        if (spellCastClip != null) spellCast.motion = spellCastClip;

        sm.defaultState = idle;

        Transition(idle,    running,   "IsMoving", AnimatorConditionMode.If,    exitTime: false);
        Transition(running, idle,      "IsMoving", AnimatorConditionMode.IfNot, exitTime: false);

        foreach (var src in new[] { idle, running })
        {
            Transition(src, death,     "Die",   AnimatorConditionMode.If, exitTime: false);
            Transition(src, buffCast,  "Cast",  AnimatorConditionMode.If, exitTime: false);
            Transition(src, spellCast, "Spell", AnimatorConditionMode.If, exitTime: false);
        }

        ReturnToIdle(buffCast,  idle);
        ReturnToIdle(spellCast, idle);

        Save(ctrl, controllerPath);
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    // Removes all states AND destroys orphaned sub-asset transitions that remain
    // after RemoveState (these dangling fileIDs crash Unity 6's Edge.WakeUp).
    static void ClearController(AnimatorController ctrl, string path)
    {
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        foreach (var cs in sm.states.ToArray())
            sm.RemoveState(cs.state);

        sm.anyStateTransitions = new AnimatorStateTransition[0];
        sm.entryTransitions    = new AnimatorTransition[0];
        ctrl.parameters        = new AnimatorControllerParameter[0];

        // Destroy any orphaned AnimatorStateTransition / AnimatorTransition sub-assets
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (obj == null || obj == ctrl) continue;
            if (obj is AnimatorStateTransition || obj is AnimatorTransition || obj is AnimatorState)
                Object.DestroyImmediate(obj, true);
        }
    }

    static void Save(AnimatorController ctrl, string path)
    {
        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RebuildController] Rebuilt in-place — GUID preserved. Path: " + path);
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
        Debug.LogWarning("[RebuildController] No clip at: " + path);
        return null;
    }
}
