using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace OCBuilders
{
    /// <summary>
    /// Fase 2 — clips y controladores de los tres enemigos nuevos.
    ///
    /// Reutiliza el mismo esquema de la Fase 1 (clip con curva de sprite sobre el hijo "Visual",
    /// parámetros Speed/Facing/Shoot/Hurt/Die) para que EnemyBase y los scripts nuevos funcionen
    /// sin cambios en el sistema de animación existente.
    /// </summary>
    public static class OC_Phase2Anim
    {
        private const string SpriteEnemies = "Assets/OperacionCuatro/Sprites/Enemies/";
        private const string AnimRoot = "Assets/OperacionCuatro/Animations/";
        private const string CtrlRoot = "Assets/OperacionCuatro/Animations/Controllers/";

        public static void Build()
        {
            EnsureFolder(AnimRoot);
            EnsureFolder(CtrlRoot);

            BuildArmored();
            BuildDrone();
            BuildTurret();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[OC] Animaciones de Fase 2 creadas (Armored, Drone, Turret).");
        }

        // ==================================================================
        private static void BuildArmored()
        {
            string s = SpriteEnemies + "Armored/Armored_";
            string dir = AnimRoot + "Armored/";
            EnsureFolder(dir);

            var idle = Clip(dir + "Armored_idle.anim", 5f, true, Load(s + "idle_0"), Load(s + "idle_1"));
            var walk = Clip(dir + "Armored_walk.anim", 8f, true,
                Load(s + "walk_0"), Load(s + "walk_1"), Load(s + "walk_2"), Load(s + "walk_3"));
            var shoot = Clip(dir + "Armored_shoot.anim", 12f, false, Load(s + "shoot"), Load(s + "idle_0"));
            var hurt = Clip(dir + "Armored_hurt.anim", 10f, false, Load(s + "hurt"), Load(s + "idle_0"));
            var death = Clip(dir + "Armored_death.anim", 8f, false, Load(s + "death"));

            BuildController("Armored", idle, walk, shoot, hurt, death);
        }

        private static void BuildDrone()
        {
            string s = SpriteEnemies + "Drone/Drone_";
            string dir = AnimRoot + "Drone/";
            EnsureFolder(dir);

            var idle = Clip(dir + "Drone_idle.anim", 7f, true, Load(s + "idle_0"), Load(s + "idle_1"));
            var walk = Clip(dir + "Drone_walk.anim", 9f, true, Load(s + "walk_0"), Load(s + "walk_1"));
            var shoot = Clip(dir + "Drone_shoot.anim", 12f, false, Load(s + "shoot"), Load(s + "idle_0"));
            var hurt = Clip(dir + "Drone_hurt.anim", 10f, false, Load(s + "hurt"), Load(s + "idle_0"));
            var death = Clip(dir + "Drone_death.anim", 8f, false, Load(s + "death"));

            BuildController("Drone", idle, walk, shoot, hurt, death);
        }

        private static void BuildTurret()
        {
            string s = SpriteEnemies + "Turret/Turret_";
            string dir = AnimRoot + "Turret/";
            EnsureFolder(dir);

            var idle = Clip(dir + "Turret_idle.anim", 4f, true, Load(s + "idle_0"));
            var shoot = Clip(dir + "Turret_shoot.anim", 10f, false, Load(s + "shoot"), Load(s + "idle_0"));
            var hurt = Clip(dir + "Turret_hurt.anim", 10f, false, Load(s + "hurt"), Load(s + "idle_0"));
            var death = Clip(dir + "Turret_death.anim", 8f, false, Load(s + "death"));

            BuildController("Turret", idle, null, shoot, hurt, death);
        }

        // ==================================================================
        private static void BuildController(string name, AnimationClip idle, AnimationClip walk,
            AnimationClip shoot, AnimationClip hurt, AnimationClip death)
        {
            string ctrlPath = CtrlRoot + name + "Controller.controller";
            AssetDatabase.DeleteAsset(ctrlPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            EnsureLayerWeight(controller);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Facing", AnimatorControllerParameterType.Float);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
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
                var stShoot = sm.AddState("Shoot", new Vector3(520f, 10f, 0f));
                stShoot.motion = shoot;

                var anyShoot = sm.AddAnyStateTransition(stShoot);
                anyShoot.hasExitTime = false;
                anyShoot.duration = 0.04f;
                anyShoot.canTransitionToSelf = false;
                anyShoot.AddCondition(AnimatorConditionMode.If, 0f, "Shoot");

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

        private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, float duration, bool exitTime,
            params (AnimatorConditionMode mode, float threshold, string parameter)[] conditions)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = exitTime;
            t.exitTime = exitTime ? 0.92f : 0f;
            t.duration = duration;
            t.hasFixedDuration = true;

            if (conditions != null)
            {
                for (int i = 0; i < conditions.Length; i++)
                    t.AddCondition(conditions[i].mode, conditions[i].threshold, conditions[i].parameter);
            }

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
