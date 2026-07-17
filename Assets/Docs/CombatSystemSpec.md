# Heros Combat System - Starter Specification

## 1. Purpose

Build the first playable combat loop for Heros, a mobile game inspired by Little Fighter 2. The first version must work while characters are still triangles and have no animations.

The system must support:

- A basic close-range attack such as a punch.
- Team-based hit filtering so allies cannot damage each other.
- Health, normal defense, and critical defense.
- Critical hits.
- Death or knockout when health reaches zero.
- Player and bot characters using the same combat rules.
- A design that can later add animations, weapons, abilities, blocking, knockback, and status effects without replacing the damage core.

## 2. MVP scope

### Included

- One basic attack per character.
- An attack cooldown.
- A temporary attack hitbox represented by a 2D trigger collider.
- One hurtbox per character for receiving hits.
- One hit per target during each attack execution.
- Enemy checks through the existing `ITeamRelationshipService`.
- Damage calculation with a critical-hit flag.
- Defense calculation on the defender.
- Health reduction, death state, and combat events.
- Inspector-configurable combat values.
- Unit tests for calculations and hit rules.

### Not included yet

- Animation timing and animation events.
- Blocking input or a temporary guarding state.
- Combos and chained attacks.
- Multiple attack shapes in one move.
- Knockback, hit stun, invulnerability frames, and crowd control.
- Elemental damage, armor types, buffs, and debuffs.
- Healing, revival, and respawning.
- Networked multiplayer.

These features should be possible later, but they must not complicate the MVP.

## 3. Core combat flow

1. An input source or bot requests the basic attack.
2. The attacker verifies that it is alive, can act, and is not on cooldown.
3. The attack starts and receives a unique attack execution ID.
4. The attack hitbox becomes active for a short configured time.
5. A hurtbox entering or already overlapping the hitbox is considered as a hit candidate.
6. The system rejects the candidate if it is the attacker, an ally, neutral, dead, or already hit by this execution.
7. The attacker rolls for a critical hit and creates a `DamageRequest`.
8. The defender queues the valid request for the current physics-step damage batch.
9. At the end of the physics step, the defender calculates mitigation for every queued hit.
10. The defender applies the whole batch to health, clamped to zero.
11. Combat events report every hit result for future UI, sound, animation, and effects.
12. At zero health the defender enters the dead state, cannot attack, becomes untargetable, and has movement disabled.

Important rule: colliders only discover possible targets. They do not calculate or directly subtract health.

## 4. Responsibilities

### `CharacterCombat` Unity facade

The character prefab should expose combat through one root `MonoBehaviour`. This is the combat composition root and the only component other Unity systems need to reference.

Responsibilities:

- Hold serialized combat configuration.
- Resolve the owning `RegisteredCombatant` and movement integration.
- Construct the plain C# runtime objects in `Awake`.
- Forward Unity lifecycle time to the attack runtime.
- Expose a small public API such as `TryBasicAttack`, `ReceiveDamage`, `IsAlive`, and health events.
- Dispose or unsubscribe runtime objects when destroyed.

`CharacterCombat` does not contain the health, defense, critical, or attack formulas. It wires those collaborating objects together.

Suggested composition:

```text
CharacterCombat (MonoBehaviour facade)
  HealthPool (plain C# object)
  DefenseResolver (plain C# object)
  DamageReceiver (plain C# object)
  BasicAttackRuntime (plain C# object)
  AttackExecutionTracker (plain C# object)
```

This keeps the Inspector understandable while preserving small, independently tested classes.

### Component budget rule

Use one `MonoBehaviour` per character-level feature boundary, not one per internal class and not one giant script for the entire game.

For combat, the budget is one new `CharacterCombat` component. Internal behavior is composed from ordinary C# objects. Add another component only when Unity requires a separate scene identity, callback location, enable state, transform, or lifetime.

Examples:

- Health and defense need no Transform or Unity callback, so they are plain C# objects.
- Attack configuration needs Inspector serialization but no independent lifetime, so it is nested serializable data.
- The attack area needs a child Transform and collider, but no script in the MVP.
- A future shield with its own collider, enable state, and hit rules may justify a small bridge component.

Do not merge combat into the existing `CombatCharacter` merely to reduce the component count. That class currently composes movement. Keeping one movement facade and one combat facade avoids a growing god component while still keeping the prefab small.

### `BasicAttackRuntime`

Owns attack execution for one character as a plain C# object created by `CharacterCombat`.

Responsibilities:

- Accept an attack request from player or bot input.
- Check attack eligibility and cooldown.
- Create a new execution ID.
- Enable and disable the basic attack hitbox.
- Roll the critical-hit result once per target hit.
- Create a `DamageRequest` from the attacker's configured stats.
- Prevent the same target from being hit twice by one execution.

The controller should not read or change a defender's health directly.

### Attack hitbox Unity integration

A child object has a `Collider2D` configured as a trigger, but it needs no script. `CharacterCombat` holds a serialized reference to this collider. Because the root owns the `Rigidbody2D`, the root facade receives trigger messages from its child colliders and delegates valid hitbox contacts to the attack runtime.

Rules:

- It is disabled while no attack is active.
- It holds no permanent combat statistics.
- It supports `OnTriggerEnter2D` and an initial overlap check when activated so a target already inside the area can be hit reliably.
- The facade resolves `CharacterCombat` from the contacted collider or its parent.

### Hurtbox ownership

For the MVP, do not add a separate `Hurtbox2D` component. The attack physics bridge resolves `CharacterCombat` with `GetComponentInParent<CharacterCombat>()` from the contacted collider. Cache successful lookups during an active attack to avoid repeated hierarchy searches.

The character body collider therefore acts as the hurtbox. If the game later needs several hurt regions with different behavior, add one lightweight `Hurtbox2D` metadata bridge only at that point.

Future hurtbox responsibilities would be:

- Expose the owning `CharacterCombat` facade.
- Optionally identify a region such as body, head, or shield.
- Allow multiple hurtbox colliders later while still identifying one logical target.

The triangle prototype can use one body collider as its hurtbox.

### `IDamageReceiver`

The stable entry point for anything that can take damage.

Suggested contract:

```csharp
public interface IDamageReceiver
{
    bool IsAlive { get; }
    DamageResult ReceiveDamage(in DamageRequest request);
}
```

### `HealthPool`

Owns only maximum and current health. This is a plain C# object, not a `MonoBehaviour`.

Responsibilities:

- Store maximum and current health.
- Apply an already calculated positive final-damage value.
- Clamp health.
- Emit damaged, health changed, and died events.
- Ignore damage after death.

`HealthPool` knows nothing about teams, attacks, critical hits, or defense.

### `DefenseResolver`

Owns defender-side mitigation as a plain C# strategy.

Responsibilities:

- Read normal defense.
- Apply critical defense only for a critical request.
- Calculate and return final damage plus mitigation details.
- Never change health.

Suggested contract:

```csharp
public interface IDamageMitigation
{
    MitigationResult Mitigate(in DamageRequest request);
}
```

Keeping this behind an interface lets a future character replace flat defense with percentage armor, shields, invulnerability, or a layered mitigation pipeline without changing `HealthPool`.

### `DamageReceiver`

Coordinates receiving damage as a plain C# application service:

1. Validate alive state and request.
2. Ask `DefenseResolver` for the mitigated value.
3. Pass final damage to `HealthPool`.
4. Combine both results into `DamageResult`.

This is the logic corresponding to the proposed `GetHurt` behavior. Prefer the public name `ReceiveDamage` because it remains meaningful after animations are added.

### Serialized combat configuration

Use one `[Serializable]` configuration owned by `CharacterCombat`, with nested groups so the Inspector stays readable:

```text
CombatConfiguration
  Vitality
    MaxHealth
  Defense
    Defense
    CriticalDefense
  BasicAttack
    Damage
    CriticalChance
    CriticalDamageMultiplier
    Cooldown
    ActiveTime
```

These serializable classes contain configuration data only. They do not run gameplay or inherit from `MonoBehaviour`.

The initial values are:

```text
MaxHealth
BasicAttackDamage
Defense
CriticalChance
CriticalDamageMultiplier
CriticalDefense
BasicAttackCooldown
BasicAttackActiveTime
```

For the MVP this configuration is serialized inside the single `CharacterCombat` facade. When several characters share presets, move the same configuration to a `ScriptableObject` asset without changing the runtime interfaces.

## 5. Damage data

### `DamageRequest`

Immutable data created by the attacker and consumed by the defender.

Required fields:

```text
Attacker              ICombatant reference
AttackExecutionId     unique integer or unsigned integer
RawDamage             non-negative value before defense
IsCritical            critical-hit flag
HitPoint              world position for future visual effects
```

Later fields may include damage type, knockback, ability ID, or status effects.

### `DamageResult`

Returned by the defender and used for feedback.

Required fields:

```text
WasApplied            false if the hit was rejected
RawDamage
NormalDefenseApplied
CriticalDefenseApplied
FinalDamage
RemainingHealth
WasCritical
WasLethal
```

Returning a result keeps damage numbers, sound, and future hit reactions consistent with the actual health calculation.

## 6. Initial calculation rules

Use whole-number health and damage for predictable tuning. Stats must be clamped to valid ranges during validation.

### Critical roll

```text
IsCritical = random value in [0, 1) < CriticalChance
```

- `CriticalChance` is clamped from `0` to `1`.
- The critical roll belongs to the attacker.
- Inject a random source into the pure calculation code so tests do not depend on Unity randomness.

### Raw damage

```text
RawDamage = BasicAttackDamage
if IsCritical:
    RawDamage = round(BasicAttackDamage * CriticalDamageMultiplier)
```

### Defender mitigation

```text
TotalDefense = Defense
if IsCritical:
    TotalDefense += CriticalDefense

FinalDamage = max(1, RawDamage - TotalDefense)
```

The minimum of 1 means a valid enemy hit always makes progress. This is the recommended MVP rule and can later become a configurable minimum.

### Health

```text
NewHealth = clamp(CurrentHealth - FinalDamage, 0, MaxHealth)
WasLethal = CurrentHealth was above 0 and NewHealth is 0
```

Example:

- Basic attack damage: 10
- Critical multiplier: 2
- Defender normal defense: 3
- Defender critical defense: 4
- Normal hit: `max(1, 10 - 3) = 7`
- Critical hit: `max(1, 20 - 3 - 4) = 13`

## 7. Simultaneous hits and race-condition policy

Unity gameplay callbacks normally execute on the main thread, but callback order can still create an ordering race. Two attackers may hit one defender during the same physics step. The result must not depend on which `OnTriggerEnter2D` callback Unity invokes first.

Use a per-defender pending damage batch:

1. When a hit is detected, validate teams, self-hit rules, duplicate execution IDs, and the defender's alive state.
2. Queue the immutable `DamageRequest` instead of changing health immediately.
3. Resolve all requests queued during that physics step together at one defined point after physics callbacks.
4. Calculate defense independently for each hit. Defense is not shared or consumed between hits.
5. Sum the final damage values and apply the total to `HealthPool` once.
6. Produce one `DamageResult` per accepted request so both attackers receive hit confirmation.
7. Emit `HealthChanged` once for the batch and `Died` at most once.

Example:

- Defender starts the physics step with 30 health.
- Attacker A deals 8 final damage.
- Attacker B deals 11 final damage during the same step.
- Both hits are accepted and reported.
- Health after the batch is `30 - 8 - 11 = 11`.

If the combined damage is lethal, every hit accepted into that batch still resolves even if one hit would have reached zero by itself. Health remains clamped to zero and death occurs once. A request discovered in a later physics step is rejected because the defender is already dead.

The batch must use a snapshot of eligibility at queue time. Event listeners must run only after the batch state is committed, so a listener cannot insert damage halfway through resolution. Any damage requested recursively from a damage event is queued for the next batch.

For deterministic tests and future replay support, resolve a batch in a stable order using `AttackExecutionId`, with a stable attacker identifier as a tie breaker. The final health is based on the sum and therefore does not depend on this ordering, but event order remains predictable.

## 8. Team and target rules

Use the existing `RegisteredCombatant`, `ITeamMember`, and `DifferentTeamRelationshipService`.

A hit is valid only when all conditions are true:

- Attacker and defender references are valid.
- Attacker and defender are different logical combatants.
- `AreEnemies(attacker.TeamMember, defender.TeamMember)` returns true.
- Defender is alive and targetable.
- Defender was not already hit by this attack execution.

With the current relationship service, neutral characters do not attack or receive enemy damage. Friendly fire is off.

Update `RegisteredCombatant.IsTargetable` so it also reflects the character's alive state after health is integrated.

## 9. Collider and physics setup

Recommended prefab structure with the minimum new component count:

```text
CharacterRoot
  CombatCharacter
  RegisteredCombatant
  CharacterCombat
  Rigidbody2D
  BodyCollider
    Collider2D
  BasicAttackHitbox
    Collider2D (trigger, disabled by default)
```

Only one new `MonoBehaviour` type is required for the complete MVP combat system:

- `CharacterCombat`, the facade, composition root, and Unity physics bridge on the character root.

Health, defense, damage receiving, attack state, critical rolls, and duplicate-hit tracking remain plain C# objects. The child hitbox is only a configured collider. Serializable configuration remains nested data inside `CharacterCombat` and does not add prefab components.

If Unity callback routing becomes ambiguous after a character gains several trigger colliders, introduce a tiny hitbox bridge at that time. Do not add it preemptively.

Use dedicated Unity layers such as `AttackHitbox` and `Hurtbox`. Configure the physics collision matrix so attack hitboxes query hurtboxes but do not produce unrelated collisions.

The attack hitbox is placed in front of the character based on its facing direction. Until animations exist, show a short color flash or gizmo so attacks and hits can be debugged visually.

## 10. Attack timing and state

The first attack needs these phases:

- `Ready`: an attack may begin.
- `Active`: the hitbox is enabled for `BasicAttackActiveTime`.
- `Cooldown`: the hitbox is disabled and a new attack cannot begin.

The first implementation can drive these phases with elapsed time in `Update`. Do not depend on animation events yet. When animations arrive, animation events or state-machine signals can define the active window while keeping the same hit and damage pipeline.

An attacker cannot begin a basic attack when:

- Dead.
- Already in an active attack.
- Cooldown has not completed.
- A future action gate disables attacking.

Add an attack gate similar in spirit to the existing movement gate, or expose an `IsAttacking` state that can be added to `CompositeMovementGate`. For the MVP, movement should be disabled during the active phase and allowed during cooldown.

## 11. Death behavior

When health reaches zero:

- Emit the death event exactly once.
- Cancel any active attack and disable its hitbox.
- Disable movement through the movement gate.
- Make the combatant untargetable.
- Ignore damage requests from later physics steps. Finish every request already accepted into the lethal batch.
- Keep the GameObject present for now so the triangle remains visible and debugging is easy.

Removal, knockout animation, rewards, and respawning belong to a later stage controller.

## 12. Events for future presentation

The domain components should publish C# events without requiring animation or UI components:

```text
AttackStarted
AttackEnded
DamageApplied(DamageResult)
HealthChanged(current, maximum)
Died
```

Presentation components may subscribe later for:

- Animation triggers.
- Hit flashes and particles.
- Sound and vibration.
- Floating damage numbers.
- Health bars.
- Camera shake.

Gameplay correctness must not depend on a listener being present.

## 13. Mobile input boundary

Combat logic must not read touch input directly. Define an attack input source, similar to movement input:

```csharp
public interface IAttackInputSource
{
    bool ConsumeBasicAttackPressed();
}
```

Initial implementations can include:

- A keyboard source for editor testing.
- A mobile UI button source.
- A bot attack source that requests an attack when an enemy is within range.

All three feed the same `CharacterCombat.TryBasicAttack` facade method and therefore the same `BasicAttackRuntime`.

## 14. Test plan

### EditMode unit tests

- Normal damage subtracts normal defense.
- Critical damage applies the multiplier, normal defense, and critical defense.
- Critical defense is ignored for a normal hit.
- Final damage never falls below 1 for a valid hit.
- Health never falls below zero.
- Death occurs once when health reaches zero.
- Damage after death is rejected.
- Zero and invalid serialized stats are clamped safely.
- Allies cannot damage each other.
- Neutral combatants are not valid enemies.
- Self-hits are rejected.
- One attack execution cannot damage the same target twice.
- A new execution can damage the same target again.
- A deterministic random source produces expected critical results.
- Two valid hits queued in one physics step both reduce health.
- Reversing simultaneous-hit callback order produces the same final health.
- A simultaneous lethal batch reports every accepted hit and emits death once.
- A recursively requested hit is deferred to the next batch.

### PlayMode integration tests

- Activating a hitbox damages an overlapping enemy.
- A target already overlapping when the hitbox activates is detected.
- An allied hurtbox is ignored.
- The hitbox turns off after the active window.
- Cooldown prevents attack spam.
- Death cancels the active hitbox and disables movement and targeting.
- Two attackers contacting one defender in the same physics step both receive hit confirmation.

## 15. Recommended implementation order

1. Add pure immutable `DamageRequest`, `DamageResult`, and calculation result types.
2. Add plain C# `HealthPool`, `DefenseResolver`, and batched `DamageReceiver` with EditMode tests.
3. Add the `CharacterCombat` facade to compose the runtime objects and expose their state.
4. Connect alive state to `RegisteredCombatant.IsTargetable` and movement gating.
5. Add the child hitbox collider, root trigger routing, layer filtering, and parent facade resolution.
6. Add `BasicAttackRuntime`, execution IDs, timing, cooldown, and duplicate-hit protection.
7. Add editor keyboard attack input.
8. Add mobile UI button and bot attack sources.
9. Add PlayMode tests and temporary triangle hit feedback.

## 16. MVP acceptance criteria

The combat foundation is ready when:

- Two triangle characters on opposing teams can damage one another with a basic attack.
- Characters on the same team cannot damage one another.
- Each attack can damage each enemy at most once.
- Hits from multiple attackers in the same physics step all reduce the defender's health.
- Normal and critical damage produce the expected final health after both defense values.
- A dead character cannot move, attack, take more damage, or be selected as a bot target.
- Player, mobile button, and bot attack requests use the same `CharacterCombat` facade and attack runtime.
- The core calculations are covered by deterministic EditMode tests.
- No part of the damage calculation depends on animations.

## 17. Decisions to revisit after the first playable build

- Whether defense stays flat or becomes percentage-based.
- Whether critical defense reduces critical bonus damage only or all critical damage. The MVP reduces all critical damage.
- Whether a valid hit always deals at least 1 damage. The MVP says yes.
- Whether movement is locked during windup, active time, recovery, or only active time.
- Whether one punch may hit several enemies. The MVP says yes, once per enemy.
- Whether neutral objects can be damaged through a separate destructible relationship rule.
- Whether stats should move to character `ScriptableObject` assets when the roster grows.
