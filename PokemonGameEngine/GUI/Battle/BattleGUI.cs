using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.DefaultData.AI;
using Kermalis.PokemonBattleEngine.Packets;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Pkmn;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.R3D;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.Trainer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        // Battle thread
        private IPBEPacket _newPacket;
        private PBEBattleState? _newState;
        private readonly ManualResetEvent _resumeProcessing = new(false);

        private readonly string _trainerDefeatText;
        private readonly TrainerClass _trainerClass;

        public BattleGUI(PBEBattle battle, Action onClosed, IReadOnlyList<Party> trainerParties, TrainerClass trainerClass = default, string trainerDefeatText = null)
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

        private void SinglePlayerBattle_OnNewEvent(PBEBattle _, IPBEPacket packet)
        {
            _newPacket = packet;
            _resumeProcessing.Reset(); // Pause battle thread
            _resumeProcessing.WaitOne(); // Wait for permission to continue
            // TODO: Will keep the game alive even if close is pressed
        }
        private void SinglePlayerBattle_OnStateChanged(PBEBattle battle)
        {
            _newState = battle.BattleState;
            // Let this thread die; we will create a new one if the battle is to continue
        }
        /// <summary>
        /// Using this is necessary to prevent the battle state from changing on the main thread.
        /// Whatever GL calls we make must be on the main thread though, which is why we delegate the packets and states back.
        /// </summary>
        private static void CreateBattleThread(ThreadStart start)
        {
            new Thread(start) { Name = "Battle Thread" }.Start();
        }
        private void ResumeBattleThread()
        {
            _resumeProcessing.Set();
        }
        private bool HandleNewPacket()
        {
            if (_newPacket is null)
            {
                return false;
            }
            IPBEPacket packet = _newPacket;
            _newPacket = null;
            ProcessPacket(packet);
            return true;
        }
        private void HandleNewBattleState()
        {
            if (_newState is null)
            {
                return;
            }
            PBEBattleState s = _newState.Value;
            _newState = null;
            switch (s)
            {
                case PBEBattleState.Ended: OnBattleEnded(); break;
                case PBEBattleState.ReadyToRunSwitches: CreateBattleThread(Battle.RunSwitches); break;
                case PBEBattleState.ReadyToRunTurn: CreateBattleThread(Battle.RunTurn); break;
            }
        }
        private void HandleNewEvents()
        {
            if (!HandleNewPacket())
            {
                HandleNewBattleState();
            }
        }

        private void OnPartyReplacementClosed()
        {
            OverworldGUI.UpdateDayTint(true); // Catch up time
            _fadeTransition = FadeFromColorTransition.FromBlackStandard();
            Game.Instance.SetCallback(CB_FadeFromPartyReplacement);
        }

        private void CB_FadeInBattle()
        {
            OverworldGUI.UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _stringWindow = Window.CreateStandardMessageBox(Colors.FromRGBA(49, 49, 49, 128));
            OnFadeInFinished();
            Game.Instance.SetCallback(CB_RunTasksAndEvents);
        }
        private void CB_FadeOutBattle()
        {
            OverworldGUI.UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            _stringPrinter?.Delete();
            _stringPrinter = null;
            _stringWindow.Close();
            _stringWindow = null;
            _actionsGUI?.Dispose();
            _actionsGUI = null;
            CleanUpStuffAfterFadeOut();
            _onClosed();
            _onClosed = null;
            Instance = null;
        }
        private void CB_FadeToPartyForReplacement()
        {
            OverworldGUI.UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            SetMessageWindowVisibility(true);
            _ = new PartyGUI(SpritedParties[Trainer.Id], PartyGUI.Mode.BattleReplace, OnPartyReplacementClosed);
        }
        private void CB_FadeFromPartyReplacement()
        {
            OverworldGUI.UpdateDayTint(false);
            RenderFading();
            if (!_fadeTransition.IsDone)
            {
                return;
            }

            _fadeTransition = null;
            SetMessageWindowVisibility(false);
            Game.Instance.SetCallback(CB_RunTasksAndEvents);
            CreateBattleThread(() => Trainer.SelectSwitchesIfValid(Switches, out _));
        }
        private void CB_RunTasksAndEvents()
        {
            OverworldGUI.UpdateDayTint(false);
            HandleNewEvents();
            _tasks.RunTasks();

#if DEBUG_BATTLE_CAMERAPOS
            if (InputManager.JustPressed(Key.Select))
            {
                MoveCameraToDefaultPosition(null);
            }
            else
            {
                _camera.PR.Debug_Move(5f);
                //_models[3].PR.Debug_Move(5f);
            }
#endif

            RenderBattle();
        }

        /// <summary>Run after the fade out, and deletes the info bars etc, but also does Pokerus, updating the bag, everything else</summary>
        private void CleanUpStuffAfterFadeOut()
        {
            foreach (Model m in _models)
            {
                m.Delete();
            }
            _shader.Delete();
            // Copy our Pokémon back from battle, update teammates, update wild Pokémon
            // Could technically only update what we need (like caught mon, roaming mon, and following partners)
            for (int i = 0; i < SpritedParties.Length; i++)
            {
                SpritedBattlePokemonParty p = SpritedParties[i];
                p.UpdateToParty(i == Trainer?.Id);
                foreach (SpritedBattlePokemon pp in p.SpritedParty)
                {
                    pp.Delete();
                }
            }
            // Update inventory
            Game.Instance.Save.PlayerInventory.FromPBEInventory(Trainer.Inventory);
            // Do capture stuff (temporary)
            if (Battle.BattleResult == PBEBattleResult.WildCapture)
            {
                Game.Instance.Save.GameStats[GameStat.PokemonCaptures]++;
                PBETrainer wildTrainer = Battle.Teams[1].Trainers[0];
                SpritedBattlePokemonParty sp = SpritedParties[wildTrainer.Id];
                PBEBattlePokemon wildPkmn = wildTrainer.ActiveBattlers.Single();
                PartyPokemon pkmn = sp[wildPkmn].PartyPkmn;
                pkmn.UpdateFromBattle_Caught(wildPkmn);
                Game.Instance.Save.GivePokemon(pkmn); // Also sets pokedex caught flag
            }
            // Pokerus
            Pokerus.TryCreatePokerus(Game.Instance.Save.PlayerParty);
            Pokerus.TrySpreadPokerus(Game.Instance.Save.PlayerParty);
        }

        private void UpdateDisguisedPID(PBEBattlePokemon pkmn)
        {
            SpritedBattlePokemonParty sp = SpritedParties[pkmn.Trainer.Id];
            sp[pkmn].UpdateDisguisedPID(sp);
        }
        private void SetSeen(PBEBattlePokemon pkmn)
        {
            if (pkmn.Trainer == Trainer)
            {
                return;
            }
            SpritedBattlePokemon sPkmn = SpritedParties[pkmn.Trainer.Id][pkmn];
            Game.Instance.Save.Pokedex.SetSeen(pkmn.KnownSpecies, pkmn.KnownForm, pkmn.KnownGender, pkmn.KnownShiny, sPkmn.DisguisedPID);
        }
        private void UpdateFriendshipForFaint(PBEBattlePokemon pkmn)
        {
            byte oppLevel = pkmn.Team.OpposingTeam.ActiveBattlers.Max(p => p.Level);
            PartyPokemon pp = SpritedParties[pkmn.Trainer.Id][pkmn].PartyPkmn;
            Friendship.Event e = oppLevel - pkmn.Level >= 30 ? Friendship.Event.Faint_GE30 : Friendship.Event.Faint_L30;
            Friendship.AdjustFriendship(pkmn, pp, e);
        }
        private void UpdateFriendshipForLevelUp(PBEBattlePokemon pkmn)
        {
            PartyPokemon pp = SpritedParties[pkmn.Trainer.Id][pkmn].PartyPkmn;
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

        #region Actions

        private readonly List<PBEBattlePokemon> _actions = new(3);
        public List<PBEBattlePokemon> StandBy { get; } = new(3);
        private void BeginActionsLoop()
        {
            foreach (PBEBattlePokemon pkmn in Trainer.Party)
            {
                pkmn.TurnAction = null;
            }
            _actions.Clear();
            _actions.AddRange(Trainer.ActiveBattlersOrdered);
            StandBy.Clear();
            ActionsLoop();
        }
        public void ActionsLoop()
        {
            int i = _actions.FindIndex(p => p.TurnAction is null);
            if (i == -1) // None in need of an action; time to submit
            {
                RemoveActionsGUIAndSetCallbacks();
                var arr = new PBETurnAction[_actions.Count];
                for (int j = 0; j < arr.Length; j++)
                {
                    arr[j] = _actions[j].TurnAction;
                }
                CreateBattleThread(() => Trainer.SelectActionsIfValid(out _, arr));
            }
            else
            {
                SpritedBattlePokemonParty party = SpritedParties[Trainer.Id];
                _actionsGUI?.Dispose();
                PBEBattlePokemon pkmn = _actions[i];
                _actionsGUI = new ActionsGUI(party, pkmn);
                SetStaticMessage($"What will {pkmn.Nickname} do?", _actionsGUI.SetCallbackForFightChoices);
                if (i != 0)
                {
                    Game.Instance.SetCallback(CB_RunTasksAndEvents); // Will run the message task
                }
            }
        }
        public void Flee()
        {
            RemoveActionsGUIAndSetCallbacks();
            CreateBattleThread(() => Trainer.SelectFleeIfValid(out _));
        }

        public List<PBESwitchIn> Switches { get; } = new(3);
        public byte SwitchesRequired;
        public List<PBEFieldPosition> PositionStandBy { get; } = new(3);
        private void SetUpBattleReplacementFade()
        {
            // TODO: Run from wild?
            Switches.Clear();
            StandBy.Clear();
            PositionStandBy.Clear();
            _fadeTransition = FadeToColorTransition.ToBlackStandard();
            Game.Instance.SetCallback(CB_FadeToPartyForReplacement);
        }

        private void RemoveActionsGUIAndSetCallbacks()
        {
            _actionsGUI.Dispose();
            _actionsGUI = null;
            Game.Instance.SetCallback(CB_RunTasksAndEvents);
        }

        #endregion

        #region Packet Processing

        private void ProcessPacket(IPBEPacket packet)
        {
            // Packets with logic
            switch (packet)
            {
                case PBEMoveLockPacket _:
                case PBEMovePPChangedPacket _:
                case PBEIllusionPacket _:
                case PBETransformPacket _:
                case PBEBattlePacket _:
                case PBETurnBeganPacket _:
                {
                    ResumeBattleThread(); // No need to wait
                    return;
                }
                case PBEActionsRequestPacket arp:
                {
                    PBETrainer t = arp.Trainer;
                    if (t == Trainer)
                    {
                        BeginActionsLoop();
                    }
                    else
                    {
                        // If the team is wild, no flees are allowed by default
                        // Check IsWild instead of Battle type, since we can have an ai partner trainer
                        if (t.IsWild)
                        {
                            CreateBattleThread(() => _wildAI.CreateActions(false));
                        }
                        else
                        {
                            CreateBattleThread(_ais[t.Id].CreateActions);
                        }
                    }
                    ResumeBattleThread(); // No need to wait
                    return;
                }
                case PBESwitchInRequestPacket sirp:
                {
                    PBETrainer t = sirp.Trainer;
                    if (t == Trainer)
                    {
                        SwitchesRequired = sirp.Amount;
                        SetUpBattleReplacementFade();
                    }
                    else
                    {
                        CreateBattleThread(_ais[t.Id].CreateSwitches);
                    }
                    ResumeBattleThread(); // No need to wait
                    return;
                }
                case PBEAutoCenterPacket acp:
                {
                    PBEBattlePokemon pkmn0 = acp.Pokemon0Trainer.GetPokemon(acp.Pokemon0);
                    PBEBattlePokemon pkmn1 = acp.Pokemon1Trainer.GetPokemon(acp.Pokemon1);
                    MovePokemon(pkmn0, acp.Pokemon0OldPosition);
                    MovePokemon(pkmn1, acp.Pokemon1OldPosition);
                    break;
                }
                case PBEPkmnEXPChangedPacket pecp:
                {
                    PBEBattlePokemon pokemon = pecp.PokemonTrainer.GetPokemon(pecp.Pokemon);
                    if (pokemon.FieldPosition != PBEFieldPosition.None)
                    {
                        UpdatePokemon(pokemon, true, false, false, false, false, false);
                    }
                    break;
                }
                case PBEPkmnFaintedPacket pfp:
                {
                    PBEBattlePokemon pkmn = pfp.PokemonTrainer.GetPokemon(pfp.Pokemon);
                    HidePokemon(pkmn, pfp.OldPosition);
                    if (pkmn.Trainer == Trainer)
                    {
                        UpdateFriendshipForFaint(pkmn);
                    }
                    PlayCry(pkmn);
                    break;
                }
                case PBEPkmnFormChangedPacket pfcp:
                {
                    PBEBattlePokemon pkmn = pfcp.PokemonTrainer.GetPokemon(pfcp.Pokemon);
                    SetSeen(pkmn);
                    UpdatePokemon(pkmn, true, true, true, false, true, false);
                    break;
                }
                case PBEPkmnHPChangedPacket phcp:
                {
                    PBEBattlePokemon pkmn = phcp.PokemonTrainer.GetPokemon(phcp.Pokemon);
                    UpdateAnimationSpeed(pkmn);
                    UpdatePokemon(pkmn, true, false, false, false, false, false);
                    break;
                }
                case PBEPkmnLevelChangedPacket plcp:
                {
                    PBEBattlePokemon pokemon = plcp.PokemonTrainer.GetPokemon(plcp.Pokemon);
                    if (pokemon.FieldPosition != PBEFieldPosition.None)
                    {
                        UpdatePokemon(pokemon, true, false, false, false, false, false);
                    }
                    UpdateFriendshipForLevelUp(pokemon);
                    break;
                }
                case PBEStatus1Packet s1p:
                {
                    PBEBattlePokemon status1Receiver = s1p.Status1ReceiverTrainer.GetPokemon(s1p.Status1Receiver);
                    UpdateAnimationSpeed(status1Receiver);
                    UpdatePokemon(status1Receiver, true, false, false, false, false, false);
                    break;
                }
                case PBEStatus2Packet s2p:
                {
                    PBEBattlePokemon status2Receiver = s2p.Status2ReceiverTrainer.GetPokemon(s2p.Status2Receiver);
                    switch (s2p.Status2)
                    {
                        case PBEStatus2.Airborne: UpdatePokemon(status2Receiver, false, true, false, false, false, true); break;
                        case PBEStatus2.Disguised:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Ended:
                                {
                                    UpdateDisguisedPID(status2Receiver);
                                    SetSeen(status2Receiver);
                                    UpdatePokemon(status2Receiver, true, true, true, false, true, false);
                                    break;
                                }
                            }
                            break;
                        }
                        case PBEStatus2.ShadowForce: UpdatePokemon(status2Receiver, false, true, false, false, false, true); break;
                        case PBEStatus2.Substitute:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added:
                                case PBEStatusAction.Ended: UpdatePokemon(status2Receiver, false, true, true, true, false, true); break;
                            }
                            break;
                        }
                        case PBEStatus2.Transformed:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: UpdatePokemon(status2Receiver, false, true, true, false, true, false); break;
                            }
                            break;
                        }
                        case PBEStatus2.Underground: UpdatePokemon(status2Receiver, false, true, false, false, false, true); break;
                        case PBEStatus2.Underwater: UpdatePokemon(status2Receiver, false, true, false, false, false, true); break;
                    }
                    break;
                }
                case PBEPkmnSwitchInPacket psip:
                {
                    PBETrainer trainer = psip.Trainer;
                    foreach (PBEPkmnAppearedInfo info in psip.SwitchIns)
                    {
                        PBEBattlePokemon pkmn = trainer.GetPokemon(info.Pokemon);
                        UpdateDisguisedPID(pkmn);
                        SetSeen(pkmn);
                        ShowPokemon(pkmn);
                        PlayCry(pkmn);
                    }
                    break;
                }
                case PBEPkmnSwitchOutPacket psop:
                {
                    PBEBattlePokemon pkmn = psop.PokemonTrainer.GetPokemon(psop.Pokemon);
                    HidePokemon(pkmn, psop.OldPosition);
                    break;
                }
                case PBEWildPkmnAppearedPacket wpap:
                {
                    PBETrainer trainer = Battle.Teams[1].Trainers[0];
                    foreach (PBEPkmnAppearedInfo info in wpap.Pokemon)
                    {
                        PBEBattlePokemon pkmn = trainer.GetPokemon(info.Pokemon);
                        UpdateDisguisedPID(pkmn);
                        SetSeen(pkmn);
                        ShowWildPokemon(pkmn);
                        PlayCry(pkmn);
                    }
                    break;
                }
                /*
                case PBEWeatherPacket wp:
                {
                    switch (wp.WeatherAction)
                    {
                        case PBEWeatherAction.Added:
                        case PBEWeatherAction.Ended: BattleView.Field.UpdateWeather(); break;
                        case PBEWeatherAction.CausedDamage: break;
                    }
                    break;
                }*/
            }

            // Packets that change the message
            string message = null;
            switch (packet)
            {
                case PBEFleeFailedPacket ffp:
                {
                    PBETrainer t = ffp.PokemonTrainer;
                    if (t == Trainer)
                    {
                        message = "Couldn't get away!";
                        break;
                    }
                    break;
                }
                case PBEBattleResultPacket brp:
                {
                    switch (brp.BattleResult)
                    {
                        case PBEBattleResult.Team0Win:
                        {
                            if (Battle.BattleType == PBEBattleType.Trainer)
                            {
                                message = _trainerDefeatText;
                            }
                            break;
                        }
                    }
                    break;
                }
            }

            // No custom message, so get the default one
            if (message is null)
            {
                message = PBEBattle.GetDefaultMessage(Battle, packet, userTrainer: Trainer);
            }
            // No message, so return
            if (string.IsNullOrEmpty(message))
            {
                ResumeBattleThread();
                return;
            }
            // Print message
            SetMessage(message, ResumeBattleThread);
            return;
        }

        #endregion
    }
}
