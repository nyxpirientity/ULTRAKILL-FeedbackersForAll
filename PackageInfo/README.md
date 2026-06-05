# Important!
For this plugin/mod to work, you'll need to ensure your BepInEx is configured to have *`HideManagerGameObject`* set to *`true`*, or else the game will destroy the plugin!

# About
Adds a cheat that lets *enemies parry you*, whether they parry you is decided by a simple algorithm I made to determine the 'parryability' of an attempted hit (a value between 0.0 and 1.0), which is supposed to represent whether they successfully manage to hit a parry on you. This process is explicitly not supposed to use RNG in any way (however, RNG may be featured *as an option* eventually). Enemies also have feedbacker stamina, and feedbacker cooldowns, just like you.

Enemies can only parry many of your *ranged attacks* and when you're parried, there is usually a way to counter parry! But alternatively, you can simply dodge the projectile being thrown back at you, but this will result in no damage dealt. Everytime a projectile is boosted/parried, either by you or the enemy, the projectile is intended to increase in damage and thus danger for both you and your opponent.

# Configurability
In the configuration for the mod you can adjust many of the feedbacker stats, and the parry "skill" of enemies, on a per enemy basis, alongside some controls for the signalling that you've been parried.