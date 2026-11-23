using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace OCBuilders
{
    /// <summary>
    /// Fase 3 — clips y controladores de los tres enemigos nuevos.
    ///
    /// Mismo esquema que la Fase 1 y la Fase 2: clip de sprites sobre el hijo «Visual» y los
    /// parámetros Speed / Facing / Shoot / Hurt / Die, de modo que EnemyBase y los scripts de
    /// Fase 3 funcionan sin tocar el sistema de animación existente.
    /// </summary>
    public static class OC_Phase3Anim
    {
        private const string SpriteEnemies = "Assets/OperacionCuatro/Sprites/Enemies/";
        private const string AnimRoot = "Assets/OperacionCuatro/Animations/";
        private const string CtrlRoot = "Assets/OperacionCuatro/Animations/Controllers/";

        public static void Build()
        {
            EnsureFolder(AnimRoot);
            EnsureFolder(CtrlRoot);

            BuildSentinel();
            BuildLaserDrone();
            BuildGrenadier();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[OC] Animaciones de Fase 3 creadas (Sentinel, LaserDrone, Grenadier).");
        }

        // ==================================================================
        private static void BuildSentinel()
        {
            string s = SpriteEnemies + "Sentinel/Sentinel_";
            string dir = AnimRoot + "Sentinel/";
            EnsureFolder(dir);

            var idle = Clip(dir + "Sentinel_idle.anim", 5f, true, Load(s + "idle_0"), Load(s + "idle_1"));
            var walk = Clip(dir + "Sentinel_walk.anim", 8f, true,
                Load(s + "walk_0"), Load(s + "walk_1"), Load(s + "walk_2"), Load(s + "walk_3"));
            var shoot = Clip(dir + "Sentinel_shoot.anim", 12f, false, Load(s + "shoot"), Load(s + "idle_0"));
            var hurt = Clip(dir + "Sentinel_hurt.anim", 10f, false, Load(s + "hurt"), Load(s + "idle_0"));
            var death = Clip(dir + "Sentinel_death.anim", 8f, false, Load(s + "death"));

            BuildController("Sentinel", idle, walk, shoot, hurt, death, false);
        }

        private static void BuildLaserDrone()
        {
            string s = SpriteEnemies + "LaserDrone/LaserDrone_";
            string dir = AnimRoot + "LaserDrone/";
            EnsureFolder(dir);

            var idle = Clip(dir + "LaserDrone_idle.anim", 7f, true, Load(s + "idle_0"), Load(s + "idle_1"));
            var walk = Clip(dir + "LaserDrone_walk.anim", 9f, true, Load(s + "walk_0"), Load(s + "walk_1"));
            var shoot = Clip(dir + "LaserDrone_shoot.anim", 12f, false, Load(s + "shoot"), Load(s + "idle_0"));
            var hurt = Clip(dir + "LaserDrone_hurt.anim", 10f, false, Load(s + "hurt"), Load(s + "idle_0"));
            var death = Clip(dir + "LaserDrone_death.anim", 8f, false, Load(s + "death"));

            BuildController("LaserDrone", idle, walk, shoot, hurt, death, false);
        }

        private static void BuildGrenadier()
        {
            string s = SpriteEnemies + "Grenadier/Grenadier_";
            string dir = AnimRoot + "Grenadier/";
            EnsureFolder(dir);

            var idle = Clip(dir + "Grenadier_idle.anim", 5f, true, Load(s + "idle_0"), Load(s + "idle_1"));
            var walk = Clip(dir + "Grenadier_walk.anim", 8f, true,
                Load(s + "walk_0"), Load(s + "walk_1"), Load(s + "walk_2"), Load(s + "walk_3"));
            var shoot = Clip(dir + "Grenadier_throw.anim", 12f, false, Load(s + "shoot"), Load(s + "idle_0"));
            var hurt = Clip(dir + "Grenadier_hurt.anim", 10f, false, Load(s + "hurt"), Load(s + "idle_0"));
            var death = Clip(dir + "Grenadier_death.anim", 8f, false, Load(s + "death"));

            BuildController("Grenadier", idle, walk, shoot, hurt, death, true);
        }

        // ==================================================================
        private static void BuildController(string name, AnimationClip idle, AnimationClip walk,
            AnimationClip shoot, AnimationClip hurt, AnimationClip death, bool withThrow)
        {
            string ctrlPath = CtrlRoot + name + "Controller.controller";
            AssetDatabase.DeleteAsset(ctrlPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            EnsureLayerWeight(controller);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Facing", AnimatorControllerParameterType.Float);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            if (withThrow) controller.AddParameter("Throw", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;

            var stIdle = sm.AddState("Idle", new Vector3(260f, 0f, 0f));
            stIdle.motion = idle;

            var stHurt = sm.AddState("Hurt", new Vector3(520f, -120f, 0f));
            stHurt.motion = hurt;

            var stDeath = sm.AddState("Death", new Vector3(760f, -120f, 0f));
            stDeath.motion = death;

            sm.defaultState = stIdle;

            if (walk != null)
            {
                var stWalk = sm.AddState("Walk", new Vector3(260f, 110f, 0f));
                stWalk.motion = walk;
                Transition(stIdle, stWalk, 0.08f, false).AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");
                Transition(stWalk, stIdle, 0.08f, false).AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");
            }

            if (shoot != null)
            {
                var stShoot = sm.AddState(withThrow ? "Throw" : "Shoot", new Vector3(520f, 10f, 0f));
                stShoot.motion = shoot;

                var anyShoot = sm.AddAnyStateTransition(stShoot);
                anyShoot.hasExitTime = false;
                anyShoot.duration = 0.04f;
                anyShoot.canTransitionToSelf = false;
                anyShoot.AddCondition(AnimatorConditionMode.If, 0f, "Shoot");

                if (withThrow)
                {
                    var anyThrow = sm.AddAnyStateTransition(stShoot);
                    anyThrow.hasExitTime = false;
                    anyThrow.duration = 0.04f;
                    anyThrow.canTransitionToSelf = false;
                    anyThrow.AddCondition(AnimatorConditionMode.If, 0f, "Throw");
                }

                Transition(stShoot, stIdle, 0.05f, true);
            }

            var anyHurt = sm.AddAnyStateTransition(stHurt);
            anyHurt.hasExitTime = false;
            anyHurt.duration = 0.04f;
            anyHurt.canTransitionToSelf = false;
            anyHurt.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");

            var anyDeath = sm.AddAnyStateTransition(stDeath);
            anyDeath.hasExitTime = false;
            anyDeath.duration = 0.05f;
            anyDeath.canTransitionToSelf = false;
            anyDeath.AddCondition(AnimatorConditionMode.If, 0f, "Die");

            Transition(stHurt, stIdle, 0.06f, true);

            EditorUtility.SetDirty(controller);
        }

        private static void EnsureLayerWeight(AnimatorController controller)
        {
            var layers = controller.layers;
            if (layers == null || layers.Length == 0) return;

            bool changed = false;
            for (int i = 0; i < layers.Length; i++)
            {
                if (!Mathf.Approximately(layers[i].defaultWeight, 1f))
                {
                    layers[i].defaultWeight = 1f;
                    changed = true;
                }
            }

            if (changed) controller.layers = layers;
        }

        private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, float duration, bool exitTime)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = exitTime;
            t.exitTime = exitTime ? 0.92f : 0f;
            t.duration = duration;
            t.hasFixedDuration = true;
            return t;
        }

        private static Sprite Load(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path + ".png");
            if (sprite == null) Debug.LogError("[OC] Sprite no encontrado: " + path + ".png");
            return sprite;
        }

        private static AnimationClip Clip(string path, float fps, bool loop, params Sprite[] frames)
        {
            var valid = new List<Sprite>();
            for (int i = 0; i < frames.Length; i++) if (frames[i] != null) valid.Add(frames[i]);
            if (valid.Count == 0) return null;

            var clip = new AnimationClip();
            clip.frameRate = fps;

            var keys = new ObjectReferenceKeyframe[valid.Count];
            for (int i = 0; i < valid.Count; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / fps,
                    value = valid[i]
                };
            }

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "Visual",
                propertyName = "m_Sprite"
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void EnsureFolder(string path)
        {
            path = path.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
