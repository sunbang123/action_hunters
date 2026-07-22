using System.Collections;
using System.Linq;
using ActionHunters.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ActionHunters.Tests
{
    public sealed class DemoTraversalAndGimmickTests
    {
        private const float SimulationDelta = 1f / 60f;

        [UnityTest]
        public IEnumerator MainScene_JumpPlatformsAndWorldGimmicksArePlayable()
        {
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var match = Object.FindFirstObjectByType<DemoMatchController>();
            var tutorial = Object.FindFirstObjectByType<DemoTutorialDirector>();
            Assert.That(match, Is.Not.Null);
            Assert.That(tutorial, Is.Not.Null);
            tutorial.DismissRules();

            var timeout = 4f;
            while (match.State != DemoMatchState.Playing && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.That(match.State, Is.EqualTo(DemoMatchState.Playing));
            match.enabled = false;

            var gimmicks = Object.FindObjectsByType<DemoWorldGimmick>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(gimmicks, Has.Length.EqualTo(10));
            Assert.That(gimmicks.Count(item => item.Kind == DemoWorldGimmickKind.WaterShield), Is.EqualTo(1));
            Assert.That(gimmicks.Count(item => item.Kind == DemoWorldGimmickKind.FireProjectile), Is.EqualTo(1));
            Assert.That(gimmicks.Count(item => item.Kind == DemoWorldGimmickKind.WindBeam), Is.EqualTo(1));
            Assert.That(gimmicks.Count(item => item.Kind == DemoWorldGimmickKind.EarthAoe), Is.EqualTo(1));
            Assert.That(gimmicks.Count(item => item.Kind == DemoWorldGimmickKind.JumpPad), Is.EqualTo(2));
            Assert.That(gimmicks.Count(item => item.Kind == DemoWorldGimmickKind.PipeLauncher), Is.EqualTo(2));
            Assert.That(gimmicks.Count(item => item.Kind == DemoWorldGimmickKind.TeamFlag), Is.EqualTo(2));
            Assert.That(gimmicks.All(item => item.gameObject.activeInHierarchy && item.Trigger != null && item.Trigger.enabled && item.Trigger.isTrigger), Is.True);
            Assert.That(gimmicks.All(item => item.HasActivationEffect), Is.True);
            Assert.That(gimmicks.All(item => item.GetComponent<Rigidbody>() is { isKinematic: true, useGravity: false }), Is.True);

            foreach (var pipe in gimmicks.Where(item => item.Kind == DemoWorldGimmickKind.PipeLauncher))
            {
                Assert.That(pipe.GetComponents<BoxCollider>().Any(collider => collider.enabled && !collider.isTrigger), Is.True,
                    $"{pipe.name} needs a solid landing collider in addition to its launcher trigger.");
            }

            foreach (var pad in gimmicks.Where(item => item.Kind == DemoWorldGimmickKind.JumpPad))
            {
                Assert.That(pad.GetComponents<BoxCollider>().Any(collider => collider.enabled && !collider.isTrigger), Is.True,
                    $"{pad.name} needs a solid pad collider.");
            }

            var hunterAnimators = match.BlueHunters
                .Concat(match.RedHunters)
                .Select(actor => actor.GetComponentInChildren<Animator>(true))
                .ToArray();
            Assert.That(hunterAnimators.All(animator => animator != null && animator.parameters.Any(parameter => parameter.name == "Jump")), Is.True);

            var controlled = match.ControlledHunter;
            Assert.That(controlled, Is.Not.Null);
            var controller = controlled.GetComponent<CharacterController>();
            Assert.That(controller, Is.Not.Null);
            var northPlatform = GameObject.Find("Blue_North_Platform");
            Assert.That(northPlatform, Is.Not.Null);
            var northPlatformSolids = northPlatform.GetComponentsInChildren<Collider>(true)
                .Where(collider => collider.enabled && !collider.isTrigger)
                .ToArray();
            Assert.That(northPlatformSolids, Is.Not.Empty, "Blue_North_Platform needs a solid landing collider.");
            var northPlatformTop = northPlatformSolids.Max(collider => collider.bounds.max.y);
            yield return StabilizeOnGround(controlled, new Vector3(-7f, 0.35f, -4f));
            var groundY = controlled.transform.position.y;
            Assert.That(controlled.TryJump(), Is.True);
            Assert.That(controlled.TryJump(), Is.False, "A second airborne jump must not reset vertical velocity.");

            var apex = groundY;
            for (var frame = 0; frame < 220; frame++)
            {
                controlled.Tick(SimulationDelta);
                apex = Mathf.Max(apex, controlled.transform.position.y);
                if (frame > 20 && controlled.IsGrounded && controlled.VerticalVelocity <= 0f)
                {
                    break;
                }

                yield return null;
            }

            Assert.That(apex - groundY, Is.GreaterThan(2.05f), "Normal jump must clear the 1.88m pipe top.");
            Assert.That(controlled.IsGrounded, Is.True);

            yield return StabilizeOnGround(controlled, new Vector3(-10.5f, 0.35f, 7.1f));
            Assert.That(controlled.TryJump(), Is.True);
            var landedOnPlatform = false;
            for (var frame = 0; frame < 170; frame++)
            {
                controlled.Tick(SimulationDelta);
                if (frame < 30)
                {
                    controlled.Move(Vector2.up, SimulationDelta);
                }

                if (frame > 30 && controlled.IsGrounded && controlled.transform.position.y > 1.15f)
                {
                    landedOnPlatform = true;
                    break;
                }

                yield return null;
            }

            Assert.That(
                landedOnPlatform,
                Is.True,
                $"The hunter should jump from the floor onto Blue_North_Platform. Final={controlled.transform.position}, " +
                $"grounded={controlled.IsGrounded}, vertical={controlled.VerticalVelocity:0.00}");
            Assert.That(
                controlled.transform.position.y,
                Is.EqualTo(northPlatformTop + controller.skinWidth).Within(0.1f));

            var landingPipe = gimmicks.First(item => item.Kind == DemoWorldGimmickKind.PipeLauncher && item.OwningTeam == DemoTeam.Blue);
            landingPipe.enabled = false;
            yield return StabilizeOnGround(controlled, new Vector3(-21f, 0.35f, -10.9f));
            Assert.That(controlled.TryJump(), Is.True);
            var landedOnPipe = false;
            for (var frame = 0; frame < 180; frame++)
            {
                controlled.Tick(SimulationDelta);
                if (frame < 28)
                {
                    controlled.Move(Vector2.down, SimulationDelta);
                }

                if (frame > 35 && controlled.IsGrounded && controlled.transform.position.y > 1.65f)
                {
                    landedOnPipe = true;
                    break;
                }

                yield return null;
            }

            Assert.That(
                landedOnPipe,
                Is.True,
                $"The hunter should land on Blue_Pipe. Final={controlled.transform.position}, " +
                $"grounded={controlled.IsGrounded}, vertical={controlled.VerticalVelocity:0.00}");
            Assert.That(controlled.transform.position.y, Is.EqualTo(1.88f).Within(0.18f));
            landingPipe.enabled = true;

            var redHunter = match.RedHunters.First(actor => actor.IsHired && actor.IsAlive);
            yield return StabilizeOnGround(controlled, new Vector3(-7f, 0.35f, -5.8f));
            yield return StabilizeOnGround(redHunter, new Vector3(-4.2f, 0.35f, -5.8f));

            var water = gimmicks.Single(item => item.Kind == DemoWorldGimmickKind.WaterShield);
            var fire = gimmicks.Single(item => item.Kind == DemoWorldGimmickKind.FireProjectile);
            var earth = gimmicks.Single(item => item.Kind == DemoWorldGimmickKind.EarthAoe);
            var wind = gimmicks.Single(item => item.Kind == DemoWorldGimmickKind.WindBeam);
            var redFlag = gimmicks.Single(item => item.Kind == DemoWorldGimmickKind.TeamFlag && item.OwningTeam == DemoTeam.Red);
            var jumpPad = gimmicks.First(item => item.Kind == DemoWorldGimmickKind.JumpPad);
            var pipeLauncher = landingPipe;

            Assert.That(water.TryActivate(controlled), Is.True);
            var shieldedHealth = controlled.Health;
            controlled.ReceiveDamage(100f, redHunter);
            Assert.That(shieldedHealth - controlled.Health, Is.EqualTo(45f).Within(0.1f));
            Assert.That(water.TryActivate(controlled), Is.False, "Pickup cooldown must prevent immediate reuse.");

            var redHealth = redHunter.Health;
            Assert.That(fire.TryActivate(controlled), Is.True);
            yield return null;
            Assert.That(redHunter.Health, Is.LessThan(redHealth), "Fire Projectile must damage the nearest visible enemy.");

            redHealth = redHunter.Health;
            Assert.That(earth.TryActivate(controlled), Is.True);
            yield return null;
            Assert.That(redHunter.Health, Is.LessThan(redHealth), "Earth AOE must damage nearby hostiles.");
            Assert.That(redHunter.VerticalVelocity, Is.GreaterThan(0f), "Earth AOE must knock nearby hostiles upward.");

            var normalJumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * match.Config.GravityMultiplier * match.Config.JumpHeight);
            Assert.That(wind.TryActivate(controlled), Is.True);
            Assert.That(controlled.VerticalVelocity, Is.GreaterThan(normalJumpVelocity));

            jumpPad.ResetGimmick();
            Assert.That(jumpPad.TryActivate(controlled), Is.True);
            Assert.That(controlled.VerticalVelocity, Is.GreaterThan(normalJumpVelocity));

            pipeLauncher.ResetGimmick();
            Assert.That(pipeLauncher.TryActivate(controlled), Is.True);
            Assert.That(controlled.VerticalVelocity, Is.GreaterThan(normalJumpVelocity));

            var scoreBeforeFlag = match.BlueScore;
            Assert.That(redFlag.TryActivate(controlled), Is.True);
            Assert.That(match.BlueScore, Is.EqualTo(scoreBeforeFlag + match.Config.FlagCaptureScore));
            Assert.That(redFlag.TryActivate(controlled), Is.False);
            Assert.That(match.BlueScore, Is.EqualTo(scoreBeforeFlag + match.Config.FlagCaptureScore));

            water.ResetGimmick();
            Assert.That(water.IsReady, Is.True);
            Assert.That(water.TryActivate(controlled), Is.True, "A reset pickup must be reusable.");
        }

        private static IEnumerator StabilizeOnGround(DemoCombatant actor, Vector3 position)
        {
            var controller = actor.GetComponent<CharacterController>();
            controller.enabled = false;
            actor.transform.position = position;
            controller.enabled = true;

            for (var frame = 0; frame < 120; frame++)
            {
                actor.Tick(SimulationDelta);
                if (frame > 2 && actor.IsGrounded)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"{actor.name} did not settle on the ground at {position}.");
        }
    }
}
