using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.DefaultData.AI;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.R3D;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.Trainer;
using System;
using System.Collections.Generic;
using System.Linq;
#if DEBUG_BATTLE_CAMERAPOS
using Kermalis.PokemonGameEngine.Input;
#endif

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed partial class BattleGUI
    {
        public static BattleGUI Instance { get; private set; }

        private Action _onClosed;

        public readonly PBEBattle Battle;
        public readonly PBETrainer Trainer;
        private readonly PBEDDWildAI _wildAI;
        private readonly PBEDDAI[] _ais;

        // Trainer battle
        private readonly string _trainerDefeatText;
        private readonly TrainerClass _trainerClass;

        private BattleGUI(PBEBattle battle, Action onClosed, IReadOnlyList<Party> trainerParties, TrainerClass trainerClass, string trainerDefeatText)
            : this(battle, trainerParties) // BattleGUI_Render
        {
            // Create AIs
            _ais = new PBEDDAI[trainerParties.Count];
            // Skip player
            for (int i = 1; i < trainerParties.Count; i++)
            {
                PBETrainer t = battle.Trainers[i];
                if (t.IsWild)
                {
                    _wildAI = new PBEDDWildAI(t);
                }
                else
                {
                    _ais[i] = new PBEDDAI(t);
                }
            }

            // Finish init
            _onClosed = onClosed;
            _trainerClass = trainerClass;
            _trainerDefeatText = trainerDefeatText;
            battle.OnNewEvent += SinglePlayerBattle_OnNewEvent;
            battle.OnStateChanged += SinglePlayerBattle_OnStateChanged;

            Instance = this;
        }

        public static void CreateWildBattle(PBEBattle battle, Action onClosed, IReadOnlyList<Party> trainerParties)
        {
            _ = new BattleGUI(battle, onClosed, trainerParties,
                default, default);
        }
        public static void CreateTrainerBattle(PBEBattle battle, Action onClosed, IReadOnlyList<Party> trainerParties, TrainerClass trainerClass, string trainerDefeatText)
        {
            _ = new BattleGUI(battle, onClosed, trainerParties,
                trainerClass, trainerDefeatText);
        }

        private void Begin()
        {
            CreateBattleThread(Battle.Begin);
        }

        private void SetExitToOverworldFadeAndCallback()
        {
            SoundControl.FadeOutBattleBGMToOverworldBGM();
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeOutBattle);
        }
        private void OnBattleEnded()
        {
            // Fade out (temporarily until capture screen exists)
            // TODO: Does pokerus spread before the capture screen, or after the mon is added to the party?
            SetExitToOverworldFadeAndCallback();
        }

        private void CB_FadeInBattle()
        {
            _tasks.RunTasks();
            RenderBattle();
            _fadeTransition.Render();
            _frameBuffer.RenderToScreen();

            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _stringWindow = Window.CreateStandardMessageBox(Colors.FromRGBA(49, 49, 49, 128), RenderSize);
            OnFadeInFinished();
            Game.Instance.SetCallback(CB_RunTasksAndEvents);
        }
        private void CB_FadeOutBattle()
        {
            _tasks.RunTasks();
            RenderBattle();
            _fadeTransition.Render();
            _frameBuffer.RenderToScreen();

            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _stringPrinter?.Delete();
            _stringPrinter = null;
            _stringWindow.Close();
            _stringWindow = null;
            RemoveActionsGUI();
            CleanUpStuffAfterFadeOut();
            _frameBuffer.Delete();
            Instance = null;
            _onClosed();
            _onClosed = null;
        }
        private void CB_RunTasksAndEvents()
        {
            _tasks.RunTasks();
            HandleNewEvents();

#if DEBUG_BATTLE_CAMERAPOS
            if (InputManager.JustPressed(Key.Select))
            {
                CreateCameraMotionTask(_defaultPosition, null, CAM_SPEED_DEFAULT);
            }
            else
            {
                _camera.PR.Debug_Move(5f);
                //_models[3].PR.Debug_Move(5f);
            }
#endif

            RenderBattle();
            _frameBuffer.RenderToScreen();
        }

        /// <summary>Run after the fade out, and deletes the info bars etc, but also does Pokerus, updating the bag, everything else</summary>
        private void CleanUpStuffAfterFadeOut()
        {
            _trainerSprite?.AnimImage.DeductReference();
            foreach (Model m in _models)
            {
                m.Delete();
            }
            _modelShader.Delete();
            _spriteShader.Delete();
            _spriteMesh.Delete();
            // Copy our Pokémon back from battle, update teammates, update wild Pokémon
            // Could technically only update what we need (like caught mon, roaming mon, and following partners)
            for (int i = 0; i < _parties.Length; i++)
            {
                BattlePokemonParty party = _parties[i];
                party.UpdateToParty(i == Trainer?.Id);
                foreach (BattlePokemon bPkmn in party.BattleParty)
                {
                    bPkmn.Delete();
                }
            }
            // Update inventory
            Game.Instance.Save.PlayerInventory.FromPBEInventory(Trainer.Inventory);
            // Do capture stuff (temporary)
            if (Battle.BattleResult == PBEBattleResult.WildCapture)
            {
                Game.Instance.Save.GameStats[GameStat.PokemonCaptures]++;
                PBETrainer wildTrainer = Battle.Teams[1].Trainers[0];
                BattlePokemonParty party = _parties[wildTrainer.Id];
                PBEBattlePokemon wildPkmn = wildTrainer.ActiveBattlers.Single();
                PartyPokemon pPkmn = party[wildPkmn].PartyPkmn;
                pPkmn.UpdateFromBattle_Caught(wildPkmn);
                Game.Instance.Save.GivePokemon(pPkmn); // Also sets pokedex caught flag
            }
            // Pokerus
            Pokerus.TryCreatePokerus(Game.Instance.Save.PlayerParty);
            Pokerus.TrySpreadPokerus(Game.Instance.Save.PlayerParty);
        }

        private void UpdateDisguisedPID(PBEBattlePokemon pkmn)
        {
            BattlePokemonParty party = _parties[pkmn.Trainer.Id];
            party[pkmn].UpdateDisguisedPID(party);
        }
        private void SetSeen(PBEBattlePokemon pkmn)
        {
            if (pkmn.Trainer == Trainer)
            {
                return;
            }
            BattlePokemon bPkmn = _parties[pkmn.Trainer.Id][pkmn];
            Game.Instance.Save.Pokedex.SetSeen(pkmn.KnownSpecies, pkmn.KnownForm, pkmn.KnownGender, pkmn.KnownShiny, bPkmn.DisguisedPID);
        }
        private void UpdateFriendshipForFaint(PBEBattlePokemon pkmn)
        {
            byte oppLevel = pkmn.Team.OpposingTeam.ActiveBattlers.Max(p => p.Level);
            PartyPokemon pp = _parties[pkmn.Trainer.Id][pkmn].PartyPkmn;
            Friendship.Event e = oppLevel - pkmn.Level >= 30 ? Friendship.Event.Faint_GE30 : Friendship.Event.Faint_L30;
            Friendship.AdjustFriendship(pkmn, pp, e);
        }
        private void UpdateFriendshipForLevelUp(PBEBattlePokemon pkmn)
        {
            PartyPokemon pp = _parties[pkmn.Trainer.Id][pkmn].PartyPkmn;
            Friendship.AdjustFriendship(pkmn, pp, Friendship.Event.LevelUpBattle);
        }
        private static void PlayCry(PBEBattlePokemon pkmn)
        {
            SoundControl.PlayCryFromHP(pkmn.KnownSpecies, pkmn.KnownForm, pkmn.HPPercentage, pan: GetCryPanpot(pkmn));
        }
        private static float GetCryPanpot(PBEBattlePokemon pkmn)
        {
            return pkmn.Team.Id == 0 ? -0.35f : 0.35f;
        }
    }
}
