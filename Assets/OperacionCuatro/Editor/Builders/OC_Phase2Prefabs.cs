using OperacionCuatro;
using UnityEditor;
using UnityEngine;

namespace OCBuilders
{
    /// <summary>
    /// Fase 2 — prefabs de los tres enemigos nuevos, la munición propia del complejo y el
    /// generador objetivo. No toca ningún prefab de la Fase 1.
    /// </summary>
    public static class OC_Phase2Prefabs
    {
        private const string PrefabRoot = "Assets/OperacionCuatro/Prefabs/";
        private const string SpriteRoot = "Assets/OperacionCuatro/Sprites/";
        private const string CtrlRoot = "Assets/OperacionCuatro/Animations/Controllers/";
        private const string EnemyRoot = "Assets/OperacionCuatro/Resources/Enemies/";
        private const string ItemRoot = "Assets/OperacionCuatro/Prefabs/Level02/";

        public static void Build()
        {
            EnsureFolder(ItemRoot);
            EnsureFolder(EnemyRoot);

            var impact = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Combat/Impact.prefab");
            var explosion = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Combat/Explosion.prefab");

            var bulletHeavy = BuildBullet("BulletHeavy", "FX/bullet_heavy.png", Faction.Player, "PlayerBullet",
                9, 26f, LayerMask.GetMask("Ground", "Enemy"), impact);
            var bulletTurret = BuildBullet("BulletTurret", "FX/bullet_turret.png", Faction.Enemy, "EnemyBullet",
                12, 12f, LayerMask.GetMask("Ground", "Player"), impact);

            var hitSpark = BuildHitSpark();
            var heavyPickup = BuildHeavyPickup(impact);

            BuildArmored(bulletTurret, hitSpark, explosion);
            BuildDrone(bulletTurret, explosion);
            BuildTurret(bulletTurret, hitSpark, explosion);
            BuildGenerator(explosion);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[OC] Prefabs de Fase 2 creados: Armored, Drone, Turret, BulletHeavy, BulletTurret, " +
                      "HitSpark, HeavyWeaponPickup y Generator. (heavy=" + (bulletHeavy != null) +
                      ", heavyPickup=" + (heavyPickup != null) + ")");
        }

        // ==================================================================
        //  Proyectiles y efectos
        // ==================================================================
        private static Projectile BuildBullet(string name, string spriteRel, Faction faction, string layerName,
            int damage, float speed, LayerMask hitMask, GameObject impact)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer(layerName);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(spriteRel);
            sr.sortingLayerName = "Projectiles";
            sr.sortingOrder = 6;

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.mass = 0.05f;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = 0.16f;
            circle.isTrigger = true;

            var proj = go.AddComponent<Projectile>();
            proj.damage = damage;
            proj.speed = speed;
            proj.lifetime = 2.4f;
            proj.faction = faction;
            proj.hitMask = hitMask;
            proj.impactPrefab = impact;

            var prefab = Save(go, PrefabRoot + "Combat/" + name + ".prefab");
            return prefab.GetComponent<Projectile>();
        }

        private static GameObject BuildHitSpark()
        {
            var go = new GameObject("HitSpark");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("FX/shield_spark.png");
            sr.sortingLayerName = "Effects";
            sr.sortingOrder = 11;
            go.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            return Save(go, PrefabRoot + "Combat/HitSpark.prefab");
        }

        private static GameObject BuildHeavyPickup(GameObject impact)
        {
            var go = new GameObject("HeavyWeaponPickup");
            go.layer = LayerMask.NameToLayer("TriggerZone");

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = 0.62f;
            circle.isTrigger = true;

            var icon = new GameObject("Icon");
            icon.transform.SetParent(go.transform, false);
            var sr = icon.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("FX/heavy_weapon.png");
            sr.sortingLayerName = "Environment";
            sr.sortingOrder = 6;
            icon.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            var pickup = go.AddComponent<HeavyWeaponPickup>();
            pickup.ammo = 150;
            pickup.iconRoot = icon;
            pickup.pickupEffectPrefab = impact;

            return Save(go, ItemRoot + "HeavyWeaponPickup.prefab");
        }

        // ==================================================================
        //  Enemigo A — Soldado blindado
        // ==================================================================
        private static void BuildArmored(Projectile bullet, GameObject hitSpark, GameObject explosion)
        {
            var go = new GameObject("Armored");
            go.layer = LayerMask.NameToLayer("Enemy");
            go.tag = "Enemy";

            var body = go.AddComponent<Rigidbody2D>();
            body.mass = 1.6f;
            body.gravityScale = 1f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.9f, 1.62f);
            box.offset = new Vector2(0f, 0.81f);

            var health = go.AddComponent<Health>();
            health.maxHealth = 60;
            health.faction = Faction.Enemy;

            var idle = LoadSprite("Enemies/Armored/Armored_idle_0.png");
            var visual = NewVisual(go.transform, idle, 1.66f, 9);

            // Placa de escudo frontal (hija de Visual para que se voltee con el personaje).
            var shieldGo = new GameObject("Shield");
            shieldGo.transform.SetParent(visual, false);
            shieldGo.transform.localPosition = new Vector3(0.052f, 0.30f, 0f);
            var shieldSr = shieldGo.AddComponent<SpriteRenderer>();
            shieldSr.sprite = LoadSprite("Props/shield_plate.png");
            shieldSr.sortingLayerName = "Characters";
            shieldSr.sortingOrder = 10;
            shieldGo.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(go.transform, false);
            muzzle.transform.localPosition = new Vector3(0.66f, 1.02f, 0f);

            var animator = AddAnimator(go, "ArmoredController");

            var soldier = go.AddComponent<ArmoredSoldier>();
            soldier.health = health;
            soldier.animator = animator;
            soldier.spriteRenderer = visual.GetComponent<SpriteRenderer>();
            soldier.groundMask = LayerMask.GetMask("Ground");
            soldier.bulletPrefab = bullet;
            soldier.muzzle = muzzle.transform;
            soldier.shieldRenderer = shieldSr;
            soldier.hitSparkPrefab = hitSpark;
            soldier.deathEffectPrefab = explosion;
            soldier.maxHealth = 60;
            soldier.scoreValue = 250;
            soldier.contactDamage = 14;
            soldier.walkSpeed = 1.2f;
            soldier.detectionRange = 15f;
            soldier.stopRange = 6.5f;
            soldier.burstSize = 2;
            soldier.burstInterval = 0.16f;
            soldier.fireCooldown = 2.1f;
            soldier.frontalResistance = 0.25f;
            soldier.vulnerableWindow = 1.3f;

            AddContactTrigger(go, 14, 0.62f, 0.85f);

            Save(go, EnemyRoot + "Armored.prefab");
        }

        // ==================================================================
        //  Enemigo B — Dron militar
        // ==================================================================
        private static void BuildDrone(Projectile bullet, GameObject explosion)
        {
            var go = new GameObject("Drone");
            go.layer = LayerMask.NameToLayer("Enemy");
            go.tag = "Enemy";

            var body = go.AddComponent<Rigidbody2D>();
            body.mass = 0.5f;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = 0.52f;

            var health = go.AddComponent<Health>();
            health.maxHealth = 35;
            health.faction = Faction.Enemy;

            var idle = LoadSprite("Enemies/Drone/Drone_idle_0.png");
            var visual = NewVisual(go.transform, idle, 0.95f, 9);

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(go.transform, false);
            muzzle.transform.localPosition = new Vector3(0.62f, -0.12f, 0f);

            var animator = AddAnimator(go, "DroneController");

            var drone = go.AddComponent<DroneEnemy>();
            drone.health = health;
            drone.animator = animator;
            drone.spriteRenderer = visual.GetComponent<SpriteRenderer>();
            drone.groundMask = LayerMask.GetMask("Ground");
            drone.bulletPrefab = bullet;
            drone.muzzle = muzzle.transform;
            drone.deathEffectPrefab = explosion;
            drone.maxHealth = 35;
            drone.scoreValue = 200;
            drone.contactDamage = 10;
            drone.flySpeed = 2.4f;
            drone.hoverAmplitude = 0.45f;
            drone.hoverSpeed = 1.7f;
            drone.detectionRange = 13f;
            drone.fireCooldown = 1.9f;
            drone.patrolRange = 6f;

            AddContactTrigger(go, 10, 0.5f, 0f);

            Save(go, EnemyRoot + "Drone.prefab");
        }

        // ==================================================================
        //  Enemigo C — Torreta automática
        // ==================================================================
        private static void BuildTurret(Projectile bullet, GameObject hitSpark, GameObject explosion)
        {
            var go = new GameObject("Turret");
            go.layer = LayerMask.NameToLayer("Enemy");
            go.tag = "Enemy";

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.05f, 1.45f);
            box.offset = new Vector2(0f, 0.72f);

            var health = go.AddComponent<Health>();
            health.maxHealth = 50;
            health.faction = Faction.Enemy;

            var idle = LoadSprite("Enemies/Turret/Turret_idle_0.png");
            var visual = NewVisual(go.transform, idle, 1.55f, 8);

            // Cañón giratorio (pivote en la cúpula).
            var barrelGo = new GameObject("Barrel");
            barrelGo.transform.SetParent(go.transform, false);
            barrelGo.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            var barrelSr = barrelGo.AddComponent<SpriteRenderer>();
            barrelSr.sprite = LoadSprite("Enemies/Turret/Turret_barrel.png");
            barrelSr.sortingLayerName = "Characters";
            barrelSr.sortingOrder = 9;
            barrelGo.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(barrelGo.transform, false);
            muzzle.transform.localPosition = new Vector3(1.32f, 0f, 0f);

            var animator = AddAnimator(go, "TurretController");

            var turret = go.AddComponent<TurretEnemy>();
            turret.health = health;
            turret.animator = animator;
            turret.spriteRenderer = visual.GetComponent<SpriteRenderer>();
            turret.groundMask = LayerMask.GetMask("Ground");
            turret.bulletPrefab = bullet;
            turret.muzzle = muzzle.transform;
            turret.barrel = barrelGo.transform;
            turret.hitSparkPrefab = hitSpark;
            turret.deathEffectPrefab = explosion;
            turret.maxHealth = 50;
            turret.scoreValue = 300;
            turret.detectionRange = 14f;
            turret.fieldOfView = 110f;
            turret.burstSize = 3;
            turret.burstInterval = 0.15f;
            turret.fireCooldown = 2.3f;
            turret.bulletDamage = 12;

            Save(go, EnemyRoot + "Turret.prefab");
        }

        // ==================================================================
        //  Objetivo final — Generador central
        // ==================================================================
        private static void BuildGenerator(GameObject explosion)
        {
            var go = new GameObject("Generator");
            go.layer = LayerMask.NameToLayer("Enemy");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Props/generator.png");
            sr.sortingLayerName = "Environment";
            sr.sortingOrder = 7;
            go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.45f, 2.6f);
            box.offset = new Vector2(0f, 1.3f);

            var health = go.AddComponent<Health>();
            health.maxHealth = 280;
            health.faction = Faction.Enemy;

            var shield = new GameObject("Shield");
            shield.transform.SetParent(go.transform, false);
            shield.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            var shieldSr = shield.AddComponent<SpriteRenderer>();
            shieldSr.sprite = LoadSprite("Props/generator_shield.png");
            shieldSr.sortingLayerName = "Effects";
            shieldSr.sortingOrder = 6;
            shield.transform.localScale = new Vector3(2.6f, 2.6f, 1f);

            var objective = go.AddComponent<GeneratorObjective>();
            objective.health = health;
            objective.maxHealth = 280;
            objective.coreVisual = sr;
            objective.shieldVisual = shield;
            objective.explosionPrefab = explosion;
            objective.overloadDuration = 2.6f;
            objective.overloadExplosions = 6;

            Save(go, ItemRoot + "Generator.prefab");
        }

        // ==================================================================
        //  Utilidades
        // ==================================================================
        private static Transform NewVisual(Transform parent, Sprite sprite, float height, int order)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(parent, false);

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = order;

            float scale = height / Mathf.Max(0.01f, sprite != null ? sprite.bounds.size.y : 1f);
            visual.transform.localScale = new Vector3(scale, scale, 1f);

            // El pivote de los sprites de personaje está a los pies: sube el visual hasta el suelo.
            if (sprite != null)
            {
                Vector2 pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
                visual.transform.localPosition = new Vector3(0f, pivot.y * height, 0f);
            }

            return visual.transform;
        }

        private static Animator AddAnimator(GameObject go, string controllerName)
        {
            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                CtrlRoot + controllerName + ".controller");
            animator.applyRootMotion = false;

            if (animator.runtimeAnimatorController == null)
                Debug.LogError("[OC] Controlador de animación no encontrado: " + controllerName);

            return animator;
        }

        private static void AddContactTrigger(GameObject host, int damage, float radius, float y)
        {
            var hit = new GameObject("HitBox");
            hit.transform.SetParent(host.transform, false);
            hit.transform.localPosition = new Vector3(0f, y, 0f);

            var circle = hit.AddComponent<CircleCollider2D>();
            circle.radius = radius;
            circle.isTrigger = true;

            var touch = hit.AddComponent<DamageOnTouch>();
            touch.damage = damage;
            touch.faction = Faction.Enemy;
            touch.repeatDelay = 0.9f;
        }

        private static Sprite LoadSprite(string rel)
        {
            string full = SpriteRoot + rel;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(full);
            if (sprite == null) Debug.LogError("[OC] Sprite no encontrado: " + full);
            return sprite;
        }

        private static GameObject Save(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
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
