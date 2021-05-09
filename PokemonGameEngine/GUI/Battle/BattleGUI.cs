using Kermalis.PokemonBattleEngine.AI;
using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Packets;
using Kermalis.PokemonGameEngine.Core;
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

        private readonly PBEBattle _battle;
        private Thread _battleThread;
        private bool _pauseBattleThread;
        public readonly SpritedBattlePokemonParty[] _spritedParties;
        private readonly PBETrainer _trainer;

        private Window _stringWindow;
        private StringPrinter _stringPrinter;
        private int _autoAdvanceTimer;

        private ActionsGUI _actionsGUI;

        public BattleGUI(PBEBattle battle, Action onClosed, IReadOnlyList<Party> trainerParties)
            : this(battle.BattleFormat) // Init field controller
        {
            _battle = battle;
            _trainer = battle.Trainers[0];
            _battleBackground = Image.LoadOrGet($"GUI.Battle.Background.BG_{battle.BattleTerrain}_{battle.BattleFormat}.png");
            _spritedParties = new SpritedBattlePokemonParty[battle.Trainers.Count];
            for (int i = 0; i < battle.Trainers.Count; i++)
            {
                PBETrainer trainer = battle.Trainers[i];
                _spritedParties[i] = new SpritedBattlePokemonParty(trainer.Party, trainerParties[i], IsBackImage(trainer.Team), ShouldUseKnownInfo(trainer), this);
            }
            _onClosed = onClosed;
            battle.OnNewEvent += SinglePlayerBattle_OnNewEvent;
            battle.OnStateChanged += SinglePlayerBattle_OnStateChanged;

            Instance = this;
        }

        public unsafe void FadeIn()
        {
            OverworldGUI.ProcessDayTint(true); // Catch up time
            _fadeTransition = new FadeFromColorTransition(20, 0);
            Game.Instance.SetCallback(CB_FadeInBattle);
            Game.Instance.SetRCallback(RCB_Fading);
        }

        private unsafe void TransitionOut()
        {
            SoundControl.FadeOutBattleBGMToOverworldBGM();
            _fadeTransition = new FadeToColorTransition(20, 0);
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
                    foreach (SpritedBattlePokemonParty p in _spritedParties)
                    {
                        p.UpdateToParty(); // Copy our Pokémon back from battle, update teammates, update wild Pokémon
                    }
                    Game.Instance.Save.PlayerInventory.FromPBEInventory(_trainer.Inventory);
                    if (_battle.BattleResult == PBEBattleResult.WildCapture)
                    {
                        Game.Instance.Save.GameStats[GameStat.PokemonCaptures]++;
                        PBETrainer wildTrainer = _battle.Teams[1].Trainers[0];
                        SpritedBattlePokemonParty sp = _spritedParties[wildTrainer.Id];
                        PBEBattlePokemon wildPkmn = wildTrainer.ActiveBattlers.Single();
                        PartyPokemon pkmn = sp[wildPkmn].PartyPkmn;
                        pkmn.UpdateFromBattle_Caught(wildPkmn);
                        Game.Instance.Save.GivePokemon(pkmn);
                    }
                    Pokerus.TryCreatePokerus(Game.Instance.Save.PlayerParty);
                    Pokerus.TrySpreadPokerus(Game.Instance.Save.PlayerParty);
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

        private void AddMessage(string message, bool staticMsg)
        {
            _stringPrinter?.Close();
            _stringPrinter = null;
            if (!(message is null))
            {
                _stringPrinter = new StringPrinter(_stringWindow, message, 0.1f, 0.01f, Font.Default, Font.DefaultWhite);
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

        private unsafe void CB_FadeInBattle()
        {
            OverworldGUI.ProcessDayTint(false);
            if (_fadeTransition.IsDone)
            {
                _fadeTransition = null;
                _stringWindow = new Window(0, 0.79f, 1, 0.16f, RenderUtils.Color(49, 49, 49, 128));
                _battleThread = new Thread(_battle.Begin) { Name = ThreadName };
                _battleThread.Start();
                Game.Instance.SetCallback(CB_LogicTick);
                Game.Instance.SetRCallback(RCB_RenderTick);
            }
        }
        private unsafe void CB_FadeOutBattle()
        {
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
        private void CB_ReadOutMessage()
        {
            OverworldGUI.ProcessDayTint(false);
            _stringPrinter.LogicTick();
            if (_stringPrinter.IsEnded)
            {
                if (_stringPrinter.IsDone || ++_autoAdvanceTimer >= AutoAdvanceTicks)
                {
                    _autoAdvanceTimer = 0;
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
                Game.Instance.SetCallback(CB_LogicTick);
            }
        }
        private void CB_LogicTick()
        {
            OverworldGUI.ProcessDayTint(false);
            _actionsGUI?.LogicTick();
        }

        private unsafe void RenderPkmn(uint* bmpAddress, int bmpWidth, int bmpHeight, PkmnPosition pos, bool ally)
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
            img.DrawSizedOn(bmpAddress, bmpWidth, bmpHeight, RenderUtils.GetCoordinatesForCentering(bmpWidth, width, pos.MonX), (int)(bmpHeight * pos.MonY) - height, width, height);
        }
        private unsafe void RenderPkmnInfo(uint* bmpAddress, int bmpWidth, int bmpHeight, PkmnPosition pos)
        {
            pos.SPkmn.InfoBarImg.DrawOn(bmpAddress, bmpWidth, bmpHeight, pos.BarX, pos.BarY);
        }

        private unsafe void RCB_Fading(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RCB_RenderTick(bmpAddress, bmpWidth, bmpHeight);
            _fadeTransition.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        }
        private unsafe void RCB_RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
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

            _actionsGUI?.RenderTick(bmpAddress, bmpWidth, bmpHeight);
        }

        private void SetSeen(PBEBattlePokemon pkmn)
        {
            if (pkmn.Trainer == _trainer)
            {
                return;
            }
            PartyPokemon pPkmn = _spritedParties[pkmn.Trainer.Id][pkmn].PartyPkmn;
            Game.Instance.Save.Pokedex.SetSeen(pkmn.KnownSpecies, pkmn.KnownForm, pkmn.KnownGender, pkmn.KnownShiny, pPkmn.PID); // TODO: #49 (Spinda spots disguise)
        }
        private void UpdateAnimationSpeed(PBEBattlePokemon pkmn)
        {
            SpritedBattlePokemon sPkmn = _spritedParties[pkmn.Trainer.Id][pkmn];
            sPkmn.UpdateAnimationSpeed();
        }
        private void UpdateFriendshipForFaint(PBEBattlePokemon pkmn)
        {
            byte oppLevel = pkmn.Team.OpposingTeam.ActiveBattlers.Max(p => p.Level);
            PartyPokemon pp = _spritedParties[pkmn.Trainer.Id][pkmn].PartyPkmn;
            Friendship.Event e = oppLevel - pkmn.Level >= 30 ? Friendship.Event.Faint_GE30 : Friendship.Event.Faint_L30;
            Friendship.AdjustFriendship(pkmn, pp, e);
        }
        private void UpdateFriendshipForLevelUp(PBEBattlePokemon pkmn)
        {
            PartyPokemon pp = _spritedParties[pkmn.Trainer.Id][pkmn].PartyPkmn;
            Friendship.AdjustFriendship(pkmn, pp, Friendship.Event.LevelUpBattle);
        }

        #region Actions
        private readonly List<PBEBattlePokemon> _actions = new List<PBEBattlePokemon>(3);
        public List<PBEBattlePokemon> StandBy { get; } = new List<PBEBattlePokemon>(3);
        public void ActionsLoop(bool begin)
        {
            if (begin)
            {
                foreach (PBEBattlePokemon pkmn in _trainer.Party)
                {
                    pkmn.TurnAction = null;
                }
                _actions.Clear();
                _actions.AddRange(_trainer.ActiveBattlers);
                StandBy.Clear();
            }
            int i = _actions.FindIndex(p => p.TurnAction == null);
            if (i == -1)
            {
                _actionsGUI.Dispose();
                _actionsGUI = null;
                new Thread(() => _trainer.SelectActionsIfValid(_actions.Select(p => p.TurnAction).ToArray())) { Name = ThreadName }.Start();
            }
            else
            {
                AddMessage($"What will {_actions[i].Nickname} do?", true);
                SpritedBattlePokemonParty party = _spritedParties[_trainer.Id];
                _actionsGUI?.Dispose();
                _actionsGUI = new ActionsGUI(this, party, _actions[i]);
            }
        }
        public void Flee()
        {
            _actionsGUI.Dispose();
            _actionsGUI = null;
            new Thread(() => _trainer.SelectFleeIfValid()) { Name = ThreadName }.Start();
        }

        public List<PBESwitchIn> Switches { get; } = new List<PBESwitchIn>(3);
        private byte _switchesRequired;
        public List<PBEFieldPosition> PositionStandBy { get; } = new List<PBEFieldPosition>(3);
        public void SwitchesLoop(bool begin)
        {
            new Thread(_trainer.CreateAISwitches) { Name = ThreadName }.Start();
            return;
            // TODO: LMAOOOOOOOOO
            if (begin)
            {
                Switches.Clear();
                StandBy.Clear();
                PositionStandBy.Clear();
            }
            else
            {
                _switchesRequired--;
            }
            if (_switchesRequired == 0)
            {
                new Thread(() => _trainer.SelectSwitchesIfValid(Switches)) { Name = ThreadName }.Start();
            }
            else
            {
                AddMessage($"You must send in {_switchesRequired} Pokémon.", true);
                //BattleView.Actions.DisplaySwitches();
            }
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
                    if (t == _trainer)
                    {
                        ActionsLoop(true);
                    }
                    else
                    {
                        new Thread(t.CreateAIActions) { Name = ThreadName }.Start();
                    }
                    return;
                }
                case PBESwitchInRequestPacket sirp:
                {
                    PBETrainer t = sirp.Trainer;
                    if (t == _trainer)
                    {
                        _switchesRequired = sirp.Amount;
                        SwitchesLoop(true);
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
                    if (t == _trainer)
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
                case PBEPkmnFaintedPacket pfp:
                {
                    PBEBattlePokemon pkmn = pfp.PokemonTrainer.TryGetPokemon(pfp.Pokemon);
                    HidePokemon(pkmn, pfp.OldPosition);
                    if (pkmn.Trainer == _trainer)
                    {
                        UpdateFriendshipForFaint(pkmn);
                    }
                    break;
                }
                case PBEPkmnFormChangedPacket pfcp:
                {
                    PBEBattlePokemon pkmn = pfcp.PokemonTrainer.TryGetPokemon(pfcp.Pokemon);
                    SetSeen(pkmn);
                    UpdatePokemon(pkmn, false, true);
                    break;
                }
                case PBEPkmnHPChangedPacket phcp:
                {
                    PBEBattlePokemon pkmn = phcp.PokemonTrainer.TryGetPokemon(phcp.Pokemon);
                    UpdateAnimationSpeed(pkmn);
                    UpdatePokemon(pkmn, true, false);
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
                    foreach (PBEPkmnAppearedInfo info in psip.SwitchIns)
                    {
                        PBEBattlePokemon pkmn = psip.Trainer.TryGetPokemon(info.Pokemon);
                        SetSeen(pkmn);
                        ShowPokemon(pkmn);
                        SoundControl.Debug_PlayCry(pkmn.KnownSpecies, pkmn.KnownForm);
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
                    PBETrainer trainer = _battle.Teams[1].Trainers[0];
                    foreach (PBEPkmnAppearedInfo info in wpap.Pokemon)
                    {
                        PBEBattlePokemon pkmn = trainer.TryGetPokemon(info.Pokemon);
                        SetSeen(pkmn);
                        ShowWildPokemon(pkmn);
                        SoundControl.Debug_PlayCry(pkmn.KnownSpecies, pkmn.KnownForm);
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
            string message = PBEBattle.GetDefaultMessage(_battle, packet, userTrainer: _trainer);
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
