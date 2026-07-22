using System;
using System.Collections.Generic;
using UnityEngine;

namespace ActionHunters.Runtime
{
    public enum DemoTeam
    {
        Neutral,
        Blue,
        Red
    }

    public enum DemoRole
    {
        Guardian,
        Ranger,
        Medic,
        Striker,
        Monster,
        Boss
    }

    public enum DemoMatchState
    {
        Countdown,
        Playing,
        SuddenDeath,
        Result
    }

    [Serializable]
    public struct DemoRoleStats
    {
        public DemoRole role;
        [Min(1f)] public float maxHealth;
        [Min(0f)] public float moveSpeed;
        [Min(0f)] public float attackDamage;
        [Min(0.1f)] public float attackRange;
        [Min(0.05f)] public float attackCooldown;
        [Min(0.1f)] public float detectionRange;
        [Min(0.1f)] public float skillCooldown;
    }

    [CreateAssetMenu(fileName = "DemoGameConfig", menuName = "Action Hunters/Demo Game Config")]
    public sealed class DemoGameConfig : ScriptableObject
    {
        [Header("Match")]
        [SerializeField, Min(10f)] private float matchDuration = 300f;
        [SerializeField, Min(0f)] private float countdownDuration = 3f;
        [SerializeField, Min(5f)] private float suddenDeathDuration = 60f;
        [SerializeField, Min(0f)] private float hunterRespawnDelay = 8f;
        [SerializeField, Min(0f)] private float monsterRespawnDelay = 18f;

        [Header("Economy and score")]
        [SerializeField, Min(0)] private int startingGold = 30;
        [SerializeField, Min(0)] private int hireCost = 60;
        [SerializeField, Min(0)] private int monsterGold = 30;
        [SerializeField, Min(0)] private int eliteMonsterGold = 60;
        [SerializeField, Min(0)] private int hunterKillScore = 10;
        [SerializeField, Min(0)] private int bossKillScore = 5;

        [Header("Simulation")]
        [SerializeField, Min(0.05f)] private float aiThinkInterval = 0.25f;
        [SerializeField] private Vector2 arenaExtents = new Vector2(23f, 15f);
        [SerializeField] private List<DemoRoleStats> roleStats = new List<DemoRoleStats>();

        public float MatchDuration => matchDuration;
        public float CountdownDuration => countdownDuration;
        public float SuddenDeathDuration => suddenDeathDuration;
        public float HunterRespawnDelay => hunterRespawnDelay;
        public float MonsterRespawnDelay => monsterRespawnDelay;
        public int StartingGold => startingGold;
        public int HireCost => hireCost;
        public int MonsterGold => monsterGold;
        public int EliteMonsterGold => eliteMonsterGold;
        public int HunterKillScore => hunterKillScore;
        public int BossKillScore => bossKillScore;
        public float AiThinkInterval => aiThinkInterval;
        public Vector2 ArenaExtents => arenaExtents;

        public DemoRoleStats GetStats(DemoRole role)
        {
            for (var index = 0; index < roleStats.Count; index++)
            {
                if (roleStats[index].role == role)
                {
                    return roleStats[index];
                }
            }

            throw new InvalidOperationException($"No demo stats configured for {role}.");
        }

        public void ApplyDemoDefaults()
        {
            matchDuration = 300f;
            countdownDuration = 3f;
            suddenDeathDuration = 60f;
            hunterRespawnDelay = 8f;
            monsterRespawnDelay = 18f;
            startingGold = 30;
            hireCost = 60;
            monsterGold = 30;
            eliteMonsterGold = 60;
            hunterKillScore = 10;
            bossKillScore = 5;
            aiThinkInterval = 0.25f;
            arenaExtents = new Vector2(23f, 15f);
            roleStats = new List<DemoRoleStats>
            {
                Stats(DemoRole.Guardian, 230f, 4.4f, 22f, 2.2f, 0.9f, 9f, 9f),
                Stats(DemoRole.Ranger, 130f, 5.2f, 25f, 8.5f, 0.8f, 12f, 8f),
                Stats(DemoRole.Medic, 145f, 4.9f, 15f, 6.5f, 1.05f, 11f, 10f),
                Stats(DemoRole.Striker, 155f, 6.2f, 34f, 2.1f, 0.72f, 10f, 7f),
                Stats(DemoRole.Monster, 95f, 3.6f, 13f, 1.8f, 1.15f, 8f, 12f),
                Stats(DemoRole.Boss, 360f, 2.8f, 27f, 2.8f, 1.25f, 13f, 10f)
            };
        }

        private static DemoRoleStats Stats(
            DemoRole role,
            float health,
            float speed,
            float damage,
            float range,
            float cooldown,
            float detection,
            float skillCooldown)
        {
            return new DemoRoleStats
            {
                role = role,
                maxHealth = health,
                moveSpeed = speed,
                attackDamage = damage,
                attackRange = range,
                attackCooldown = cooldown,
                detectionRange = detection,
                skillCooldown = skillCooldown
            };
        }
    }
}
