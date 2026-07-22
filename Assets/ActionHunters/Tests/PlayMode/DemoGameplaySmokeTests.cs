using System.Collections;
using System.Linq;
using ActionHunters.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ActionHunters.Tests
{
    public sealed class DemoGameplaySmokeTests
    {
        [UnityTest]
        public IEnumerator MainScene_TutorialMovementCollisionAnimationAndVfxAreWired()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var match = Object.FindFirstObjectByType<DemoMatchController>();
            var tutorial = Object.FindFirstObjectByType<DemoTutorialDirector>();
            Assert.That(match, Is.Not.Null);
            Assert.That(tutorial, Is.Not.Null);
            Assert.That(match.RulesVisible, Is.True);

            var combatants = Object.FindObjectsByType<DemoCombatant>(FindObjectsSortMode.None);
            Assert.That(combatants, Has.Length.EqualTo(12));
            Assert.That(combatants.All(actor => actor.GetComponent<CharacterController>() != null), Is.True);
            Assert.That(combatants.Count(actor => actor.GetComponentInChildren<Animator>(true)?.runtimeAnimatorController != null), Is.EqualTo(8));

            tutorial.DismissRules();
            var timeout = 4f;
            while (match.State != DemoMatchState.Playing && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.That(match.State, Is.EqualTo(DemoMatchState.Playing));
            match.enabled = false;

            var controlled = match.ControlledHunter;
            Assert.That(controlled, Is.Not.Null);
            var controller = controlled.GetComponent<CharacterController>();
            controller.enabled = false;
            controlled.transform.position = new Vector3(-2f, 0.92f, 0f);
            controller.enabled = true;
            yield return null;

            var start = controlled.transform.position;
            for (var frame = 0; frame < 90; frame++)
            {
                controlled.Move(Vector2.right, Time.deltaTime);
                yield return null;
            }

            Assert.That(controlled.transform.position.x, Is.GreaterThan(start.x + 0.25f));
            Assert.That(controlled.transform.position.x, Is.LessThan(-0.72f), "The central objective core should stop collision-aware movement.");

            var monster = combatants.First(actor => actor.Team == DemoTeam.Neutral && actor.Role == DemoRole.Monster);
            var monsterController = monster.GetComponent<CharacterController>();
            monsterController.enabled = false;
            monster.transform.position = new Vector3(0.9f, controlled.transform.position.y, 0f);
            monsterController.enabled = true;
            var previousHealth = monster.Health;
            Assert.That(controlled.TryBasicAttack(monster), Is.False, "The central objective should block attacks through its collider.");
            Assert.That(monster.Health, Is.EqualTo(previousHealth));

            monsterController.enabled = false;
            monster.transform.position = controlled.transform.position + Vector3.back * 1.6f;
            monsterController.enabled = true;
            Assert.That(controlled.TryBasicAttack(monster), Is.True);
            yield return null;

            Assert.That(monster.Health, Is.LessThan(previousHealth));
            var pooledEffects = match.EffectPool.GetComponentsInChildren<DemoPooledEffect>(true);
            Assert.That(pooledEffects, Has.Length.GreaterThanOrEqualTo(15));
            Assert.That(pooledEffects.Any(effect => effect.gameObject.activeSelf), Is.True);
            Assert.That(GameObject.Find("World_Health_Bar"), Is.Not.Null);
            Assert.That(GameObject.Find("Controlled_Hunter_Marker"), Is.Not.Null);

            var effectRepresentatives = pooledEffects
                .GroupBy(effect => effect.transform.parent)
                .Select(group => group.First())
                .ToArray();
            Assert.That(effectRepresentatives, Has.Length.EqualTo(3));
            foreach (var effect in effectRepresentatives)
            {
                var particles = effect.GetComponentsInChildren<ParticleSystem>(true);
                Assert.That(particles, Is.Not.Empty);
                Assert.That(particles.All(particle => particle.main.stopAction == ParticleSystemStopAction.None), Is.True);

                effect.Play(Vector3.zero, Quaternion.identity);
                var effectTimeout = 8f;
                while (effect.gameObject.activeSelf && effectTimeout > 0f)
                {
                    effectTimeout -= Time.deltaTime;
                    yield return null;
                }

                Assert.That(effect.gameObject.activeSelf, Is.False, $"{effect.name} should return to its pool.");
                Assert.That(effect.GetComponentsInChildren<ParticleSystem>(true), Has.Length.EqualTo(particles.Length));
                effect.Play(Vector3.zero, Quaternion.identity);
                yield return null;
                Assert.That(effect.gameObject.activeSelf, Is.True, $"{effect.name} should be reusable after its first lifetime.");

                effectTimeout = 8f;
                while (effect.gameObject.activeSelf && effectTimeout > 0f)
                {
                    effectTimeout -= Time.deltaTime;
                    yield return null;
                }

                Assert.That(effect.gameObject.activeSelf, Is.False, $"{effect.name} should also finish its second lifetime.");
            }

        }
    }
}
