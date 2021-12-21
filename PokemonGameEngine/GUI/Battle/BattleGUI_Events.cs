using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Packets;
using System.Threading;

namespace Kermalis.PokemonGameEngine.GUI.Battle
{
    internal sealed partial class BattleGUI
    {
        // Battle thread
        private IPBEPacket _newPacket;
        private PBEBattleState? _newState;
        private readonly ManualResetEvent _resumeProcessing = new(false);

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
            new Thread(start) { Name = "Battle Thread" }.Start(); // TODO: Need to implement cancellation tokens in PBE... thread causes the game to hang when closed
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
                        _ = new ActionsBuilder(t);
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
                        _ = new SwitchesBuilder(sirp.Amount);
                        InitFadeToPartyForReplacement();
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
                        UpdatePokemon(pokemon,
                            info: true);
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
                    UpdatePokemon(pkmn,
                        info: true, // Update info because HP change is possible in PBE with custom data
                        spriteImg: true, spriteImgIfSubstituted: false,
                        spriteMini: true);
                    break;
                }
                case PBEPkmnHPChangedPacket phcp:
                {
                    PBEBattlePokemon pkmn = phcp.PokemonTrainer.GetPokemon(phcp.Pokemon);
                    UpdatePokemon(pkmn,
                        info: true);
                    UpdateAnimationSpeed(pkmn);
                    break;
                }
                case PBEPkmnLevelChangedPacket plcp:
                {
                    PBEBattlePokemon pokemon = plcp.PokemonTrainer.GetPokemon(plcp.Pokemon);
                    if (pokemon.FieldPosition != PBEFieldPosition.None)
                    {
                        UpdatePokemon(pokemon,
                            info: true);
                    }
                    UpdateFriendshipForLevelUp(pokemon);
                    break;
                }
                case PBEStatus1Packet s1p:
                {
                    PBEBattlePokemon status1Receiver = s1p.Status1ReceiverTrainer.GetPokemon(s1p.Status1Receiver);
                    UpdatePokemon(status1Receiver,
                        info: true,
                        spriteColor: true);
                    UpdateAnimationSpeed(status1Receiver);
                    break;
                }
                case PBEStatus2Packet s2p:
                {
                    PBEBattlePokemon status2Receiver = s2p.Status2ReceiverTrainer.GetPokemon(s2p.Status2Receiver);
                    switch (s2p.Status2)
                    {
                        case PBEStatus2.Airborne:
                        case PBEStatus2.ShadowForce:
                        case PBEStatus2.Underground:
                        case PBEStatus2.Underwater:
                        {
                            UpdatePokemon(status2Receiver,
                                spriteVisibility: true);
                            break;
                        }
                        case PBEStatus2.Disguised:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Ended:
                                {
                                    UpdateDisguisedPID(status2Receiver);
                                    SetSeen(status2Receiver);
                                    UpdatePokemon(status2Receiver,
                                        info: true,
                                        spriteImg: true, spriteImgIfSubstituted: false,
                                        spriteMini: true);
                                    break;
                                }
                            }
                            break;
                        }
                        case PBEStatus2.Substitute:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added:
                                case PBEStatusAction.Ended:
                                {
                                    UpdatePokemon(status2Receiver,
                                        spriteImg: true, spriteImgIfSubstituted: true,
                                        spriteVisibility: true,
                                        spriteColor: true);
                                    break;
                                }
                            }
                            break;
                        }
                        case PBEStatus2.Transformed:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added:
                                {
                                    UpdatePokemon(status2Receiver,
                                        spriteImg: true, spriteImgIfSubstituted: false,
                                        spriteMini: true);
                                    break;
                                }
                            }
                            break;
                        }
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
                    PBEBattlePokemon pbePkmn = psop.PokemonTrainer.GetPokemon(psop.Pokemon);
                    HidePokemon(pbePkmn, psop.OldPosition);
                    BattlePokemon bPkmn = _parties[pbePkmn.Trainer.Id][pbePkmn];
                    bPkmn.UpdateMini();
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
    }
}
