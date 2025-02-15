# 2.22.4
- Added 'GBC Vanilla Render' config to community patches (false by default) - makes GBC cards render with vanilla cost sprites
- Added 'Vanilla Stacking' config to community patches (false by default) - renders cards with only two (stacking) sigils as they appear in vanilla, eg Spore Mice and Sporedigger
- Fixed incorrect rulebook icon scaling in Act 3
- Fixed rulebook-related null errors when playing during Magnificus' Act
- Fixed slot modification rulebook pages not appearing correctly during Magnificus' Act
- Fixed cards shield sigils not rendering correctly
- DamageShieldBehaviour class now has 'initialised' boolean field
- Highest displayable bone cost value in Act 1 raised from 13+ to 15+
- Minor tweaks to blood and bone cost icons

# 2.22.3
- Fixed pelt names when a user goes to the trader with modded cards, Examples shown below.
<img src="https://github.com/user-attachments/assets/49b9da13-e602-4020-a560-40344e9ef6af" width="750">
<img src="https://github.com/user-attachments/assets/4b432cc9-a0f7-4d75-99b4-8951ba46705b" width="750">
- Fixed index error when loading custom challenges
- Publicised ConsumableItemData.SetPrefabModelType

# 2.22.2
- Added GetEnergyConfig method to community patch's EnergyDrone class - retrieves the current Act's EnergyConfigInfo
- CommunityPatches: Added community config to move pelt price tags to the right of the card
- Experimental: Changed gemified to only reduce a single cost on a card, with priority of Energy > Bones > Gems > Blood
- Fixed positioning errors caused by having multiple custom boss challenge icons
- EnergyConfigInfo's fields can now be modified when initialising a new instance
- Updated installation guide on the ReadMe to match the wiki, added link to wiki.

# 2.22.1
- Added IShieldPreventedDamage and IShieldPreventedDamageInHand ability triggers and interfaces
- Added TriggerBreakShield, wraps BreakShield in an IEnumerator for additional customisation by modders
- Added ICustomExhaustSequence trigger interface for modifying the draw pile exhaustion effect -  use with Opponents
- Fixed board slots being uninteractable if a slot effect with a rulebook interactable was reset
- Removed debug logging related to rulebook redirect coordinates

# 2.22.0
- Added FullBoon objects for each vanilla Boon
- Added 'AllFullBoons' list to BoonManager
- Added support for boons and items appearing in multiple acts' rulebooks
- Added RuleBookRedirectManager and support for rulebook text redirects/page links
- Added additional methods to RuleBookManager: ItemShouldBeAdded, BoonShouldBeAdded, SlotModShouldBeAdded, GetUnformattedPageId
- Added GetFullBoon and GetFullConsumableItemData extension methods
- Added extension methods for adding text redirects to abilities, stat icons, items, boons, slot modifications, and rulebook pages
- Added ModificationType.SetSharedRulebook - used for slot modifications that should share their rulebook entry with other slot modifications
- Added support for multiple rulebook sprites for slot modifications (SetRulebookP03Sprite, SetRulebookGrimoraSprite, SetRulebookMagnificusSprite)
- Added RuleBookController.Instance.OpenToCustomPage
- Added CustomDiskTalkingCard abstract class
- Added TalkingCardManager.NewDisk and TalkingCardManager.CreateDisk
- Fixed RuleBook construction patches having lower patch priority than intended
- Fixed slot modification interactable being enabled when no rulebook entry exists
- Fixed slot modification rulebook pages not working in Act 3
- Fixed rulebook sprites being smaller than normal after flipping to a slot modification rulebook page
- Fixed DiskTalkingCards created through the API not correctly working under certain conditions
- Moved ConsumableItemManager patches to a separate ConsumableItemPatches class
- Modified implementation of rulebook fill page logic to let modders patch the API logic
    - Patch 'RuleBookManagerPatches.FillPage' to do this
- Tweaked how custom rulebook pages are added and detected
- Wiki: Tweaked page for adding custom rulebook sections
- Wiki: Added section on adding text redirects

# 2.21.1
- Fixed RuleBookManager not syncing when playing with no custom rulebook sections

# 2.21.0
- Fixed ability stacks not rendering
- Fixed rendering error when displaying a card with tribes outside of Act 1
- Fixed ResetShields not re-setting lostShield to false under certain conditions
- Fixed RemoveMaxEnergy not working as intended
- Fixed custom AudioClips not loading correctly on Mac OSX
- Added RuleBookManager for adding custom rulebook sections (see wiki for more info)
- Added AllModificationInfos, AllModificationTypes, and modification syncing to SlotModificationManager
- Added additional functionality to SlotModificationManager - Infos now store name and GUID
- Added rulebook entry support for slot modifications - use extension method .SetRulebook() when adding your slot modification
- Added more shield-related extensions
- Added some AbilityInfo-related extensions
- Added ShieldManager.AllShieldAbilities and ShieldManager.AllShieldInfos for easier tracking of custom shield abilities
- Added config to the community patches to add a forced red emission to Undead Cat
- Modified Obsolete warning for Helpers.CustomLine to point to Dialogue.CustomLine

# 2.20.0
- Updated wiki sections for Adding Map Nodes, and Conditional Map Nodes; moved Special Sequencers section to Opponents
- Fixed issues related to challlenge icon sorting when a boss icon is present
- Fixed TalkingCardManager not properly configuring talking cards in Part 3on the page
- Fixed extension properties for CardModificationInfos saved to the save file not consistently loading
- Fixed cards not shaking when losing a shield
- Fixed ShieldManager.BreakShield not being called when a shielded card takes damage
- Added SlotModificationManager for adding behaviour to card slots; see wiki for more info
- Added SaveFileExtensions class
- Added support for cards costing multiple of the same colour Mox
- Added debugging to GetCustomCardCosts method; please let the API folks know if you receive any warnings/errors marked by '[GetCustomCardCosts]'
- Added StatIconInfo.SetRulebookInfo and StatIconInfo.SetAppliesToStats
- Added NodeData.SelectionCondition's ChallengeIsActive and NumChallengesOfTypeActive
- Added PlayableCard.GetShieldCount<\T>() for getting a specific shield ability's NumShield value
- Added new config to the community patches to reduce the size of the price tags during the Buy Pelts sequence
- Added CardModificationInfo.AddNegateAbilities extension method
- Removed leftover debugging related to boss challenge icons and custom costs
- P03 Face Card Displayer can now show card costs other than energy
- Publiciser warnings from the API and Community Patches no longer appear in the console
- SaveFile.CurrentDeck now returns gbcData.deck in Act 2
- DeathShieldLatch can longer target cards with an active shield
- Cards with a broken shield return to their default portrait when regaining their shield
- Shield abilities now use NumShields to determine visual sigil stacking
- CardModificationInfo.SetNameReplacement now accepts null value for the name replacement

# 2.19.5
- Fixed pixel Bones cost icons not appearing when the cost is greater than vanilla amounts
- Fixed interaction where a Gemified card that gives a blue gem doesn't spend the correct resource amount when played
- Fixed active challenges desyncing from the icons when returning to the select challenges screen from a custom screen
- Added pixel icon to Aquasquirrel (courtesy of Zepht)
- Added GetCustomCostAmount extension methods for automatically accounting for whether a custom cost can be negative
- Added support for making custom challenges that use the Final Boss challenge's icon format (occupying the whole column)
- AscensionChallengePaginator's leftArrow and rightArrow fields are now AscensionMenuInteractables
- Changed method name of GenBaseGameChallengs to GenBaseGameChallenges
- Changed Act 1 energy cost choice cardback to match the cost's icon colour
- Changed TestCost.OnPlayed to no longer trigger on negative values
- CustomCardCost.OnPlayed no longer triggers for custom costs with a value of 0 (negative values can still occur)
- Challenges no longer show dependencies/incompatibilies when viewed in the pause menu and end screen

# 2.19.4
- Fixed error when retrieving custom card costs from a card with no custom card costs
- Fixed cards with custom card costs using the pixel cost icons in some circumstances
- Fixed modifications to base Pelt choice amounts not being reflected in-game
- Fixed latched sigils not appearing in Act 3
- Added config to randomise cost choice order
- Added additional functionality to FullCardCost - see wiki and documentation for more info
- Added TestCost class to community patches - can be added to the game by enabling "Test Mode" in the configs
- Added extension methods for setting and getting a custom card cost using the CustomCardCost class instance
- Custom costs now support cost tiers and checking CanBePlayedByTurn2WithHand
- Custom costs' textures now differentiate whether they're from Acts 1, 2, or 3 when storing them post-assemblage
- Card choices when trading Pelts are now positioned correctly for amounts non-divisible by 4
- Modified Act 1 latch patch logic
- Publicised a number of TradePeltSequence patch methods
- Refactored some TradePeltSequence patches
- Reverted undocumented changes to some SniperFix parameter names in previous version

# 2.19.3
- Fixed index error related to Totem sigils
- Fixed index error related to opponent sniper targeting
- Fixed Shield Latch sigil not displaying the first latched sigil
- Added config to community patches to reset Leshy's eye colour after triggering the grizzly bear sequence during boss fights

# 2.19.2
- Fixed activated abilities not being interactable in Act 3
- Fixed cards with costs above vanilla defaults not displaying
- Added debug logs to AddCustomTribesToList (used to add custom Tribes to the list of obtainable Totem tops)

# 2.19.1
- Fixed API not retrieving pixel card costs above 5

# 2.19.0
- Fixed decals added via temporary mods not being cleared from the base card
- Fixed merged and totem sigils being uninteractable if the icon has been flipped vertically
- Fixed pixel Shapeshifter patch not correctly patching DisguiseOutOfBattle
- Fixed temporary decal mods not being removed in Act 1
- Fixed softlock in Part 1 during the boon-gaining sequence
- Fixed all copies of a custom challenge becoming activated/deactivated when the page is reloaded
- Fixed Sentry ability softlocking when the base card dies before all Sentry stacks are triggered
- Fixed softlock when talking card dialogue cannot be parsed in certain conditions
- Added public method GetIjiraqDisguises to pixel Shapeshifter patch for easier modification of Shapeshifter for modders
- Added variant of PeltManager.New
- Added variant of PlayableCard.AllAbilities that accounts for negated abilities in TemporaryMods
- Added support for creating custom card costs using new class CustomCardCost; see wiki for more information
- Added ability to remove gems costs from a card using CardModificationInfos
- Added a number of extension methods for CardModificationInfos (RemoveGemsCost, SetCustomCostId, etc)
- Added helpers for getting TextBox.Style from CardInfo.temple or the chosen ambition
- Rewrote CardModificationInfoManager's id system for setting persistent extended properties in a CardModificationInfo's singletonId
- Rewrote pixel Shapeshifter patch to RevealInBattle to hopefully prevent errors in Act 1
- PeltManager.New now throws an error when getCardChoices is null
- Changed LogLevel of dialogue event insertion message from Info to Debug
- API death cards now use the clean singleton id when creating the death card info mod
- Temporary decal mods are now removed from Act 2 cards instead of being cleared
- Opponent snipers will now target a random slot if there are no opposing cards (previously only targeted the opposing slot)

# 2.18.7
- Fixed softlock during Act 1's final boss cabin/boons sequence 
- Fixed startup errors relating to ShieldManager transpilers
- Fixed resource drone not showing up outside of Act 3
- Fixed latched sigils not visually disappearing when using RemoveTemporaryMod to remove a latch CardModInfo
- Fixed stack sigil icons not correctly replacing the '1' in stackable sigil icons with the appropriate stack number
- Fixed Act 2 Tutor sequence displaying the wrong number of cards above the max of 42
- Fixed temporary mods not correctly updating a card's shield count above 1
- Added extension methods PlayableCard.AllCardModificationInfos(), PlayableCard.RemoveCardModificationInfo()
- Added SpriteType 'PixelStandardButton'
- CustomTriggerFinder now caches the list of non-card triggers before iteration
- ActivatedDamageShieldBehaviour now inherits from DamageShieldBehaviour instead of ActivateAbilityBehaviour
- ActivatedDamageShieldBehaviour now implements the logic from ExtendedActivatedAbilityBehaviour
- Mud Turtle now has a broken shield portrait (identical to its alternate portrait, which is unchanged)
- CardTriggerHandler.RemoveAbility now only destroys the AbilityBehaviour if triggeredAbilities no longer contains the corresponding Ability
- Act 2 Tutor now supports multiple pages of cards

# 2.18.6
- Fixed Royal fight softlocking if config option 'Hide Act 1 Scenery' is set to true
- Fixed activated custom challenges not remaining activated when returning to the challenge screen
- Fixed TransformIntoCardInHand and TransformIntoCardAboveHand not checking for TriggersOncePerStack
- Added missing null checks to ResourceDrone patches
- Added pixel icon to Transformer
- Transformer sigil icon will now display the number of turns till evolution if it's greater than 1
- Transformer and Fledgling sigils now correctly update their display when evolving into another card with the Fledgling/Transformer sigil
- Certain shield-giving effects no longer reset shields to prevent incorrect shield totals
- Improved the 'Custom Card Costs' section of the wiki

# 2.18.5
- Fixed DrawCopyOnDeath creating warnings in the console
- Fixed talking cards locking the camera view when obtained during the Trapper boss's final phase
- Fixed ResourceDrone softlocking during Leshy's goodbye sequence if ConfigDefaultDrone is false
- Added missing null checks
- Added PlayableCard.GetStatIconHealthBuffs()
- Added PlayableCard.TransformIntoCardAboveHand() - variant of TransformIntoCardInHand that incorporates MoveCardAboveHand
- Added FullAbility.SetExtendedProperty for setting an AbilityInfo's custom property during ability creation
- Reverted change to resource drone preventing it from being parented to the scale outside of Act 1
- Improved visual fix for the full pack Pack Rat sequence

# 2.18.4
- Fixed Sniper sigil targeting the wrong side of the board
- Fixed placeholder tribe choice icons being placed incorrectly
- Auto-gen tribe choice texture is now only created if the tribe can be found in tribe choices

# 2.18.3
- Fixed resource drone behaving incorrectly outside of Act 1
- Added null checks to various custom triggers
- Added more extension methods for CardInfo and AbilityInfo
- Added PlayableCard extension methods: AddShieldCount(Ability), AddShieldCount\<T>() and AddShieldCount(Ability), RemoveShieldCount\<T>()
    - These affect the internal numShields field, and do NOT add or remove ability stacks
- Added alternate portrait 'SacrificablePortrait' for when a card can be sacrificed in Act 1 or Act 2 (part of the SetShaking method)
- Added methods for getting the emissive portraits for extra alt portraits (EmissiveSteelTrapPortrait(), EmissiveBrokenShieldPortrait(), etc.)
- Expanded SniperFix sniper logic with additional methods for easier patching and modification:
    - DoSniperLogic() - controls whether to use player or opponent sniper logic
    - DoAttackTargetSlotsLogic() - controls attack logic for each target slot
    - GetValidTargets() - returns the list of card slot the player and opponent can target
    - PlayerTargetSelectedCallback() - called when the player selects a valid target
    - PlayerSlotCursorEnterCallback() - called when the player's cursor enters a slot
    - OpponentSelectTarget() - returns a card slot for the opponent to target and attack
- Revamped the wiki to (hopefully) make it easier to navigate and read through

# 2.18.2
- Fixed abilities marked TriggersOncePerStack not actually triggering once per stack on evolution
- Fixed CardManager.Remove not actually removing cards
- Fixed mods on card clones being lost during card sync
- Added extension methods for setting the emissions for SteelTrap and BrokenShield alt portraits
- Added Config to disable boss scenery for optimization purposes
- Exposed EncounterManager.NewEncounters so JSONLoader may replace existing Encounters
- Refactored Act 1 energy drone movement logic, added support for 'immediate' bool (Default Drone must be true)
- Act 1 energy drone game object is now named 'Part1ResourceDrone'
- Act 1 energy drone is now correctly synced with the scale when Default Drone config is false

# 2.18.1
- Fixed BoxCollider null reference during Act 3 Build-A-Card-Sequencer
- Fixed Act 3 bone displayer screen changing to static whenever P03 changes their face
- Added TryGetGuidAndKeyEnumValue for getting the mod GUID and key from enum value
- Custom regions now store their mod GUID

# 2.18.0
- Fixed SetPixelAbilityIcon() not accepting 22x10 textures for activated abilities
- Fixed IModifyDamageTaken priority sorting being reversed
- Fixed null errors in TakeDamage and custom trigger calls
- Added extension methods for getting emission portraits, setting animated portrait
- Added CustomFields helper for associating data with objects or classes
- Added IModifyDirectDamage, IOnTurnEndInQueue custom triggers
- Custom Tribes now store their name and GUID

# 2.17.0
- Fixed card extension GetAbilityStacks() being able to return a negative value; minimum value is now capped at 0
- Added ability interfaces IModifyDamageTaken, IPreTakeDamage, which trigger at the start of PlayableCard.TakeDamage
- Added PlayableCard extension method ResetShield(Ability) for only resetting shields belonging to a certain ability
- Added ShieldManager class and changed how shields are managed in the game's logic
- Added abstract classes DamageShieldBehaviour and ActivatedDamageShieldBehaviour
- Added support for adding alternate portraits for SteelTrap activation and broken shields
- Added portrait setters SetSteelTrapPortrait(), SetBrokenShieldPortrait(), SetPixelSteelTrapPortrait(), SetPixelBrokenShieldPortrait()
- Added support for adding new language translations
- Added AbilityInfo extension method SetHideSingleStacks(), affecting how stacking sigils are affected by being hidden (see wiki)
- DeathShield ability now has a custom AbilityBehaviour attached to it
- DeathShield ability is no longer passive, and can stack
- TakeDamage trigger now requires damage to be above 0 to activate
- Cards can no longer lose shields from attacks that deal 0 damage
- Damage dealt to cards can no longer go below 0
- Updated the wiki with sections on the additions
- Zombie Parrot is now part of the Avian tribe

# 2.16.1
- Gem Shield sigil now visually applies the Armoured sigil to cards in Act 1

# 2.16.0
- Added interface IGetAttackingSlots for altering the order cards attack in, see the wiki for more information
- Added out-of-turn (cards attacking outside of their owner's turn) damage support
- Added PlayableCard extension method GetAbilityStacks()
- Added PlayableCard extension method TransformIntoCardInHand()
- Moved SlotAttackSlotFixes and SelfAttackDamagePatch from community patches to the API, renamed to SlotAttackSlotPatches and DoCombatPhasePatches respectively
- Made community patch method RandomAbilityPatches.GetRandomAbility public

# 2.15.2
- Fixed cards not evolving correctly if the Fledgling sigil was obtained via card mods (card merge, totem, etc.)
- Moved the Squirrel Orbit community patch into the main API
- Added SetTransformCardId(), GetTransformerCardId() for controlling the Transformer evolution separate of the standard evolution
- Transformer sigil will now also check for a card's API-set TransformerCardId if no card mod is found
- Transformer sigil now also adjusts Blood and Bone costs when transforming
- Transformer sigil now correctly works for cards without a defined evolution/transformation

# 2.15.1
- Fixed Transformer sigil disappearing upon transformation in certain scenarios
- Fixed Act 3 Bone Display checking the wrong card cost, resulting in the display always appearing
- Fixed Act 3 Bone Display null error in certain Acts

# 2.15.0
- Fixed friend cards created by G0LLY not having any mods
- Reverted previous change to cloned CardInfos
- Tweaked RandomAbilityPatches to hopefully prevent obtaining sigils already possessed by the card
- Added cost display support for Act 3
- Added bone counter for Act 3

# 2.14.5
- Cloned CardInfos now only copy over Gemify mods, unless they possess BountyHunterInfo/DeathCardInfo/BuildACardInfo
- Fixed certain card mods duplicating when the card evolve
- Added ResourcesManager.RemoveMaxEnergy, ResourcesManager.ShowRemoveMaxEnergy extension methods

# 2.14.4
- Fixed the first energy cell remaining closed in Act 1 when battle starts
- Added new field to PeltManager.PeltData 'peltTierName' used when trading pelts
- Added extension method PeltData.SetTierName
- The Trader will now speak the correct name of custom pelts when trading with them
- Added DialogueManager.GenerateTraderPeltsEvent for creating custom dialogue events spoken by the Trader when trading a custom pelt
- Added DialogueManager.GenerateRegionIntroEvent for creating the dialogue event played upon entering a custom region

# 2.14.3
- Fixed Act 2 bug relating to stackable sigils and activated sigils in the deck display menu
- Fixed dynamic costs still not working in Act 2
- Fixed dynamic gem costs checking ResourcesManager instead of OpponentGemsManager for opponent cards
- Fixed dynamic costs not checking for owned blue gems
- Fixed dynamic costs not updating energy display correctly
- Changed dynamic costs to patch SetInfo instead of Awake
- Re-added dynamic cost error messages for when the card or card info is null
- Added ResourcesManager.Instance.GemsOfType(GemType) to check for owned gems of the specified type

# 2.14.2
- Fixed Overclock patch not checking for the correct Acts
- Fixed appearance behaviour's Card field always returning null in Act 2
- Added OverridePixelPortrait virtual method to PixelAppearanceBehaviour to allow for changing card portraits in Act 2
- Added CardInfo.SetPixelAlternatePortrait() and Cardinfo.GetPixelAlternatePortrait() for storing alternate pixel portraits
- Re-added SetTerrain method without optional bool parameter
- SwitchToAlternatePortrait and SwitchToDefaultPortrait now work in Act 2 using the above system
- Removed cost-related error spam in Act 2

# 2.14.1
- Custom tribes are now given a placeholder reward cardback if one isn't provided
- Fixed visual error when flipping a custom tribe choice for a tribe without a custom cardback
- Fixed pixel stat icons not hiding the underlying stat number
- Fixed ChooseTarget null exception
- Fixed opponent cards with mods not being created properly (eg Bounty Hunters)
- Fixed being able to ring the bell in Part 2 during the Tutor sequence
- Fixed GBC packs not checking for onePerDeck when selecting possible cards
- Fixed decals added via temporary mods not clearing from cards in Act 2
- Changed what vanilla abilities are marked as Act2Modular (see the Part2ModularAbilities file for the full list)
- Removed leftover debug info during start-up
- Added CardInfo.SetCardTemple()
- Added CardModInfo extension methods SetTemporaryDecal and IsTemporaryDecal (primarily for internal use, maybe you'll find a use for it)
- Added GBCPackManager.ModifyGBCPacks function for altering what cards can be found in GBC card packs

# 2.14.0
- Fixed Sniper duplicating attacks from sigils like Double Strike
- Fixed interaction between Waterborne and Fledgling in Act 2
- Fixed Cuckoo sigil softlocking in Act 2 when making a Raven Egg
- Fixed sigils added via temporary mods not displaying in Act 2
- Fixed hiddenAbilities not affecting sigil display in Act 2
- Fixed Handy sigil visual bug outside of Act 2
- Fixed Shapeshifter special ability in Act 2
- Added pixel sprites for Raven Egg and Cuckoo/Broken Egg
- Added ResourceBankManager.AddDecal(), PlayableCard.AddTemporaryMods(), CardModificationInfo.AddDecalIds
- Added AbilityInfo.SetPixelIcon(string pathToArt), CardInfo.RemoveAppearances(), CardInfo.SetDefaultEvolutionName()
- Added DialogueManager.PlayDialogueEventSafe - combines TextDisplayer.PlayDialogueEvent and DialogueHandler.PlayDialogueHandler for multi-act support
- Added support for directly loading AudioClips via the GramophoneManager
- Added support for adding decals to pixel cards via DecalIds
- Added pixel portrait for Ijiraq
- Added support for changing costs midbattle using CardModificationInfos or a HarmonyPatch
- Changed TranspilerHelpers.LogCodeInscryptions to also function as an extension method for List<CodeInstruction>
- FullSpecialTriggeredAbility now stores the ability name and mod GUID
- Temporary mods can now be used to add decals to a card
- CardRenderInfo.OverrideAbilityIcon now works for Act 2 sigils
- CardInfo.SetTerrain() now has optional parameter 'useTerrainLayout', defaulting to true
- Made method used to add stacks to pixel sigils public
- Updated the wiki

# 2.13.3
- Fixed null error when opening card packs in Act 2
- Fixed pixel cards with activated sigils showing the activated sigil icon twice (does not fix the button obscuring sigils)
- Added new helper class GemsManagerHelpers with helper methods: OpponentHasGems, PlayerHasGems
- Changed how Act 2 descriptions are altered to prevent conflicts
- True Scholar now correctly requires a Blue Gem to be owned prior to use

# 2.13.2
- Fixed Hoarder sigil breaking when used by opponents in Act 2
- Fixed Hodag special ability not working in Act 2
- Fixed cards marked as AffectedByTidalLock not being killed by Tidal Lock when it's on a giant card
- Added card extension methods SetAffectedByTidalLock and HasAlternatePortrait
- Added ability extension method SetPart2Ability
- Added AbilityCardMetaCategory AbilityManager.Part2Modular
- Added pixel portraits for Empty Vessel and its Gemified variants, Ant, Bee, Dam, Chime, and the Tail cards
- Amorphous sigil now works in Act 2
- Vessel Printer sigil now works in Act 2
- Trinket Bearer sigil is now disabled in Act 2
- Hidden abilities are now properly hidden in Act 2
- Fledgling sigil now properly shows the required (up to the number 3) in Act 2
- Fledgling sigil's rulebook description now updates to show the selected card's actual number of required turns
- Squirrel, Aqua Squirrel, and Rabbit are now marked as AffectedByTidalLock
- SteelTrap sigil no longer changes a card's portrait to the closed trap; will now switch to an alternate portrait if it exists

# 2.13.1
- Fixed custom items falling through reality
- Added card extension method IsAffectedByTidalLock
- Mental Gemnastics sigil now works in Act 1
- Tidal Lock sigil now works for non-Moon cards

# 2.13.0
- Fixed DontDestroyOnLoad warnings when using custom items
- Fixed weird spacing for Mox cost textures in Act 1
- Fixed player death cards not inheriting Energy, Mox, or custom costs
- Fixed the hint dialogue for insufficient Energy in Act 1 being the wrong colour
- Fixed ExtendedActivatedAbilityBehaviour discarding negative activation cost modifiers
- Fixed Sniper not accounting for custom sigils that modify attack slots
- Fixed Tutor not working in Act 2
- Added more extension methods to BoardManager
- Added new card extensions SetGemify and SetGemsCost(params GemType[])
- Added catch-all cost textures for when Blood or Bones go above 13
- Added CardModificationInfoManager and DeathCardManager
- Added extended property support and extensions for CardModificationInfo
- Added Blood activation cost support to ExtendedActivatedAbilityBehaviour
- ExtendedActivatedAbilityBehaviour now calls PostActivate() if a card dies from paying the Health cost
- Leshy now recognises death cards with multiple costs in his dialogue
- Leshy will now let you create death cards with up to 8 sigils
- Minor adjustments to some cost textures
- Rearranged order of Mox cost textures to align with order of Mox on the Gem Module
- Removed empty cost textures for Blood, Bones, Energy, Mox from the community patches
- Sniper patch's methods are now public

# 2.12.0
- Fixed ExtendedActivatedAbilityBehaviour's Health cost not subtracting Health correctly
- Fixed softlock in Act 1 during death card creation
- Fixed custom cards that start Gemified not working as intended when obtained in-game
- Potentially fixed softlock when making terrain for a region
- Added further checks to challenge icon-related patch to prevent softlocks
- Added decal, appearance behaviour, and Gemified card support for Act 2 cards
- Added Singleton<OpponentGemsManager> for keeping track of opponent gems
- Added new helper method for creating Sprites from resource files in an assembly
- Added new SpriteType for creating pixel card decals
- Gemified visuals now work correctly for Act 3 opponents
- Cost choice node now offers each Mox colour individually
- Added new config "Default Drone" to change the model and position of the Energy Drone
- Amorphous sigil now activates when used by opponents or obtained via evolution/temp mod
- Owned Mox in Act 1 now updates when a card is hooked by the Angler or via the Hook item

# 2.11.2
- Fixed starter deck custom unlocks not working
- Fixed card icons not being properly centred for starter decks with 4+ cards
- Cards in Acts 2 and 3 can now display up to 8 sigils
- Blood tokens in Act 3 now appear to the side of the board instead of on it
- Blood tokens now stack on each other when there are more than 4

# 2.11.1
- Fixed regions in Act 1 being out of order
- Fixed the console message concerning custom dialogue events not giving the right amount

# 2.11.0
- Refactored how regions are handled by the API to prevent duplicate bosses
- Refactored how bosses are selected to prevent duplicates being encountered
- Changed when modded Ascension data is cleared to allow for editing it post-clear
- Added more descriptive error logs for some commonly encountered errors
- Added config option to reduce the amount of debug info shown in the console
- Added methods to aid in creating encounter turn plans
- Added more methods for interacting with lists, new debug method to aid in making transpilers
- Added ExtendedActivatedAbilityBehaviour class; allows for dynamic costs and Health costs
- Fixed SetOnePerDeck() and SetHideStats() being inaccessible
- Fixed AddCardBlueprint() not setting the replacement card correctly

# 2.10.0
- Completely revamped PeltManager to be more user friendly (Mod breaking)
- Added LocalizationManager for more language support with mods
- Added helper method for custom pelts to change cards trader
- Pelts offered by Trapper capped at 8.
- Pelts offered by Trapper are now randomized if more than 8
- Fixed soft lock at trader when having more pelts than cards to offer 
- Fixed the campfire fix breaking the normal sequence
- Fixed HasCardMetaCategory returning the inverse of its intended value
- Fixed stackable sigils not showing numbers above 9

# 2.9.1
- Fixed the campfire fix breaking the normal sequence

# 2.9.0
- Added talking card support!
- Moved the "CustomLine" struct outside of the Dialogue.Helpers class.
- Fixed tribe choice node being able to offer vanilla tribes with no cards
- Fixed totem choice node being able to offer tops for vanilla tribes with no cards
- Added fallbacks for tribal choice node if there are less than 3 chooseable tribes
- Added fallback to campfire node if you don't have any cards that can be buffed 
- Fixed 'outdated plugins' warning showing up when it shouldn't, tweaked message slightly

# 2.8.1
- Added CardInfo extensions for checking CardMetaCategories, cause why not
- Added DialogueManager for custom dialogue for regions and Custom Color support
- Added ResourceBankManager for custom resources. Avoids doing this for every mod
- Deprecated DialogueEventGenerator (Moved to Dialogue Manager)
- Fixed repeating bosses on regions that have multiple boss possibilities
- Fixed custom props not having a renderer on the top parent and breaking loading regions
- Fixed arrows on the challenges select screen being offscreen at certain resolutions
- Fixed tribe choice node being able to offer custom tribes with no cards
- Fixed being able to get custom totem tops for tribes with no cards

# 2.8.0
- Added support for custom masks
- Fixed sometimes items use the wrong behaviour
- Added more resource and asset bundle helpers

# 2.7.4
- Fixed latch fix modifying the base info
- Fixed stackable abilities activating twice when they shouldn't

# 2.7.3
- Fixed sniper fix not accounting for cards with Repulsive ability
- Fixed latch abilities not working in Act 2
- Added ExtendedProperties for abilities
- Added new ability setter SetTriggersOncePerStack for controlling the behaviour of stackable abilities after a card evolves
- Added new helper methods for creating cards: SetOnePerDeck, SetHideStats
- Added new helper methods for abilities: SetCanStack, SetTriggersOncePerStack, SetActivated, SetPassive, SetConduit, SetConduitCell
- Added new remover methods for cards: RemoveAbilities, RemoveAbilitiesSingle, RemoveTraits, RemoveTribes

# v2.7.2
- Added `CanActivateOutsideBattles` extension method to ConsumableItemData so they can be used outside of battles.
- Added Missing Tribe Icon fallback texture for totem tops when a tribe has no icon
- Changed TotemManager to accept a `CompositeTotemPiece` type for custom behaviour other than always a custom icon
- Fixed lag when entering gain consumable item map node
- Fixed crash when using custom consumable items
- Fixed hard lock when getting totem top that doesn't have an icon
- Fixed Pack Rat card object not having the correct background during the item node sequence
- Fixed Latch abilities removing stat boosts when latching a card
- Fixed latched abilities not properly rendering in some acts

# v2.7.1
- Changed Pelt Manager to no longer have an interface for future safety! (NOTE This will break all mods with custom pelts!)
- Added Squirrel tribe art (Thanks Drift!)
- Fixed Green Gem stat icon showing as a black square in act 1
- Fixed Green Gem stat icon not appearing in rulebook
- Fixed Squirrel totem top causing NMA when using custom totem tops
- Fixed being unable to play cards with a Blood cost above 4 via sacrifices

# v2.7.0
- Added support for custom pelts
- Added support for converting audio files to AudioClip objects
- Added support for adding custom tracks to the Gramophone
- Added support for adding custom audio files
- Warning message for outdated plugins now lists the outdated plugins
- Energy Drone now tweens with the scales, kinda
- Fixed visual bug where energy cells didn't start closed in successive battles

# v2.6.0
- Added support for custom consumable items using a choice of a few models
- Added support for custom consumable card in a bottle items
- Added support for custom consumable items with a custom model
- Added more helper extensions for checking abilities, traits, special abilities
- Fixed null instances in Act 2 spamming the console with warnings

# v2.5.3
- Added support for custom card unlock requirements
- Fixed non-giant cards with Omni Strike not directly attacking their opposing slot when there are no opposing cards
- Fixed cards attacking their own side of the board during combat not adding damage to the correct side of the scale
- Fixed an issue where a challenge would go missing if you had more than 14 installed

# v2.5.2
- Fixed the sentry fix overriding patches to SlotAttackSlot

# v2.5.1
- Reverted part of the sentry fix that was causing problems
- Made it easier to override the default totem head

# v2.5.0
- Added support for custom totem heads
- Custom Tribes now appear as a totem in the Wood Carver nodes
- Fixes for Sentry ability in Act 1 relating to PackMule, Loose Tail, and enemy totems
- Fixed stacked ability icons causing issues when trying to render numbers on some sigil icons
- Fixed Latches not working in Act 1

# v2.4.2
- Switched to debug version

# v2.4.1
- Fixed Sentry ability not working properly in Act for players or opponents

# v2.4.0
- Reworked challenges
- Fixed gemified opponent cards not working properly
- Fixed stat icons in Act 3

# v2.3.0
- Fixed orange gem not counting towards passive attack
- Fixed PackMule special ability not working on the player's side
- Fixed Mox cost choice node not working
- Fixed boon rulebook and removal
- Fixed tribe choice nodes
- Fixed error caused by passing null when assigning a custom tail portrait
- Improved activated ability fix
- Improved some extensions and attack buffs
- Fixes for starter decks
- Fixes for custom regions, more customisation when creating one
- Added more extensions
- Fixed stat icon rendering for Act 3

# v2.2.0
- Added an interface that triggers when cards are facedown
- Updated custom artwork for GBC numbers
- Fixed flipped icons spamming the log with warnings
- Fixed Tribe API breaking mods that use CardbackTexture
- Added custom combat triggers
- Added more custom extensions
- Fixed Latch abilities in Act 1
- Fixed extension methods for setting custom flipped portrait affecting the wrong card
- Fixed optimisation issues caused by passive attack bufs
- Fixed activated sigils
- Added node manager for custom nodes
- Fixed cards getting buffs after the game ends

# v2.1.0
- Fixed blurry portraits when playing on low graphics settings

# v2.0.3
- Added support for custom tribes and boons
- Added config option to opt of custom cost renders for Act 2 cards
- Refactored and added documentation for CardExtensions

# v2.0.2
- Improved the process of creating stat icons to automatically register and add the corresponding special ability
- Added log warnings for improperly registered cards

# v2.0.1
- Bugfix for SaveData

# v2.0
- Rewritten to use base game objects

# v1.13.0
- Added support for custom card backgrounds, dialogs, encounters and talking cards
- Fixes to abilities loading and stackable custom abilities

# v1.12.1
- Bugfix so CustomCard doesn't wipe ability information.

# v1.12
- Fixes params.
- Adds feature for special abilities and special stat icons.
- Added support for emissions.

# v1.11
- Added support for more identifiers

# v1.10.1
- Fix for abilities which do not have identifier.

# v1.10
- Added ability identifiers.

# v1.9.1
- Added support for mox.
- Forced ability texture to point filter.

# v1.9
- Added config options for energy.

# v1.8.2
- Fixed appearanceBehaviour (again).

# v1.8.1
- Fix pixelTex dimensions.

# v1.8
## Not compatible with v1.7.2
- Changes to using TypeMapper.

# v1.7.2
- Fixed error when not adding any abilities.

# v1.7.1
- Fixed appearance behaviours not loading properly.

# v1.7
- Added support for custom abilities!

# v1.6
- Changed textures to point filter to reduce blur.

# v1.5.2
- Enabled fix for evolveParams and some other disabled options.

# v1.5.1
- Fix to accessing private instance for regions.

# v1.5
## Not compatible with v1.4
- Changed all references to API including guid.

# v1.4
- Set up support for customising and adding regions.

# v1.3
- Set up project to work as a library for other plugins to use.

# v1.2.1.1
- Fixed previous patch.

# v1.2.1
- Fixed cards not being inserted into the card pool on chapter select.

# v1.2
## Not compatible with v1.1
- Added customising default cards through CustomCard.
- Custom cards are added via the **CustomCard** constructor rather than through the **AddCard** method.

# v1.1
- Hooked into a much more sensible method to load the cards into the card pool.