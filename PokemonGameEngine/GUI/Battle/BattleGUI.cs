using Kermalis.PokemonBattleEngine.AI;
using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Packets;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed partial class BattleGUI
    {
        private const int WaitMilliseconds = 1750;
        private const string ThreadName = "Battle Thread"; // TODO: Put this on LogicTick somehow so it can be locked with render thread
        private readonly Sprite _battleBackground;

        private Action _onClosed;

        private readonly PBEBattle _battle;
        public readonly SpritedBattlePokemonParty[] _spritedParties;
        private readonly PBETrainer _trainer;
        private string _message;
        private ActionsGUI _actionsGUI;

        public BattleGUI(PBEBattle battle, Action onClosed, IReadOnlyList<Party> trainerParties)
            : this(battle.BattleFormat) // Init field controller
        {
            _battle = battle;
            _trainer = battle.Trainers[0];
            _battleBackground = Sprite.LoadOrGet($"GUI.Battle.Background.BG_{battle.BattleTerrain}_{battle.BattleFormat}.png");
            _spritedParties = new SpritedBattlePokemonParty[battle.Trainers.Count];
            for (int i = 0; i < battle.Trainers.Count; i++)
            {
                PBETrainer trainer = battle.Trainers[i];
                _spritedParties[i] = new SpritedBattlePokemonParty(trainer.Party, trainerParties[i], IsBackSprite(trainer.Team), ShouldUseKnownInfo(trainer), this);
            }
            _onClosed = onClosed;
            battle.OnNewEvent += SinglePlayerBattle_OnNewEvent;
            battle.OnStateChanged += SinglePlayerBattle_OnStateChanged;
        }

        private void TransitionOut()
        {
            _onClosed.Invoke();
            _onClosed = null;
            if (_actionsGUI != null)
            {
                _actionsGUI.Dispose();
                _actionsGUI = null;
            }
        }

        private void SinglePlayerBattle_OnNewEvent(PBEBattle battle, IPBEPacket packet)
        {
            if (!ProcessPacket(packet))
            {
                Thread.Sleep(WaitMilliseconds);
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
                    if (_battle.BattleResult == PBEBattleResult.WildCapture)
                    {
                        PBETrainer wildTrainer = _battle.Teams[1].Trainers[0];
                        SpritedBattlePokemonParty sp = _spritedParties[wildTrainer.Id];
                        PBEBattlePokemon wildPkmn = wildTrainer.ActiveBattlers.Single();
                        PartyPokemon pkmn = sp[wildPkmn].PartyPkmn;
                        pkmn.UpdateFromBattle_Caught(wildPkmn);
                        Game.Instance.Save.GivePokemon(pkmn);
                    }
                    TransitionOut();
                    break;
                }
                case PBEBattleState.ReadyToRunSwitches: new Thread(battle.RunSwitches) { Name = ThreadName }.Start(); break;
                case PBEBattleState.ReadyToRunTurn: new Thread(battle.RunTurn) { Name = ThreadName }.Start(); break;
            }
        }

        private void AddMessage(string message)
        {
            _message = message;
        }

        public void LogicTick()
        {
            if (_battle.BattleState == PBEBattleState.ReadyToBegin)
            {
                new Thread(_battle.Begin) { Name = ThreadName }.Start();
                return;
            }
            _actionsGUI?.LogicTick();
        }

        private unsafe void RenderPkmn(uint* bmpAddress, int bmpWidth, int bmpHeight, PkmnPosition pos, bool ally)
        {
            SpritedBattlePokemon sPkmn = pos.SPkmn;
            AnimatedSprite sprite = sPkmn.Sprite;
            int width = sprite.Width;
            int height = sprite.Height;
            if (ally)
            {
                width *= 2;
                height *= 2;
            }
            sprite.DrawOn(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * pos.MonX) - (width / 2), (int)(bmpHeight * pos.MonY) - height, width, height);
        }
        private unsafe void RenderPkmnInfo(uint* bmpAddress, int bmpWidth, int bmpHeight, PkmnPosition pos, bool ally)
        {
            Font fontDefault = Font.DefaultSmall;
            SpritedBattlePokemon sPkmn = pos.SPkmn;
            float x = pos.BarX;
            float y = pos.BarY;
            PBEBattlePokemon pkmn = sPkmn.Pkmn;
            fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, x, y + 0.00f, pkmn.KnownNickname, Font.DefaultWhite);
            string prefix = ally ? pkmn.HP.ToString() + "/" + pkmn.MaxHP.ToString() + " - " : string.Empty;
            fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, x, y + 0.04f, prefix + pkmn.HPPercentage.ToString("P2"), Font.DefaultWhite);
            fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, x, y + 0.08f, "Level " + pkmn.Level.ToString(), Font.DefaultWhite);
            fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, x, y + 0.12f, "Status: " + pkmn.Status1.ToString(), Font.DefaultWhite);
            PBEGender gender = pkmn.KnownGender;
            if (gender != PBEGender.Genderless)
            {
                fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, x, y + 0.16f, gender.ToSymbol(), gender == PBEGender.Male ? Font.DefaultMale : Font.DefaultFemale);
            }
            if (!ally && pkmn.IsWild && Game.Instance.Save.Pokedex.IsCaught(pkmn.KnownSpecies))
            {
                fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, x + 0.02f, y + 0.16f, "Caught", Font.DefaultWhite);
            }
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            Font fontDefault = Font.Default;
            uint[] defaultWhite = Font.DefaultWhite;
            _battleBackground.DrawOn(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight);
            void DoTeam(int i, bool info)
            {
                foreach (PkmnPosition p in _positions[i])
                {
                    bool ally = i == 0;
                    if (info)
                    {
                        if (p.InfoVisible)
                        {
                            RenderPkmnInfo(bmpAddress, bmpWidth, bmpHeight, p, ally);
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

            string msg = _message;
            if (msg != null)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0, (int)(bmpHeight * 0.79f), bmpWidth, (int)(bmpHeight * 0.16f), RenderUtils.Color(49, 49, 49, 128));
                fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * 0.10f), (int)(bmpHeight * 0.80f), msg, defaultWhite);
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
            Game.Instance.Save.Pokedex.SetSeen(pkmn.KnownSpecies, pkmn.KnownForm, pkmn.KnownGender, pPkmn.PID); // TODO: #49 (Spinda spots disguise)
        }
        private void UpdateAnimationSpeed(PBEBattlePokemon pkmn)
        {
            SpritedBattlePokemon sPkmn = _spritedParties[pkmn.Trainer.Id][pkmn];
            sPkmn.UpdateAnimationSpeed();
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
                AddMessage($"What will {_actions[i].Nickname} do?");
                SpritedBattlePokemonParty party = _spritedParties[_trainer.Id];
                _actionsGUI?.Dispose();
                _actionsGUI = new ActionsGUI(this, party, party.SpritedParty[i].Pkmn);
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
                AddMessage($"You must send in {_switchesRequired} Pokémon.");
                //BattleView.Actions.DisplaySwitches();
            }
        }
        #endregion

        #region Packet Processing
        private bool ProcessPacket(IPBEPacket packet)
        {
            switch (packet)
            {
                case PBEMoveLockPacket _:
                case PBEMovePPChangedPacket _:
                case PBEIllusionPacket _:
                case PBETransformPacket _:
                case PBEBattlePacket _:
                case PBETurnBeganPacket _: return true;
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
                    return true;
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
                    return true;
                }
                case PBEFleeFailedPacket ffp:
                {
                    PBETrainer t = ffp.PokemonTrainer;
                    if (t == _trainer)
                    {
                        AddMessage("Couldn't get away!");
                        return false;
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
                        ShowPokemon(pkmn); // This will set the info to visible
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
                return true;
            }
            AddMessage(message);
            return false;
        }
        #endregion
    }
}
