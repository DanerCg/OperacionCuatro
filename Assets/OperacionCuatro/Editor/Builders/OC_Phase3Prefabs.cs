using OperacionCuatro;
using UnityEditor;
using UnityEngine;

namespace OCBuilders
{
    /// <summary>
    /// Fase 3 — prefabs de los tres enemigos nuevos, los proyectiles láser, la granada enemiga
    /// y el recogible de arma láser. No modifica ningún prefab de las fases anteriores.
    /// </summary>
    public static class OC_Phase3Prefabs
    {
        private const string PrefabRoot = "Assets/OperacionCuatro/Prefabs/";
        private const string SpriteRoot = "Assets/OperacionCuatro/Sprites/";
        private const string CtrlRoot = "Assets/OperacionCuatro/Animations/Controllers/";
        private const string EnemyRoot = "Assets/OperacionCuatro/Resources/Enemies/";
        private const string ItemRoot = "Assets/OperacionCuatro/Prefabs/Level03/";

        public static void Build()
        {
            EnsureFolder(ItemRoot);
            EnsureFolder(EnemyRoot);

            var impact = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Combat/Impact.prefab");
            var explosion = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Combat/Explosion.prefab");
            var hitSpark = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Combat/HitSpark.prefab");

            var laserPlayer = BuildBullet("BulletLaser", "FX/bullet_laser.png", Faction.Player, "PlayerBullet",
                26, 34f, LayerMask.GetMask("Ground", "Enemy"), impact);

            var laserEnemy = BuildBullet("BulletLaserEnemy", "FX/bullet_laser_enemy.png", Faction.Enemy, "EnemyBullet",
                14, 28f, LayerMask.GetMask("Ground", "Player"), impact);

            BuildGrenade(explosion);
            BuildSentinel(laserEnemy, hitSpark, explosion);
            BuildLaserDrone(laserEnemy, hitSpark, explosion);
            BuildGrenadier(hitSpark, explosion);
            BuildLaserPickup(impact);
            WireLaserIntoPlayers();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[OC] Prefabs de Fase 3 creados: Sentinel, LaserDrone, Grenadier, Grenade, BulletLaser, " +
                      "BulletLaserEnemy y LaserWeaponPickup. (player=" + (laserPlayer != null) +
                      " enemy=" + (laserEnemy != null) + ")");
        }

        // ==================================================================
        //  Arma láser del jugador (enlace en los cuatro prefabs de personaje)
        // ==================================================================
        /// <summary>
        /// El arma láser es una mejora del jugador: los cuatro prefabs de personaje deben apuntar
        /// al proyectil nuevo. Sin esta asignación <c>HasLaser</c> nunca llega a disparar y el
        /// recogible quedaría inerte.
        /// </summary>
        private static void WireLaserIntoPlayers()
        {
            var bolt = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Combat/BulletLaser.prefab");
            if (bolt == null)
            {
                Debug.LogError("[OC] Falta Combat/BulletLaser.prefab: el arma láser no dispararía.");
                return;
            }

            var projectile = bolt.GetComponent<Projectile>();
            string[] names = { "Cesar", "Danner", "Ines", "Sebastian" };
            int wired = 0;

            for (int i = 0; i < names.Length; i++)
            {
                string path = "Assets/OperacionCuatro/Resources/Characters/Player_" + names[i] + ".prefab";
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;

                var shooter = go.GetComponent<PlayerShooter>();
                if (shooter == null) continue;

                var so = new SerializedObject(shooter);
                var prop = so.FindProperty("laserBoltPrefab");
                if (prop == null) continue;

                prop.objectReferenceValue = projectile;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(shooter);
                wired++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[OC] Arma láser enlazada en " + wired + "/4 prefabs de personaje.");
        }

        // ==================================================================
        private static Projectile BuildBullet(string name, string spriteRel, Faction faction, string layerName,
            int damage, float speed, LayerMask hitMask, GameObject impact)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer(layerName);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(spriteRel);
            sr.sortingLayerName = "Projectiles";
            sr.sortingOrder = 7;

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.mass = 0.05f;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = 0.15f;
            circle.isTrigger = true;

            var proj = go.AddComponent<Projectile>();
            proj.damage = damage;
            proj.speed = speed;
            proj.lifetime = 2.2f;
            proj.faction = faction;
            proj.hitMask = hitMask;
            proj.impactPrefab = impact;

            var prefab = Save(go, PrefabRoot + "Combat/" + name + ".prefab");
            return prefab.GetComponent<Projectile>();
        }

        // ==================================================================
        //  Granada enemiga (aviso previo + explosión en área)
        // ==================================================================
        private static void BuildGrenade(GameObject explosion)
        {
            var go = new GameObject("Grenade");
            go.layer = LayerMask.NameToLayer("EnemyBullet");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("FX/bomb.png");
            sr.sortingLayerName = "Projectiles";
            sr.sortingOrder = 8;
            sr.color = new Color(1f, 0.78f, 0.66f, 1f);
            go.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            var body = go.AddComponent<Rigidbody2D>();
            body.mass = 0.4f;
            body.gravityScale = 1f;
            body.freezeRotation = false;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = 0.22f;
            circle.isTrigger = false;

            var bomb = go.AddComponent<Bomb>();
            bomb.fuseTime = 1.35f;
            bomb.damage = 30;
            bomb.radius = 2.2f;
            bomb.explosionPrefab = explosion;
            bomb.damageMask = LayerMask.GetMask("Player");
            bomb.faction = Faction.Enemy;
            bomb.ownerPlayerIndex = -1;

            var warning = go.AddComponent<GrenadeWarning>();
            warning.bomb = bomb;
            warning.body = sr;
            warning.blinkSpeed = 9f;

            Save(go, ItemRoot + "Grenade.prefab");
        }

        // ==================================================================
        //  ENEMIGO 1 — Robot centinela
        // ==================================================================
        private static void BuildSentinel(Projectile bullet, GameObject hitSpark, GameObject explosion)
        {
            var go = new GameObject("Sentinel");
            go.layer = LayerMask.NameToLayer("Enemy");
            go.tag = "Enemy";

            var body = go.AddComponent<Rigidbody2D>();
            body.mass = 1.8f;
            body.gravityScale = 1f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.95f, 1.75f);
            box.offset = new Vector2(0f, 0.875f);

            var health = go.AddComponent<Health>();
            health.maxHealth = 75;
            health.faction = Faction.Enemy;

            var idle = LoadSprite("Enemies/Sentinel/Sentinel_idle_0.png");
            var visual = NewVisual(go.transform, idle, 1.8f, 9);

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(go.transform, false);
            muzzle.transform.localPosition = new Vector3(0.72f, 1.1f, 0f);

            var animator = AddAnimator(go, "SentinelController");

            var sentinel = go.AddComponent<SentinelRobot>();
            sentinel.health = health;
            sentinel.animator = animator;
            sentinel.spriteRenderer = visual.GetComponent<SpriteRenderer>();
            sentinel.groundMask = LayerMask.GetMask("Ground");
            sentinel.bulletPrefab = bullet;
            sentinel.muzzle = muzzle.transform;
            sentinel.deathEffectPrefab = explosion;
            sentinel.maxHealth = 75;
            sentinel.scoreValue = 350;
            sentinel.contactDamage = 16;
            sentinel.moveSpeed = 1.35f;
            sentinel.detectionRange = 16f;
            sentinel.stopRange = 7f;
            sentinel.burstSize = 3;
            sentinel.burstInterval = 0.14f;
            sentinel.fireCooldown = 2f;
            sentinel.bulletDamage = 13;
            sentinel.patrolRange = 5f;

            AddContactTrigger(go, 16, 0.66f, 0.9f);

            Save(go, EnemyRoot + "Sentinel.prefab");
        }

        // ==================================================================
        //  ENEMIGO 2 — Dron láser
        // ==================================================================
        private static void BuildLaserDrone(Projectile bullet, GameObject hitSpark, GameObject explosion)
        {
            var go = new GameObject("LaserDrone");
            go.layer = LayerMask.NameToLayer("Enemy");
            go.tag = "Enemy";

            var body = go.AddComponent<Rigidbody2D>();
            body.mass = 0.5f;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = 0.56f;

            var health = go.AddComponent<Health>();
            health.maxHealth = 45;
            health.faction = Faction.Enemy;

            var idle = LoadSprite("Enemies/LaserDrone/LaserDrone_idle_0.png");
            var visual = NewVisual(go.transform, idle, 1.05f, 9);

            // Aviso luminoso previo al disparo (hijo de la raíz, no se voltea con el sprite).
            var charge = new GameObject("ChargeVisual");
            charge.transform.SetParent(go.transform, false);
            charge.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            var chargeSr = charge.AddComponent<SpriteRenderer>();
            chargeSr.sprite = LoadSprite("FX/laser_charge.png");
            chargeSr.sortingLayerName = "Effects";
            chargeSr.sortingOrder = 9;
            charge.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            charge.SetActive(false);

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(go.transform, false);
            muzzle.transform.localPosition = new Vector3(0.6f, -0.14f, 0f);

            var animator = AddAnimator(go, "LaserDroneController");

            var drone = go.AddComponent<LaserDrone>();
            drone.health = health;
            drone.animator = animator;
            drone.spriteRenderer = visual.GetComponent<SpriteRenderer>();
            drone.groundMask = LayerMask.GetMask("Ground");
            drone.laserPrefab = bullet;
            drone.muzzle = muzzle.transform;
            drone.deathEffectPrefab = explosion;
            drone.deathBlastPrefab = explosion;
            drone.chargeVisual = charge;
            drone.maxHealth = 45;
            drone.scoreValue = 300;
            drone.contactDamage = 12;
            drone.flySpeed = 2.2f;
            drone.hoverAmplitude = 0.5f;
            drone.hoverSpeed = 1.6f;
            drone.detectionRange = 15f;
            drone.fireCooldown = 2.6f;
            drone.chargeTime = 0.65f;
            drone.laserDamage = 14;
            drone.laserSpeed = 28f;
            drone.patrolRange = 6f;

            AddContactTrigger(go, 12, 0.52f, 0f);

            Save(go, EnemyRoot + "LaserDrone.prefab");
        }

        // ==================================================================
        //  ENEMIGO 3 — Soldado explosivo
        // ==================================================================
        private static void BuildGrenadier(GameObject hitSpark, GameObject explosion)
        {
            var go = new GameObject("Grenadier");
            go.layer = LayerMask.NameToLayer("Enemy");
            go.tag = "Enemy";

            var body = go.AddComponent<Rigidbody2D>();
            body.mass = 1f;
            body.gravityScale = 1f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 1.66f);
            box.offset = new Vector2(0f, 0.83f);

            var health = go.AddComponent<Health>();
            health.maxHealth = 40;
            health.faction = Faction.Enemy;

            var idle = LoadSprite("Enemies/Grenadier/Grenadier_idle_0.png");
            var visual = NewVisual(go.transform, idle, 1.7f, 9);

            var throwPoint = new GameObject("ThrowPoint");
            throwPoint.transform.SetParent(go.transform, false);
            throwPoint.transform.localPosition = new Vector3(0.34f, 1.36f, 0f);

            var animator = AddAnimator(go, "GrenadierController");

            var grenade = AssetDatabase.LoadAssetAtPath<Bomb>(ItemRoot + "Grenade.prefab");

            var soldier = go.AddComponent<GrenadierEnemy>();
            soldier.health = health;
            soldier.animator = animator;
            soldier.spriteRenderer = visual.GetComponent<SpriteRenderer>();
            soldier.groundMask = LayerMask.GetMask("Ground");
            soldier.grenadePrefab = grenade;
            soldier.throwPoint = throwPoint.transform;
            soldier.deathEffectPrefab = explosion;
            soldier.maxHealth = 40;
            soldier.scoreValue = 250;
            soldier.contactDamage = 12;
            soldier.moveSpeed = 1.6f;
            soldier.detectionRange = 17f;
            soldier.minRange = 5.5f;
            soldier.maxRange = 12f;
            soldier.throwCooldown = 3.2f;
            soldier.throwVertical = 11f;
            soldier.maxThrowSpeed = 13f;
            soldier.coverTime = 0.9f;

            AddContactTrigger(go, 12, 0.58f, 0.85f);

            Save(go, EnemyRoot + "Grenadier.prefab");
        }

        // ==================================================================
        //  Recogible de arma láser
        // ==================================================================
        private static void BuildLaserPickup(GameObject impact)
        {
            var go = new GameObject("LaserWeaponPickup");
            go.layer = LayerMask.NameToLayer("TriggerZone");

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = 0.62f;
            circle.isTrigger = true;

            var icon = new GameObject("Icon");
            icon.transform.SetParent(go.transform, false);
            var sr = icon.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("FX/laser_weapon.png");
            sr.sortingLayerName = "Environment";
            sr.sortingOrder = 6;
            icon.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var pickup = go.AddComponent<LaserWeaponPickup>();
            pickup.ammo = 40;
            pickup.iconRoot = icon;
            pickup.pickupEffectPrefab = impact;

            Save(go, ItemRoot + "LaserWeaponPickup.prefab");
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
