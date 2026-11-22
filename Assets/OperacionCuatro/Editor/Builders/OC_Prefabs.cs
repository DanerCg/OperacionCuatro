using OperacionCuatro;
using UnityEditor;
using UnityEngine;

namespace OCBuilders
{
    /// <summary>Módulo 4: prefabs de combate, personajes jugables y enemigos.</summary>
    public static class OC_Prefabs
    {
        private const string PrefabRoot = "Assets/OperacionCuatro/Prefabs/";
        private const string SpriteRoot = "Assets/OperacionCuatro/Sprites/";
        private const string CtrlRoot = "Assets/OperacionCuatro/Animations/Controllers/";
        private const string CharRoot = "Assets/OperacionCuatro/Resources/Characters/";
        private const string EnemyRoot = "Assets/OperacionCuatro/Resources/Enemies/";

        private const float PlayerSpriteHeight = 1.78f;
        private const float EnemySpriteHeight = 1.58f;

        public static void Build()
        {
            EnsureFolder(PrefabRoot + "Combat/");
            EnsureFolder(CharRoot);
            EnsureFolder(EnemyRoot);

            // ---- Efectos y munición (primero: el resto los referencia) ----
            var impact = BuildSimple("Impact", "FX/impact.png", "Effects", 1.3f, 0.35f);
            var muzzle = BuildSimple("MuzzleFlash", "FX/muzzle_flash.png", "Effects", 1f, 0.09f);
            var explosion = BuildExplosion();

            var bulletPlayer = BuildBullet("BulletPlayer", "FX/bullet_player.png", Faction.Player, 22f, "PlayerBullet", impact);
            var bulletEnemy = BuildBullet("BulletEnemy", "FX/bullet_enemy.png", Faction.Enemy, 14f, "EnemyBullet", impact);
            BuildBomb(explosion);

            // ---- Personajes jugables ----
            string[] players = { "Danner", "Cesar", "Sebastian", "Ines" };
            for (int i = 0; i < players.Length; i++) BuildPlayer(players[i], bulletPlayer, bulletEnemy);

            // ---- Enemigos ----
            BuildRaider(bulletEnemy);
            BuildGunner(bulletEnemy);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[OC] Prefabs creados: combate, 4 jugadores y 2 enemigos.");
        }

        // ==================================================================
        //  FX simple (sólo sprite, se autodestruye desde el script que lo invoca)
        // ==================================================================
        private static GameObject BuildSimple(string name, string spriteRel, string sortingLayer, float scale, float unused)
        {
            var go = new GameObject(name);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(spriteRel);
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = 10;

            go.transform.localScale = new Vector3(scale, scale, 1f);

            return Save(go, PrefabRoot + "Combat/" + name + ".prefab");
        }

        private static GameObject BuildExplosion()
        {
            var go = new GameObject("Explosion");

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("FX/explosion_0.png");
            sr.sortingLayerName = "Effects";
            sr.sortingOrder = 12;
            visual.transform.localScale = new Vector3(1.9f, 1.9f, 1f);

            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                CtrlRoot + "ExplosionController.controller");
            anim.applyRootMotion = false;

            var fx = go.AddComponent<ExplosionVisual>();
            fx.lifetime = 0.45f;
            fx.shakeStrength = 0.28f;

            return Save(go, PrefabRoot + "Combat/Explosion.prefab");
        }

        // ==================================================================
        //  Balas
        // ==================================================================
        private static Projectile BuildBullet(string name, string spriteRel, Faction faction, float speed, string layerName, GameObject impact)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer(layerName);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(spriteRel);
            sr.sortingLayerName = "Projectiles";
            sr.sortingOrder = 5;

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
            proj.damage = 12;
            proj.speed = speed;
            proj.lifetime = 2.4f;
            proj.faction = faction;
            proj.hitMask = LayerMask.GetMask("Ground", faction == Faction.Player ? "Enemy" : "Player");
            proj.impactPrefab = impact;

            var prefab = Save(go, PrefabRoot + "Combat/" + name + ".prefab");
            return prefab.GetComponent<Projectile>();
        }

        // ==================================================================
        //  Granada
        // ==================================================================
        private static Bomb BuildBomb(GameObject explosion)
        {
            var go = new GameObject("Bomb");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("FX/bomb.png");
            sr.sortingLayerName = "Projectiles";
            sr.sortingOrder = 6;

            var body = go.AddComponent<Rigidbody2D>();
            body.mass = 1.2f;
            body.gravityScale = 2.2f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var circle = go.AddComponent<CircleCollider2D>();
            circle.radius = 0.15f;

            var bomb = go.AddComponent<Bomb>();
            bomb.fuseTime = 1.6f;
            bomb.damage = 45;
            bomb.radius = 2.6f;
            bomb.explosionPrefab = explosion;
            bomb.damageMask = LayerMask.GetMask("Enemy", "Ground");

            var prefab = Save(go, PrefabRoot + "Combat/Bomb.prefab");
            return prefab.GetComponent<Bomb>();
        }

        // ==================================================================
        //  Jugador
        // ==================================================================
        private static void BuildPlayer(string name, Projectile bullet, Projectile enemyBullet)
        {
            string spritePrefix = "Characters/" + name + "/" + name + "_";
            var idleSprite = LoadSprite(spritePrefix + "idle_0.png");
            if (idleSprite == null) return;

            var go = new GameObject("Player_" + name);
            go.layer = LayerMask.NameToLayer("Player");
            go.tag = "Player";

            var body = go.AddComponent<Rigidbody2D>();
            body.mass = 1f;
            body.gravityScale = 1f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.72f, 1.72f);
            box.offset = new Vector2(0f, 0.86f);

            var health = go.AddComponent<Health>();
            health.maxHealth = 100;
            health.faction = Faction.Player;

            // ---- Visual animado ----
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = idleSprite;
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = 10;

            float scale = PlayerSpriteHeight / Mathf.Max(0.01f, idleSprite.bounds.size.y);
            visual.transform.localScale = new Vector3(scale, scale, 1f);

            // ---- Punto de disparo ----
            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(go.transform, false);
            muzzle.transform.localPosition = new Vector3(0.62f, 1.06f, 0f);

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                CtrlRoot + name + "Controller.controller");
            animator.applyRootMotion = false;

            var controller = go.AddComponent<PlayerController2D>();
            controller.visual = visual.transform;
            controller.spriteRenderer = sr;
            controller.animator = animator;
            controller.health = health;
            controller.groundMask = LayerMask.GetMask("Ground");

            var shooter = go.AddComponent<PlayerShooter>();
            shooter.bulletPrefab = bullet;
            shooter.bombPrefab = AssetDatabase.LoadAssetAtPath<Bomb>(PrefabRoot + "Combat/Bomb.prefab");
            shooter.muzzleFlashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Combat/MuzzleFlash.prefab");
            shooter.muzzle = muzzle.transform;
            shooter.animator = animator;
            shooter.hitMask = LayerMask.GetMask("Ground", "Enemy");
            shooter.fireRate = 0.15f;
            shooter.bulletDamage = 12;
            shooter.bulletSpeed = 22f;
            shooter.maxBombs = 3;

            controller.shooter = shooter;

            Save(go, CharRoot + "Player_" + name + ".prefab");
        }

        // ==================================================================
        //  Enemigos
        // ==================================================================
        private static void BuildRaider(Projectile enemyBullet)
        {
            var go = BuildEnemyHost("Raider", 1.6f, out var visual, out var sr, out var health, out var animator);

            var patrol = go.AddComponent<PatrolEnemy>();
            patrol.health = health;
            patrol.animator = animator;
            patrol.spriteRenderer = sr;
            patrol.groundMask = LayerMask.GetMask("Ground");
            patrol.speed = 2.1f;
            patrol.maxHealth = 30;
            patrol.scoreValue = 100;

            AddContactTrigger(go, 15);

            Save(go, EnemyRoot + "Raider.prefab");
        }

        private static void BuildGunner(Projectile enemyBullet)
        {
            var go = BuildEnemyHost("Gunner", 1.6f, out var visual, out var sr, out var health, out var animator);

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(go.transform, false);
            muzzle.transform.localPosition = new Vector3(0.72f, 0.98f, 0f);

            var shooter = go.AddComponent<ShooterEnemy>();
            shooter.health = health;
            shooter.animator = animator;
            shooter.spriteRenderer = sr;
            shooter.groundMask = LayerMask.GetMask("Ground");
            shooter.bulletPrefab = enemyBullet;
            shooter.muzzle = muzzle.transform;
            shooter.maxHealth = 40;
            shooter.scoreValue = 150;
            shooter.detectionRange = 16f;
            shooter.minRange = 3.5f;
            shooter.fireCooldown = 1.7f;
            shooter.patrolSpeed = 1.1f;

            AddContactTrigger(go, 12);

            Save(go, EnemyRoot + "Gunner.prefab");
        }

        private static GameObject BuildEnemyHost(string name, float height, out Transform visual, out SpriteRenderer sr,
            out Health health, out Animator animator)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("Enemy");
            go.tag = "Enemy";

            var body = go.AddComponent<Rigidbody2D>();
            body.mass = 1f;
            body.gravityScale = 1f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.86f, 1.5f);
            box.offset = new Vector2(0f, 0.78f);

            health = go.AddComponent<Health>();
            health.maxHealth = 30;
            health.faction = Faction.Enemy;

            var idleSprite = LoadSprite("Enemies/" + name + "/" + name + "_idle_0.png");

            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(go.transform, false);
            sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = idleSprite;
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = 8;

            float scale = height / Mathf.Max(0.01f, idleSprite != null ? idleSprite.bounds.size.y : 1f);
            visualGo.transform.localScale = new Vector3(scale, scale, 1f);

            visual = visualGo.transform;

            animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                CtrlRoot + name + "Controller.controller");
            animator.applyRootMotion = false;

            return go;
        }

        private static void AddContactTrigger(GameObject host, int damage)
        {
            var hit = new GameObject("HitBox");
            hit.transform.SetParent(host.transform, false);
            hit.transform.localPosition = new Vector3(0f, 0.75f, 0f);

            var circle = hit.AddComponent<CircleCollider2D>();
            circle.radius = 0.62f;
            circle.isTrigger = true;

            var touch = hit.AddComponent<DamageOnTouch>();
            touch.damage = damage;
            touch.faction = Faction.Enemy;
            touch.repeatDelay = 0.9f;
        }

        // ==================================================================
        //  Utilidades
        // ==================================================================
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
