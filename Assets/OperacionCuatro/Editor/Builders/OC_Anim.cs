using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace OCBuilders
{
    /// <summary>Módulo 3: clips de animación y controladores de Animator.</summary>
    public static class OC_Anim
    {
        private const string SpriteChars = "Assets/OperacionCuatro/Sprites/Characters/";
        private const string SpriteEnemies = "Assets/OperacionCuatro/Sprites/Enemies/";
        private const string AnimRoot = "Assets/OperacionCuatro/Animations/";
        private const string CtrlRoot = "Assets/OperacionCuatro/Animations/Controllers/";

        public static void Build()
        {
            EnsureFolder(AnimRoot);
            EnsureFolder(CtrlRoot);

            string[] players = { "Danner", "Cesar", "Sebastian", "Ines" };
            for (int i = 0; i < players.Length; i++) BuildPlayer(players[i]);

            BuildRaider();
            BuildGunner();
            BuildFX();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[OC] Animaciones y controladores creados.");
        }

        // ------------------------------------------------------------------
        private static void BuildPlayer(string name)
        {
            string s = SpriteChars + name + "/" + name + "_";
            string dir = AnimRoot + name + "/";
            EnsureFolder(dir);

            var idle = Clip(dir + name + "_idle.anim", 6f, true,
                Load(s + "idle_0"), Load(s + "idle_1"));

            var walk = Clip(dir + name + "_walk.anim", 10f, true,
                Load(s + "walk_0"), Load(s + "walk_1"), Load(s + "walk_2"), Load(s + "walk_3"));

            var jump = Clip(dir + name + "_jump.anim", 12f, false, Load(s + "jump"));
            var fall = Clip(dir + name + "_fall.anim", 12f, false, Load(s + "fall"));
            var crouch = Clip(dir + name + "_crouch.anim", 12f, false, Load(s + "crouch"));

            var shoot = Clip(dir + name + "_shoot.anim", 18f, false, Load(s + "shoot"), Load(s + "idle_0"));
            var throwClip = Clip(dir + name + "_throw.anim", 12f, false, Load(s + "throw"), Load(s + "idle_0"));
            var hurt = Clip(dir + name + "_hurt.anim", 10f, false, Load(s + "hurt"), Load(s + "idle_0"));
            var death = Clip(dir + name + "_death.anim", 8f, false, Load(s + "death"));

            string ctrlPath = CtrlRoot + name + "Controller.controller";
            AssetDatabase.DeleteAsset(ctrlPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            EnsureLayerWeight(controller);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("YSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Crouch", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Throw", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;

            var stIdle = sm.AddState("Idle", new Vector3(260f, 0f, 0f));
            stIdle.motion = idle;

            var stWalk = sm.AddState("Walk", new Vector3(260f, 90f, 0f));
            stWalk.motion = walk;

            var stJump = sm.AddState("Jump", new Vector3(520f, 150f, 0f));
            stJump.motion = jump;

            var stFall = sm.AddState("Fall", new Vector3(520f, 60f, 0f));
            stFall.motion = fall;

            var stCrouch = sm.AddState("Crouch", new Vector3(260f, -90f, 0f));
            stCrouch.motion = crouch;

            var stShoot = sm.AddState("Shoot", new Vector3(520f, -40f, 0f));
            stShoot.motion = shoot;

            var stThrow = sm.AddState("Throw", new Vector3(520f, -130f, 0f));
            stThrow.motion = throwClip;

            var stHurt = sm.AddState("Hurt", new Vector3(520f, -220f, 0f));
            stHurt.motion = hurt;

            var stDeath = sm.AddState("Death", new Vector3(780f, -220f, 0f));
            stDeath.motion = death;

            sm.defaultState = stIdle;
            sm.states = sm.states; // fuerza refresco interno

            // ---- Transiciones de locomoción
            Transition(stIdle, stWalk, 0.08f, false).AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");
            Transition(stWalk, stIdle, 0.08f, false).AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            Transition(stIdle, stCrouch, 0.08f, false).AddCondition(AnimatorConditionMode.If, 0f, "Crouch");
            Transition(stWalk, stCrouch, 0.08f, false).AddCondition(AnimatorConditionMode.If, 0f, "Crouch");
            Transition(stCrouch, stIdle, 0.08f, false,
                (AnimatorConditionMode.IfNot, 0f, "Crouch"),
                (AnimatorConditionMode.Less, 0.15f, "Speed"));
            Transition(stCrouch, stWalk, 0.08f, false,
                (AnimatorConditionMode.IfNot, 0f, "Crouch"),
                (AnimatorConditionMode.Greater, 0.15f, "Speed"));

            // ---- Salto y caída
            Transition(stIdle, stJump, 0.05f, false).AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            Transition(stWalk, stJump, 0.05f, false).AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            Transition(stCrouch, stJump, 0.05f, false).AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            Transition(stFall, stJump, 0.05f, false).AddCondition(AnimatorConditionMode.Greater, 0.5f, "YSpeed");

            Transition(stJump, stFall, 0.05f, false).AddCondition(AnimatorConditionMode.Less, 0.1f, "YSpeed");
            Transition(stJump, stIdle, 0.06f, false,
                (AnimatorConditionMode.If, 0f, "Grounded"),
                (AnimatorConditionMode.Less, 0.15f, "Speed"));
            Transition(stJump, stWalk, 0.06f, false,
                (AnimatorConditionMode.If, 0f, "Grounded"),
                (AnimatorConditionMode.Greater, 0.15f, "Speed"));
            Transition(stFall, stIdle, 0.06f, false,
                (AnimatorConditionMode.If, 0f, "Grounded"),
                (AnimatorConditionMode.Less, 0.15f, "Speed"));
            Transition(stFall, stWalk, 0.06f, false,
                (AnimatorConditionMode.If, 0f, "Grounded"),
                (AnimatorConditionMode.Greater, 0.15f, "Speed"));

            // ---- Acciones (disparo, bomba, daño)
            var anyShoot = sm.AddAnyStateTransition(stShoot);
            anyShoot.hasExitTime = false;
            anyShoot.duration = 0.04f;
            anyShoot.canTransitionToSelf = false;
            anyShoot.AddCondition(AnimatorConditionMode.If, 0f, "Shoot");

            var anyThrow = sm.AddAnyStateTransition(stThrow);
            anyThrow.hasExitTime = false;
            anyThrow.duration = 0.04f;
            anyThrow.canTransitionToSelf = false;
            anyThrow.AddCondition(AnimatorConditionMode.If, 0f, "Throw");

            var anyHurt = sm.AddAnyStateTransition(stHurt);
            anyHurt.hasExitTime = false;
            anyHurt.duration = 0.04f;
            anyHurt.canTransitionToSelf = false;
            anyHurt.AddCondition(AnimatorConditionMode.If, 0f, "Hurt");

            var anyDeath = sm.AddAnyStateTransition(stDeath);
            anyDeath.hasExitTime = false;
            anyDeath.duration = 0.06f;
            anyDeath.canTransitionToSelf = false;
            anyDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");

            // ---- Vuelta desde acciones
            Transition(stShoot, stIdle, 0.04f, true);
            Transition(stThrow, stIdle, 0.04f, true);
            Transition(stHurt, stIdle, 0.06f, true);

            EditorUtility.SetDirty(controller);
        }

        // ------------------------------------------------------------------
        private static void BuildFX()
        {
            EnsureFolder(AnimRoot + "FX/");

            string fx = "Assets/OperacionCuatro/Sprites/FX/explosion_";
            var explosion = Clip(AnimRoot + "FX/Explosion.anim", 18f, false,
                Load(fx + "0"), Load(fx + "1"), Load(fx + "2"), Load(fx + "3"));

            string ctrlPath = CtrlRoot + "ExplosionController.controller";
            AssetDatabase.DeleteAsset(ctrlPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            EnsureLayerWeight(controller);

            var sm = controller.layers[0].stateMachine;
            var stExplosion = sm.AddState("Explosion", new Vector3(260f, 0f, 0f));
            stExplosion.motion = explosion;
            sm.defaultState = stExplosion;

            EditorUtility.SetDirty(controller);
        }

        // ------------------------------------------------------------------
        private static void BuildRaider()
        {
            string s = SpriteEnemies + "Raider/Raider_";
            string dir = AnimRoot + "Raider/";
            EnsureFolder(dir);

            var idle = Clip(dir + "Raider_idle.anim", 6f, true, Load(s + "idle_0"));
            var walk = Clip(dir + "Raider_walk.anim", 9f, true,
                Load(s + "walk_0"), Load(s + "walk_1"), Load(s + "walk_2"), Load(s + "walk_3"));
            var hurt = Clip(dir + "Raider_hurt.anim", 10f, false, Load(s + "hurt"));
            var death = Clip(dir + "Raider_death.anim", 8f, false, Load(s + "death"));

            BuildEnemyController("Raider", idle, walk, null, hurt, death);
        }

        private static void BuildGunner()
        {
            string s = SpriteEnemies + "Gunner/Gunner_";
            string dir = AnimRoot + "Gunner/";
            EnsureFolder(dir);

            var idle = Clip(dir + "Gunner_idle.anim", 6f, true, Load(s + "idle_0"));
            var walk = Clip(dir + "Gunner_walk.anim", 7f, true, Load(s + "walk_0"), Load(s + "walk_1"));
            var shoot = Clip(dir + "Gunner_shoot.anim", 12f, false, Load(s + "shoot"), Load(s + "idle_0"));
            var hurt = Clip(dir + "Gunner_hurt.anim", 10f, false, Load(s + "hurt"));
            var death = Clip(dir + "Gunner_death.anim", 8f, false, Load(s + "death"));

            BuildEnemyController("Gunner", idle, walk, shoot, hurt, death);
        }

        private static void BuildEnemyController(string name, AnimationClip idle, AnimationClip walk, AnimationClip shoot, AnimationClip hurt, AnimationClip death)
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

            var stWalk = sm.AddState("Walk", new Vector3(260f, 100f, 0f));
            stWalk.motion = walk;

            var stHurt = sm.AddState("Hurt", new Vector3(520f, -100f, 0f));
            stHurt.motion = hurt;

            var stDeath = sm.AddState("Death", new Vector3(760f, -100f, 0f));
            stDeath.motion = death;

            sm.defaultState = stIdle;

            Transition(stIdle, stWalk, 0.08f, false).AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");
            Transition(stWalk, stIdle, 0.08f, false).AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            if (shoot != null)
            {
                var stShoot = sm.AddState("Shoot", new Vector3(520f, 0f, 0f));
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

        // ------------------------------------------------------------------
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
