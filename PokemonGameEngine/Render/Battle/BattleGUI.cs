using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.DefaultData.AI;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.R3D;
using Kermalis.PokemonGameEngine.Render.Transitions;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.Trainer;
using System;
using System.Collections.Generic;
using System.Linq;
#if DEBUG_BATTLE_CAMERAPOS
using Kermalis.PokemonGameEngine.Input;
#endif

namespace Kermalis.PokemonGameEngine.Render.Battle
{
    internal sealed partial class BattleGUI
    {
        public static BattleGUI Instance { get; private set; }

        private Action _onClosed;

        public readonly PBEBattle Battle;
        private readonly PBETrainer _trainer;
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
            _transition = FadeToColorTransition.ToBlackStandard();
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
            RenderBattleAndHUD();
            _transition.Render();
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _transition = null;
            _stringWindow = Window.CreateStandardMessageBox(Colors.FromRGBA(49, 49, 49, 128), RenderSize);
            OnFadeInFinished();
            Game.Instance.SetCallback(CB_RunTasksAndEvents);
        }
        private void CB_FadeOutBattle()
        {
            _tasks.RunTasks();
            RenderBattleAndHUD();
            _transition.Render();
            _frameBuffer.BlitToScreen();

            if (!_transition.IsDone)
            {
                return;
            }

            _transition.Dispose();
            _stringPrinter?.Delete();
            _stringWindow.Close();
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
                CreateCameraMotionTask(DefaultCamPosition, null, CAM_SPEED_DEFAULT);
            }
            else
            {
                Camera.PR.Debug_Move(10f);
                //_models[3].PR.Debug_Move(5f);
            }
#endif

            RenderBattleAndHUD();
            _frameBuffer.BlitToScreen();
        }

        /// <summary>Run after the fade out, and deletes the info bars etc, but also does Pokerus, updating the bag, everything else</summary>
        private void CleanUpStuffAfterFadeOut()
        {
            _trainerSprite?.Image.DeductReference();
            foreach (Model m in _models)
            {
                m.Delete();
            }
            _modelShader.Delete();
            _spriteShader.Delete();
            _spriteMesh.Delete();
            _dayTintFrameBuffer.Delete();
            _shadowFrameBuffer.Delete();
            // Copy our Pokémon back from battle, update teammates, update wild Pokémon
            // Could technically only update what we need (like caught mon, roaming mon, and following partners)
            for (int i = 0; i < _parties.Length; i++)
            {
                BattlePokemonParty party = _parties[i];
                party.UpdateToParty(i == _trainer?.Id);
                foreach (BattlePokemon bPkmn in party.BattleParty)
                {
                    bPkmn.Delete();
                }
            }
            // Update inventory
            Game.Instance.Save.PlayerInventory.FromPBEInventory(_trainer.Inventory);
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

        public BattlePokemon GetBattlePokemon(PBEBattlePokemon pbePkmn)
        {
            return _parties[pbePkmn.Trainer.Id][pbePkmn];
        }
        private void SetSeen(BattlePokemon bPkmn)
        {
            PBEBattlePokemon pbePkmn = bPkmn.PBEPkmn;
            if (pbePkmn.Trainer == _trainer)
            {
                return;
            }
            Game.Instance.Save.Pokedex.SetSeen(pbePkmn.KnownSpecies, pbePkmn.KnownForm, pbePkmn.KnownGender, pbePkmn.KnownShiny, bPkmn.DisguisedPID);
        }
        private static void UpdateFriendshipForFaint(BattlePokemon bPkmn)
        {
            PBEBattlePokemon pbePkmn = bPkmn.PBEPkmn;
            byte oppLevel = pbePkmn.Team.OpposingTeam.ActiveBattlers.Max(p => p.Level);
            Friendship.Event e = oppLevel - pbePkmn.Level >= 30 ? Friendship.Event.Faint_GE30 : Friendship.Event.Faint_L30;
            Friendship.AdjustFriendship(pbePkmn, bPkmn.PartyPkmn, e);
        }
        private static void UpdateFriendshipForLevelUp(BattlePokemon bPkmn)
        {
            Friendship.AdjustFriendship(bPkmn.PBEPkmn, bPkmn.PartyPkmn, Friendship.Event.LevelUpBattle);
        }
        private static void PlayCry(PBEBattlePokemon pbePkmn)
        {
            SoundControl.PlayCryFromHP(pbePkmn.KnownSpecies, pbePkmn.KnownForm, pbePkmn.HPPercentage, pan: GetCryPanpot(pbePkmn));
        }
        private static float GetCryPanpot(PBEBattlePokemon pbePkmn)
        {
            return pbePkmn.Team.Id == 0 ? -0.35f : 0.35f;
        }
    }
}
