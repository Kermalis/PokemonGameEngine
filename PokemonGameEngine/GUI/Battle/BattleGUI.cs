using Kermalis.PokemonBattleEngine.AI;
using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Packets;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Pkmn;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Sound;
using Kermalis.PokemonGameEngine.UI;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed partial class BattleGUI
    {
        public static BattleGUI Instance { get; private set; }

        private const int AutoAdvanceTicks = Program.NumTicksPerSecond * 3; // 3 seconds
        private const string ThreadName = "Battle Thread";
        private readonly Image _battleBackground;

        private Action _onClosed;
        private FadeColorTransition _fadeTransition;

        public readonly PBEBattle Battle;
        private Thread _battleThread;
        private bool _pauseBattleThread;
        public readonly SpritedBattlePokemonParty[] SpritedParties;
        public readonly PBETrainer Trainer;

        private Window _stringWindow;
        private StringPrinter _stringPrinter;
        private int _autoAdvanceTimer;

        private ActionsGUI _actionsGUI;

        public BattleGUI(PBEBattle battle, Action onClosed, IReadOnlyList<Party> trainerParties)
            : this(battle.BattleFormat) // Init field controller
        {
            Battle = battle;
            Trainer = battle.Trainers[0];
            _battleBackground = Image.LoadOrGet($"GUI.Battle.Background.BG_{battle.BattleTerrain}_{battle.BattleFormat}.png");
            SpritedParties = new SpritedBattlePokemonParty[battle.Trainers.Count];
            for (int i = 0; i < battle.Trainers.Count; i++)
            {
                PBETrainer trainer = battle.Trainers[i];
                SpritedParties[i] = new SpritedBattlePokemonParty(trainer.Party, trainerParties[i], IsBackImage(trainer.Team), ShouldUseKnownInfo(trainer), this);
            }
            _onClosed = onClosed;
            battle.OnNewEvent += SinglePlayerBattle_OnNewEvent;
            battle.OnStateChanged += SinglePlayerBattle_OnStateChanged;

            Instance = this;
        }

        public unsafe void FadeIn()
        {
            OverworldGUI.ProcessDayTint(true); // Catch up time
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeInBattle);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void TransitionOut()
        {
            SoundControl.FadeOutBattleBGMToOverworldBGM();
            _fadeTransition = new FadeToColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeOutBattle);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private void SinglePlayerBattle_OnNewEvent(PBEBattle battle, IPBEPacket packet)
        {
            ProcessPacket(packet);
            if (_pauseBattleThread)
            {
                try
                {
                    Thread.Sleep(Timeout.Infinite);
                }
                catch (ThreadInterruptedException) { }
            }
        }
        private void SinglePlayerBattle_OnStateChanged(PBEBattle battle)
        {
            switch (battle.BattleState)
            {
                case PBEBattleState.Ended:
                {
                    // Copy our Pokémon back from battle, update teammates, update wild Pokémon
                    // Could technically only update what we need (like caught mon, roaming mon, and following partners)
                    for (int i = 0; i < SpritedParties.Length; i++)
                    {
                        SpritedBattlePokemonParty p = SpritedParties[i];
                        p.UpdateToParty(i == Trainer?.Id);
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
                        Game.Instance.Save.GivePokemon(pkmn);
                    }
                    // Pokerus
                    Pokerus.TryCreatePokerus(Game.Instance.Save.PlayerParty);
                    Pokerus.TrySpreadPokerus(Game.Instance.Save.PlayerParty);
                    // Fade out (temporarily until capture screen exists)
                    TransitionOut();
                    break;
                }
                case PBEBattleState.ReadyToRunSwitches:
                {
                    _battleThread = new Thread(battle.RunSwitches) { Name = ThreadName };
                    _battleThread.Start();
                    break;
                }
                case PBEBattleState.ReadyToRunTurn:
                {
                    _battleThread = new Thread(battle.RunTurn) { Name = ThreadName };
                    _battleThread.Start();
                    break;
                }
            }
        }

        private void AwakenBattleThread()
        {
            _pauseBattleThread = false;
            if (_battleThread.IsAlive)
            {
                _battleThread.Interrupt();
            }
        }
        private unsafe void OnPartyReplacementClosed()
        {
            OverworldGUI.ProcessDayTint(true); // Catch up time
            _fadeTransition = new FadeFromColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeFromPartyReplacement);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        public void AddMessage(string message, bool staticMsg)
        {
            _stringPrinter?.Close();
            _stringPrinter = null;
            if (!(message is null))
            {
                _stringPrinter = new StringPrinter(_stringWindow, message, 0.1f, 0.01f, Font.Default, Font.DefaultWhite_I);
                if (staticMsg)
                {
                    Game.Instance.SetCallback(CB_ReadOutStaticMessage);
                }
                else
                {
                    _pauseBattleThread = true;
                    Game.Instance.SetCallback(CB_ReadOutMessage);
                }
            }
        }
        public void SetMessageWindowVisibility(bool invisible)
        {
            _stringWindow.IsInvisible = invisible;
        }

        private unsafe void CB_FadeInBattle()
        {
            OverworldGUI.ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _stringWindow = new Window(0, 0.79f, 1, 0.16f, RenderUtils.Color(49, 49, 49, 128));
                _battleThread = new Thread(Battle.Begin) { Name = ThreadName };
                _battleThread.Start();
                Game.Instance.SetCallback(CB_LogicTick);
                Game.Instance.SetRCallback(RCB_RenderTick);
            }
        }
        private unsafe void CB_FadeOutBattle()
        {
            OverworldGUI.ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _stringPrinter?.Close();
                _stringPrinter = null;
                _stringWindow.Close();
                _stringWindow = null;
                _actionsGUI?.Dispose();
                _actionsGUI = null;
                _onClosed.Invoke();
                _onClosed = null;
                Instance = null;
            }
        }
        private void CB_FadeToPartyForReplacement()
        {
            OverworldGUI.ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                SetMessageWindowVisibility(true);
                _ = new PartyGUI(SpritedParties[Trainer.Id], PartyGUI.Mode.BattleReplace, OnPartyReplacementClosed);
            }
        }
        private unsafe void CB_FadeFromPartyReplacement()
        {
            OverworldGUI.ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                SetMessageWindowVisibility(false);
                Game.Instance.SetCallback(CB_LogicTick);
                Game.Instance.SetRCallback(RCB_RenderTick);
                new Thread(() => Trainer.SelectSwitchesIfValid(Switches)) { Name = ThreadName }.Start();
            }
        }
        private void CB_ReadOutMessage()
        {
            OverworldGUI.ProcessDayTint(false);
            _stringPrinter.LogicTick();
            if (_stringPrinter.IsEnded)
            {
                if (_stringPrinter.IsDone || ++_autoAdvanceTimer >= AutoAdvanceTicks)
                {
                    _autoAdvanceTimer = 0;
                    // TODO: Here we should branch off and do learning move logic
                    AwakenBattleThread();
                    Game.Instance.SetCallback(CB_LogicTick);
                }
            }
        }
        private void CB_ReadOutStaticMessage()
        {
            OverworldGUI.ProcessDayTint(false);
            _stringPrinter.LogicTick();
            if (_stringPrinter.IsEnded)
            {
                _actionsGUI.SetCallbacksForAllChoices();
            }
        }
        private void CB_LogicTick()
        {
            OverworldGUI.ProcessDayTint(false);
        }

        private static unsafe void RenderPkmn(uint* bmpAddress, int bmpWidth, int bmpHeight, PkmnPosition pos, bool ally)
        {
            SpritedBattlePokemon sPkmn = pos.SPkmn;
            AnimatedImage img = sPkmn.AnimImage;
            int width = img.Width;
            int height = img.Height;
            if (ally)
            {
                width *= 2;
                height *= 2;
            }
            img.DrawSizedOn(bmpAddress, bmpWidth, bmpHeight,
                RenderUtils.GetCoordinatesForCentering(bmpWidth, width, pos.MonX), RenderUtils.GetCoordinatesForEndAlign(bmpHeight, height, pos.MonY), width, height);
        }
        private static unsafe void RenderPkmnInfo(uint* bmpAddress, int bmpWidth, int bmpHeight, PkmnPosition pos)
        {
            pos.SPkmn.InfoBarImg.DrawOn(bmpAddress, bmpWidth, bmpHeight, pos.BarX, pos.BarY);
        }

        private unsafe void RCB_Fading(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RCB_RenderTick(bmpAddress, bmpWidth, bmpHeight);
            _fadeTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        }
        public unsafe void RCB_RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            AnimatedImage.UpdateCurrentFrameForAll();
            _battleBackground.DrawSizedOn(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight);
            void DoTeam(int i, bool info)
            {
                foreach (PkmnPosition p in _positions[i])
                {
                    bool ally = i == 0;
                    if (info)
                    {
                        if (p.InfoVisible)
                        {
                            RenderPkmnInfo(bmpAddress, bmpWidth, bmpHeight, p);
                        }
                    }
                    else if (p.PkmnVisible)
                    {
                        RenderPkmn(bmpAddress, bmpWidth, bmpHeight, p, ally);
                    }
                }
            }
            DoTeam(1, false);
            DoTeam(0, false);

            if (Overworld.ShouldRenderDayTint())
            {
                DayTint.Render(bmpAddress, bmpWidth, bmpHeight);
            }

            DoTeam(1, true);
            DoTeam(0, true);

            if (_stringPrinter != null)
            {
                _stringWindow.Render(bmpAddress, bmpWidth, bmpHeight);
            }
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
        private void UpdateAnimationSpeed(PBEBattlePokemon pkmn)
        {
            SpritedBattlePokemon sPkmn = SpritedParties[pkmn.Trainer.Id][pkmn];
            sPkmn.UpdateAnimationSpeed();
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
        private static float GetCryPanpot(PBEBattlePokemon pkmn)
        {
            return pkmn.Team.Id == 0 ? -0.35f : 0.35f;
        }

        #region Actions
        private readonly List<PBEBattlePokemon> _actions = new(3);
        public List<PBEBattlePokemon> StandBy { get; } = new(3);
        public unsafe void ActionsLoop(bool begin)
        {
            if (begin)
            {
                foreach (PBEBattlePokemon pkmn in Trainer.Party)
                {
                    pkmn.TurnAction = null;
                }
                _actions.Clear();
                _actions.AddRange(Trainer.ActiveBattlersOrdered);
                StandBy.Clear();
            }
            int i = _actions.FindIndex(p => p.TurnAction == null);
            if (i == -1)
            {
                RemoveActionsGUIAndSetCallbacks();
                new Thread(() => Trainer.SelectActionsIfValid(_actions.Select(p => p.TurnAction).ToArray())) { Name = ThreadName }.Start();
            }
            else
            {
                AddMessage($"What will {_actions[i].Nickname} do?", true);
                SpritedBattlePokemonParty party = SpritedParties[Trainer.Id];
                _actionsGUI?.Dispose();
                _actionsGUI = new ActionsGUI(party, _actions[i]);
                // For i == 0, while the message is being read, the R callback is already RCB_RenderTick
                // For i != 0, while the message is being read, the R callback is _actionsGUI.RCB_Targets, so we need to update it
                if (i != 0)
                {
                    Game.Instance.SetRCallback(RCB_RenderTick);
                }
            }
        }
        public void Flee()
        {
            RemoveActionsGUIAndSetCallbacks();
            new Thread(() => Trainer.SelectFleeIfValid()) { Name = ThreadName }.Start();
        }

        public List<PBESwitchIn> Switches { get; } = new(3);
        public byte SwitchesRequired;
        public List<PBEFieldPosition> PositionStandBy { get; } = new(3);
        private unsafe void SetUpBattleReplacementFade()
        {
            // TODO: Run from wild?
            Switches.Clear();
            StandBy.Clear();
            PositionStandBy.Clear();
            _fadeTransition = new FadeToColorTransition(500, 0);
            Game.Instance.SetCallback(CB_FadeToPartyForReplacement);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void RemoveActionsGUIAndSetCallbacks()
        {
            _actionsGUI.Dispose();
            _actionsGUI = null;
            Game.Instance.SetCallback(CB_LogicTick);
            Game.Instance.SetRCallback(RCB_RenderTick);
        }
        #endregion

        #region Packet Processing
        private void ProcessPacket(IPBEPacket packet)
        {
            switch (packet)
            {
                case PBEMoveLockPacket _:
                case PBEMovePPChangedPacket _:
                case PBEIllusionPacket _:
                case PBETransformPacket _:
                case PBEBattlePacket _:
                case PBETurnBeganPacket _: return;
                case PBEActionsRequestPacket arp:
                {
                    PBETrainer t = arp.Trainer;
                    if (t == Trainer)
                    {
                        ActionsLoop(true);
                    }
                    else
                    {
                        // If the team is wild, no flees are allowed by default
                        new Thread(t.CreateAIActions) { Name = ThreadName }.Start();
                    }
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
                        new Thread(t.CreateAISwitches) { Name = ThreadName }.Start();
                    }
                    return;
                }
                case PBEFleeFailedPacket ffp:
                {
                    PBETrainer t = ffp.PokemonTrainer;
                    if (t == Trainer)
                    {
                        AddMessage("Couldn't get away!", false);
                        return;
                    }
                    break; // Use default message otherwise
                }
                case PBEAutoCenterPacket acp:
                {
                    PBEBattlePokemon pkmn0 = acp.Pokemon0Trainer.TryGetPokemon(acp.Pokemon0);
                    PBEBattlePokemon pkmn1 = acp.Pokemon1Trainer.TryGetPokemon(acp.Pokemon1);
                    MovePokemon(pkmn0, acp.Pokemon0OldPosition);
                    MovePokemon(pkmn1, acp.Pokemon1OldPosition);
                    break;
                }
                case PBEPkmnEXPChangedPacket pecp:
                {
                    PBEBattlePokemon pokemon = pecp.PokemonTrainer.TryGetPokemon(pecp.Pokemon);
                    if (pokemon.FieldPosition != PBEFieldPosition.None)
                    {
                        UpdatePokemon(pokemon, true, false);
                    }
                    break;
                }
                case PBEPkmnFaintedPacket pfp:
                {
                    PBEBattlePokemon pkmn = pfp.PokemonTrainer.TryGetPokemon(pfp.Pokemon);
                    HidePokemon(pkmn, pfp.OldPosition);
                    if (pkmn.Trainer == Trainer)
                    {
                        UpdateFriendshipForFaint(pkmn);
                    }
                    SoundControl.PlayCry(pkmn.KnownSpecies, pkmn.KnownForm, pan: GetCryPanpot(pkmn)); // TODO: Distort
                    break;
                }
                case PBEPkmnFormChangedPacket pfcp:
                {
                    PBEBattlePokemon pkmn = pfcp.PokemonTrainer.TryGetPokemon(pfcp.Pokemon);
                    SetSeen(pkmn);
                    UpdatePokemon(pkmn, true, true);
                    break;
                }
                case PBEPkmnHPChangedPacket phcp:
                {
                    PBEBattlePokemon pkmn = phcp.PokemonTrainer.TryGetPokemon(phcp.Pokemon);
                    UpdateAnimationSpeed(pkmn);
                    UpdatePokemon(pkmn, true, false);
                    break;
                }
                case PBEPkmnLevelChangedPacket plcp:
                {
                    PBEBattlePokemon pokemon = plcp.PokemonTrainer.TryGetPokemon(plcp.Pokemon);
                    if (pokemon.FieldPosition != PBEFieldPosition.None)
                    {
                        UpdatePokemon(pokemon, true, false);
                    }
                    UpdateFriendshipForLevelUp(pokemon);
                    break;
                }
                case PBEStatus1Packet s1p:
                {
                    PBEBattlePokemon status1Receiver = s1p.Status1ReceiverTrainer.TryGetPokemon(s1p.Status1Receiver);
                    UpdateAnimationSpeed(status1Receiver);
                    UpdatePokemon(status1Receiver, true, false);
                    break;
                }
                case PBEStatus2Packet s2p:
                {
                    PBEBattlePokemon status2Receiver = s2p.Status2ReceiverTrainer.TryGetPokemon(s2p.Status2Receiver);
                    switch (s2p.Status2)
                    {
                        case PBEStatus2.Airborne: UpdatePokemon(status2Receiver, false, true); break;
                        case PBEStatus2.Disguised:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Ended:
                                {
                                    UpdateDisguisedPID(status2Receiver);
                                    SetSeen(status2Receiver);
                                    UpdatePokemon(status2Receiver, true, true);
                                    break;
                                }
                            }
                            break;
                        }
                        case PBEStatus2.ShadowForce: UpdatePokemon(status2Receiver, false, true); break;
                        case PBEStatus2.Substitute:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added:
                                case PBEStatusAction.Ended: UpdatePokemon(status2Receiver, false, true); break;
                            }
                            break;
                        }
                        case PBEStatus2.Transformed:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: UpdatePokemon(status2Receiver, false, true); break;
                            }
                            break;
                        }
                        case PBEStatus2.Underground: UpdatePokemon(status2Receiver, false, true); break;
                        case PBEStatus2.Underwater: UpdatePokemon(status2Receiver, false, true); break;
                    }
                    break;
                }
                case PBEPkmnSwitchInPacket psip:
                {
                    PBETrainer trainer = psip.Trainer;
                    foreach (PBEPkmnAppearedInfo info in psip.SwitchIns)
                    {
                        PBEBattlePokemon pkmn = trainer.TryGetPokemon(info.Pokemon);
                        UpdateDisguisedPID(pkmn);
                        SetSeen(pkmn);
                        ShowPokemon(pkmn);
                        SoundControl.PlayCry(pkmn.KnownSpecies, pkmn.KnownForm, pan: GetCryPanpot(pkmn));
                    }
                    break;
                }
                case PBEPkmnSwitchOutPacket psop:
                {
                    PBEBattlePokemon pkmn = psop.PokemonTrainer.TryGetPokemon(psop.Pokemon);
                    HidePokemon(pkmn, psop.OldPosition);
                    break;
                }
                case PBEWildPkmnAppearedPacket wpap:
                {
                    PBETrainer trainer = Battle.Teams[1].Trainers[0];
                    foreach (PBEPkmnAppearedInfo info in wpap.Pokemon)
                    {
                        PBEBattlePokemon pkmn = trainer.TryGetPokemon(info.Pokemon);
                        UpdateDisguisedPID(pkmn);
                        SetSeen(pkmn);
                        ShowWildPokemon(pkmn);
                        SoundControl.PlayCry(pkmn.KnownSpecies, pkmn.KnownForm, pan: GetCryPanpot(pkmn));
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
            string message = PBEBattle.GetDefaultMessage(Battle, packet, userTrainer: Trainer);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            AddMessage(message, false);
            return;
        }
        #endregion
    }
}
