using Kermalis.PokemonBattleEngine.AI;
using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Packets;
using Kermalis.PokemonBattleEngine.Utils;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.GUI.Transition;
using Kermalis.PokemonGameEngine.Pkmn;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed class BattleGUI
    {
        private const int WaitMilliseconds = 1750;
        private const string ThreadName = "Battle Thread"; // TODO: Put this on LogicTick somehow so it can be locked with render thread
        private readonly Sprite _battleBackground;

        private const int TransitionDuration = 40;
        private const float TransitionDurationF = TransitionDuration;
        private int _transitionCounter;
        private bool _transitionDone;
        private FadeToColorTransition _battleEndedTransition;
        private Action _onClosed;

        private readonly PBEBattle _battle;
        private readonly SpritedBattlePokemonParty[] _spritedParties;
        private readonly PBETrainer _trainer;
        private string _message;
        private ActionsGUI _actionsGUI;

        public readonly bool IsDarkGrass;
        public readonly bool IsCave;
        public readonly bool IsFishing;
        public readonly bool IsSurfing;
        public readonly bool IsUnderwater;

        public BattleGUI(PBEBattle battle, Action onClosed, IReadOnlyList<Party> trainerParties,
            bool isCave, bool isDarkGrass, bool isFishing, bool isSurfing, bool isUnderwater)
        {
            IsCave = isCave;
            IsDarkGrass = isDarkGrass;
            IsFishing = isFishing;
            IsSurfing = isSurfing;
            IsUnderwater = isUnderwater;
            _battle = battle;
            _trainer = battle.Trainers[0];
            _battleBackground = Sprite.LoadOrGet($"GUI.Battle.Background.BG_{battle.BattleTerrain}_{battle.BattleFormat}.png");
            _spritedParties = new SpritedBattlePokemonParty[battle.Trainers.Count];
            for (int i = 0; i < battle.Trainers.Count; i++)
            {
                _spritedParties[i] = new SpritedBattlePokemonParty(battle.Trainers[i].Party, trainerParties[i]);
            }
            _transitionCounter = TransitionDuration;
            _onClosed = onClosed;
            battle.OnNewEvent += SinglePlayerBattle_OnNewEvent;
            battle.OnStateChanged += SinglePlayerBattle_OnStateChanged;
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
                    void OnBattleEndedTransitionEnded()
                    {
                        _battleEndedTransition = null;
                        if (_actionsGUI != null)
                        {
                            _actionsGUI.Dispose();
                            _actionsGUI = null;
                        }
                        _onClosed.Invoke();
                        _onClosed = null;
                    }
                    foreach (SpritedBattlePokemonParty p in _spritedParties)
                    {
                        p.UpdateToParty(); // Copy our Pokémon back from battle, update teammates, update wild Pokémon
                    }
                    if (_battle.BattleResult == PBEBattleResult.WildCapture)
                    {
                        PBETrainer wildTrainer = _battle.Teams[1].Trainers[0];
                        SpritedBattlePokemonParty sp = _spritedParties[wildTrainer.Id];
                        PBEBattlePokemon wildPkmn = wildTrainer.ActiveBattlers.Single();
                        PartyPokemon pkmn = sp.Party[wildPkmn.Id];
                        pkmn.UpdateFromBattle_Caught(wildPkmn);
                        Game.Instance.Save.GivePokemon(pkmn);
                    }
                    _battleEndedTransition = new FadeToColorTransition(20, 0, OnBattleEndedTransitionEnded);
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
            if (_battleEndedTransition != null)
            {
                return;
            }
            _actionsGUI?.LogicTick();
        }

        private unsafe void RenderPkmn(uint* bmpAddress, int bmpWidth, int bmpHeight, float x, float y, bool ally, SpritedBattlePokemon sPkmn)
        {
            AnimatedSprite sprite = ally ? sPkmn.BackSprite : sPkmn.FrontSprite; // TODO: Substitute
            int width = sprite.Width;
            int height = sprite.Height;
            if (ally)
            {
                width *= 2;
                height *= 2;
            }
            sprite.DrawOn(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * x) - (width / 2), (int)(bmpHeight * y) - height, width, height);
        }
        private unsafe void RenderPkmnInfo(uint* bmpAddress, int bmpWidth, int bmpHeight, float x, float y, bool ally, SpritedBattlePokemon sPkmn)
        {
            Font fontDefault = Font.Default;

            PBEBattlePokemon pkmn = sPkmn.Pkmn;
            fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * x), (int)(bmpHeight * (y + 0.00f)), pkmn.KnownNickname, Font.DefaultWhite);
            string prefix = ally ? pkmn.HP.ToString() + "/" + pkmn.MaxHP.ToString() + " - " : string.Empty;
            fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * x), (int)(bmpHeight * (y + 0.06f)), prefix + pkmn.HPPercentage.ToString("P2"), Font.DefaultWhite);
            fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * x), (int)(bmpHeight * (y + 0.12f)), "Level " + pkmn.Level.ToString(), Font.DefaultWhite);
            fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * x), (int)(bmpHeight * (y + 0.18f)), "Status: " + pkmn.Status1.ToString(), Font.DefaultWhite);
            PBEGender gender = pkmn.KnownGender;
            if (gender != PBEGender.Genderless)
            {
                fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * x), (int)(bmpHeight * (y + 0.24f)), gender.ToSymbol(), gender == PBEGender.Male ? Font.DefaultMale : Font.DefaultFemale);
            }
        }

        public unsafe void RenderTick(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            PBEBattle battle = _battle;

            Font fontDefault = Font.Default;
            uint[] defaultWhite = Font.DefaultWhite;
            _battleBackground.DrawOn(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight);
            SpritedBattlePokemon foe = _spritedParties[1].SpritedParty[0];
            SpritedBattlePokemon ally = _spritedParties[0].SpritedParty[0];
            RenderPkmn(bmpAddress, bmpWidth, bmpHeight, 0.75f, 0.55f, false, foe);
            RenderPkmn(bmpAddress, bmpWidth, bmpHeight, 0.35f, 0.95f, true, ally);

            if (Overworld.ShouldRenderDayTint())
            {
                DayTint.Render(bmpAddress, bmpWidth, bmpHeight);
            }

            if (!_transitionDone)
            {
                float t = _transitionCounter / TransitionDurationF;
                //float t1 = t + 1;
                //_battleBackground.DrawOn(bmpAddress, bmpWidth, bmpHeight, 0, 0, (int)(bmpWidth * t1), (int)(bmpHeight * t1));
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight, (uint)(t * 0xFF) << 24);
                if (--_transitionCounter <= 0)
                {
                    _transitionDone = true;
                    new Thread(battle.Begin) { Name = ThreadName }.Start();
                }
                return;
            }

            RenderPkmnInfo(bmpAddress, bmpWidth, bmpHeight, 0.50f, 0.05f, false, foe);
            RenderPkmnInfo(bmpAddress, bmpWidth, bmpHeight, 0.05f, 0.45f, true, ally);

            string msg = _message;
            if (msg != null)
            {
                RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, 0, (int)(bmpHeight * 0.79f), bmpWidth, (int)(bmpHeight * 0.16f), RenderUtils.Color(49, 49, 49, 128));
                fontDefault.DrawString(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * 0.10f), (int)(bmpHeight * 0.80f), msg, defaultWhite);
            }

            _actionsGUI?.RenderTick(bmpAddress, bmpWidth, bmpHeight);
            _battleEndedTransition?.RenderTick(bmpAddress, bmpWidth, bmpHeight);
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
                _actionsGUI = new ActionsGUI(this, party, party.SpritedParty[i]);
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
                case PBEPkmnHPChangedPacket phcp:
                {
                    PBEBattlePokemon pokemon = phcp.PokemonTrainer.TryGetPokemon(phcp.Pokemon);
                    SpritedBattlePokemon sPkmn = _spritedParties[pokemon.Trainer.Id].SpritedParty[pokemon.Id];
                    sPkmn.UpdateAnimationSpeed();
                    break;
                }
                case PBEStatus1Packet s1p:
                {
                    PBEBattlePokemon status1Receiver = s1p.Status1ReceiverTrainer.TryGetPokemon(s1p.Status1Receiver);
                    SpritedBattlePokemon sPkmn = _spritedParties[status1Receiver.Trainer.Id].SpritedParty[status1Receiver.Id];
                    sPkmn.UpdateAnimationSpeed();
                    break;
                }
                /*case PBEPkmnFaintedPacket pfp:
                {
                    PBEBattlePokemon pokemon = pfp.PokemonTrainer.TryGetPokemon(pfp.Pokemon);
                    BattleView.Field.HidePokemon(pokemon, pfp.OldPosition);
                    break;
                }
                case PBEPkmnFormChangedPacket pfcp:
                {
                    PBEBattlePokemon pokemon = pfcp.PokemonTrainer.TryGetPokemon(pfcp.Pokemon);
                    BattleView.Field.UpdatePokemon(pokemon, false, true);
                    break;
                }
                case PBEPkmnHPChangedPacket phcp:
                {
                    PBEBattlePokemon pokemon = phcp.PokemonTrainer.TryGetPokemon(phcp.Pokemon);
                    BattleView.Field.UpdatePokemon(pokemon, true, false);
                    break;
                }
                case PBEPkmnSwitchInPacket psip:
                {
                    if (!psip.Forced)
                    {
                        foreach (PBEPkmnSwitchInPacket.PBESwitchInInfo info in psip.SwitchIns)
                        {
                            BattleView.Field.ShowPokemon(psip.Trainer.TryGetPokemon(info.Pokemon));
                        }
                    }
                    break;
                }
                case PBEPkmnSwitchOutPacket psop:
                {
                    PBEBattlePokemon pokemon = psop.PokemonTrainer.TryGetPokemon(psop.Pokemon);
                    BattleView.Field.HidePokemon(pokemon, psop.OldPosition);
                    break;
                }
                case PBEStatus1Packet s1p:
                {
                    PBEBattlePokemon status1Receiver = s1p.Status1ReceiverTrainer.TryGetPokemon(s1p.Status1Receiver);
                    BattleView.Field.UpdatePokemon(status1Receiver, true, false);
                    break;
                }
                case PBEStatus2Packet s2p:
                {
                    PBEBattlePokemon status2Receiver = s2p.Status2ReceiverTrainer.TryGetPokemon(s2p.Status2Receiver);
                    switch (s2p.Status2)
                    {
                        case PBEStatus2.Airborne: BattleView.Field.UpdatePokemon(status2Receiver, false, true); break;
                        case PBEStatus2.Disguised:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Ended: BattleView.Field.UpdatePokemon(status2Receiver, true, true); break;
                            }
                            break;
                        }
                        case PBEStatus2.ShadowForce: BattleView.Field.UpdatePokemon(status2Receiver, false, true); break;
                        case PBEStatus2.Substitute:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added:
                                case PBEStatusAction.Ended: BattleView.Field.UpdatePokemon(status2Receiver, false, true); break;
                            }
                            break;
                        }
                        case PBEStatus2.Transformed:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: BattleView.Field.UpdatePokemon(status2Receiver, false, true); break;
                            }
                            break;
                        }
                        case PBEStatus2.Underground: BattleView.Field.UpdatePokemon(status2Receiver, false, true); break;
                        case PBEStatus2.Underwater: BattleView.Field.UpdatePokemon(status2Receiver, false, true); break;
                    }
                    break;
                }
                case PBEWeatherPacket wp:
                {
                    switch (wp.WeatherAction)
                    {
                        case PBEWeatherAction.Added:
                        case PBEWeatherAction.Ended: BattleView.Field.UpdateWeather(); break;
                        case PBEWeatherAction.CausedDamage: break;
                    }
                    break;
                }
                case PBEAutoCenterPacket acp:
                {
                    PBEBattlePokemon pokemon0 = acp.Pokemon0Trainer.TryGetPokemon(acp0.Pokemon0);
                    PBEBattlePokemon pokemon1 = acp.Pokemon1Trainer.TryGetPokemon(acp1.Pokemon1);
                    BattleView.Field.MovePokemon(pokemon0, acp.Pokemon0OldPosition);
                    BattleView.Field.MovePokemon(pokemon1, acp.Pokemon1OldPosition);
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
